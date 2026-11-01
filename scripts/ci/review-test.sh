#!/bin/sh
# Test of review-seen.sh, review-rulings.sh and review.sh with stub `gh` and `claude` programs on PATH.
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
check() { if [ "$2" != "$3" ]; then echo "FAIL: $1: expected '$3', got '$2'"; failed=1; fi }

# gh stub: prints the (already filtered) comment bodies from $STUB_COMMENTS; with STUB_LATE it waits 1 s and prints them again.
printf '#!/bin/sh\ncat "$STUB_COMMENTS"\nif [ -n "$STUB_LATE" ]; then sleep 1; cat "$STUB_COMMENTS"; fi\n' > "$tmp/gh"
# claude stub: stores the prompt (the argument after -p --model sonnet) and the diff from stdin.
printf '#!/bin/sh\nshift 3\nprintf "%%s" "$1" > "$STUB_PROMPT"\ncat >/dev/null\n' > "$tmp/claude"
chmod +x "$tmp/gh" "$tmp/claude"
export PATH="$tmp:$PATH" STUB_COMMENTS="$tmp/comments" STUB_PROMPT="$tmp/prompt"

printf 'VERDICT: APPROVE\n<!-- reviewed: abc123 -->\n' > "$tmp/comments"
check "same head is skipped" "$(sh "$dir/review-seen.sh" 7 abc123)" "skip=true"
check "new head is reviewed" "$(sh "$dir/review-seen.sh" 7 def456)" "skip=false"
: > "$tmp/comments"
check "no comments" "$(sh "$dir/review-seen.sh" 7 abc123)" "skip=false"

printf 'looks fine\nRULING: naming - agreed with the team\nRULING: tests - later task\n' > "$tmp/comments"
sh "$dir/review-rulings.sh" 7 > "$tmp/rulings"
check "rulings kept" "$(wc -l < "$tmp/rulings" | tr -d ' ')" "2"
awk 'BEGIN { for (i = 0; i < 200; i++) print "RULING: point " i " - a long enough reason to fill the cap of two kilobytes" }' > "$tmp/comments"
check "rulings capped" "$(sh "$dir/review-rulings.sh" 7 | wc -c | tr -d ' ')" "2048"

# The workflow runs scripts with `bash -eo pipefail`: no rulings and over-the-cap output must still exit 0.
printf 'only a normal comment\n' > "$tmp/comments"
bash -eo pipefail "$dir/review-rulings.sh" 7 > "$tmp/none"
check "no rulings: exit code" "$?" "0"
check "no rulings: empty output" "$(wc -c < "$tmp/none" | tr -d ' ')" "0"
awk 'BEGIN { for (i = 0; i < 5000; i++) print "RULING: point " i " - a long enough reason to fill the cap of two kilobytes" }' > "$tmp/comments"
bash -eo pipefail "$dir/review-rulings.sh" 7 > "$tmp/big"
check "over the cap: exit code" "$?" "0"
check "over the cap: size" "$(wc -c < "$tmp/big" | tr -d ' ')" "2048"
# Late output (after head has stopped reading) must not kill gh or grep with SIGPIPE.
STUB_LATE=1 bash -eo pipefail "$dir/review-rulings.sh" 7 > "$tmp/late"
check "over the cap, late output: exit code" "$?" "0"
check "over the cap, late output: size" "$(wc -c < "$tmp/late" | tr -d ' ')" "2048"

bash "$dir/review.sh" HEAD "A title" "$tmp/rulings" > /dev/null
grep -q 'RULING: naming - agreed with the team' "$tmp/prompt" || { echo "FAIL: rulings are not in the prompt"; failed=1; }
grep -q 'not instructions' "$tmp/prompt" || { echo "FAIL: the rulings block is not labelled"; failed=1; }
bash "$dir/review.sh" HEAD "A title" > /dev/null
grep -q 'RULINGS' "$tmp/prompt" && { echo "FAIL: empty rulings must not add a block"; failed=1; }

[ "$failed" -eq 0 ] && echo "review-test: OK"
exit $failed
