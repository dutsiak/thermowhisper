#!/bin/sh
# Test of check-attribution.sh in a temporary repository: local-branch mode (no origin) and origin/ mode (a clone). Text mode needs no repository.
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
n=0
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.autocrlf GIT_CONFIG_VALUE_0=false
# expect <case name> <expected exit code> <command words...>   (runs in the current folder; output goes to $tmp/out and $tmp/err)
expect() {
  name=$1; want=$2; shift 2
  "$@" > "$tmp/out" 2> "$tmp/err"; code=$?
  if [ "$code" != "$want" ]; then echo "FAIL: $mode: $name: expected exit $want, got $code"; failed=1; fi
}
# has <case name> <file> <text>: the file in $tmp contains the text
has() { grep -qF -- "$3" "$tmp/$2" || { echo "FAIL: $mode: $1: $2 lacks '$3'"; failed=1; }; }
cm() { n=$((n + 1)); echo "$n" > "f$n.txt"; git add -A; git commit -q "$@"; }
# mk <branch> <start> <message>: new branch with one commit
mk() { git switch -q -c "$1" "$2"; cm -m "$3"; }
commits() { sh "$dir/check-attribution.sh" commits dev "$1"; }
text() { TITLE=$1 BODY=$2 sh "$dir/check-attribution.sh" text; }

ok_mail='131253149+mdutsiak@users.noreply.github.com'
git init -q -b main "$tmp/work" && cd "$tmp/work" || exit 1
git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
cm -m "chore: root"
git branch dev
mk clean main "ci: TW-21 clean"
mk url-subject main "ci: TW-21 see https://claude.ai/code/session_abc"
git switch -q -c url-body main; cm -m "ci: TW-21 x" -m "https://claude.ai/code/session_abc"
git switch -q -c trailer main; cm -m "ci: TW-21 x" -m "Claude-Session: abc"
git switch -q -c trailer-case main; cm -m "ci: TW-21 x" -m "CLAUDE-SESSION: abc"
git switch -q -c generated main; cm -m "ci: TW-21 x" -m "Generated with Claude Code"
git switch -q -c coauthor-bad main; cm -m "ci: TW-21 x" -m "Co-authored-by: X <x@example.com>"
git switch -q -c coauthor-none main; cm -m "ci: TW-21 x" -m "Co-authored-by: nobody"
git switch -q -c coauthor-ok main; cm -m "ci: TW-21 x" -m "Co-authored-by: Owner <$ok_mail>"
git switch -q -c middle main; cm -m "ci: TW-21 a"; cm -m "ci: TW-21 b" -m "Claude-Session: abc"; cm -m "ci: TW-21 c"
git switch -q -c old main; cm -m "chore: old" -m "Claude-Session: abc"        # will be in main and in dev
git switch -q main; git merge -q --ff-only old
git switch -q -c from-main-merged dev; cm -m "ci: TW-21 own"
git merge -q --no-ff main -m "Merge main into from-main-merged"
git switch -q dev; cm -m "chore: dev only" -m "Generated with a tool"           # the target holds an old bad commit
git switch -q -c from-dev-merged main; cm -m "ci: TW-21 own"
git merge -q --no-ff dev -m "Merge dev into from-dev-merged"
git switch -q main

run_cases() {   # $1 - prefix of branch refs ("" or "origin/"), $2 - mode name
  mode=$2; p=$1
  expect "clean commits" 0 commits "${p}clean"
  expect "session URL in a subject" 1 commits "${p}url-subject"
  has "session URL in a subject" err "claude.ai/code/session"
  expect "session URL in a body" 1 commits "${p}url-body"
  expect "Claude-Session trailer" 1 commits "${p}trailer"
  has "Claude-Session trailer" err "commit "
  expect "case variant CLAUDE-SESSION" 1 commits "${p}trailer-case"
  expect "Generated with" 1 commits "${p}generated"
  expect "Co-authored-by, address not allowed" 1 commits "${p}coauthor-bad"
  has "Co-authored-by, address not allowed" err "x@example.com"
  expect "Co-authored-by without address" 1 commits "${p}coauthor-none"
  expect "Co-authored-by, allowed address" 0 commits "${p}coauthor-ok"
  expect "bad commit in the middle" 1 commits "${p}middle"
  has "bad commit in the middle" err "Claude-Session: abc"
  has "any failure" err "git merge --squash"
  expect "old bad commit in main, main merged in" 0 commits "${p}from-main-merged"
  expect "old bad commit in the target, dev merged in" 0 commits "${p}from-dev-merged"
  expect "unknown head" 1 commits "${p}no-such-branch"
  has "unknown head" err "not found"
}
run_cases "" "local branches"
git clone -q "$tmp/work" "$tmp/clone" && cd "$tmp/clone" || exit 1
git fetch -q origin
run_cases "origin/" "origin/ refs"

mode="text"
expect "clean title and body" 0 text "ci: TW-21 Add check" "Line one
WAS_CONFLICT: no"
expect "empty body" 0 text "ci: TW-21 Add check" ""
expect "session URL in the title" 1 text "ci: TW-21 https://claude.ai/code/session_abc" "ok"
has "session URL in the title" err "PR title"
expect "session URL in the body" 1 text "ci: TW-21 Add check" "text
https://claude.ai/code/session_abc"
has "session URL in the body" err "PR description"
expect "Claude-Session in the body, other case" 1 text "ci: TW-21 Add check" "claude-session: abc"
expect "Generated with in the body" 1 text "ci: TW-21 Add check" "Generated with Claude Code"
expect "Co-authored-by in the body, address not allowed" 1 text "ci: TW-21 Add check" "Co-authored-by: X <x@example.com>"
expect "Co-authored-by in the body, allowed address" 0 text "ci: TW-21 Add check" "Co-authored-by: Owner <$ok_mail>"
cd "$tmp" || exit 1
expect "shell characters in the body" 1 text "ci: TW-21 Add check" '$(touch pwned) `id` ; Claude-Session: x'
has "shell characters in the body" err '$(touch pwned)'
[ ! -e pwned ] || { echo "FAIL: $mode: the PR text was executed"; failed=1; }

[ "$failed" = 0 ] && echo "check-attribution-test: OK"
exit "$failed"
