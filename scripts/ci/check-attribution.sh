#!/bin/sh
# No tool or AI attribution in commit messages and in the Pull Request title and description (PO rule, CLAUDE.md "Conventions").
# Forbidden (any case): a claude.ai session URL, a Claude-Session trailer, "generated with", and a Co-authored-by line unless all its addresses are in
# scripts/ci/allowed-authors.txt (check-authors.sh checks the Co-authored-by trailers of commits too; this script also covers the PR text and the message body).
# Only messages and PR text are scanned, never file contents.
# Usage: check-attribution.sh commits <target branch> <PR head commit>   - messages of the own commits of the PR (same set as check-commit-ids.sh)
#        check-attribution.sh text                                       - PR title and description from the environment variables TITLE and BODY
set -e
set -f                                        # no file name expansion of the words below
dir=$(cd "$(dirname "$0")" && pwd)
list="$dir/allowed-authors.txt"
[ -f "$list" ] || { echo "check-attribution: no file $list" >&2; exit 1; }
allowed=$(sed 's/#.*//; s/[[:space:]]//g; /^$/d' "$list" | tr 'A-Z' 'a-z')
t=$(mktemp -d); trap 'rm -rf "$t"' EXIT

# scan <file>: prints the forbidden lines of the file
scan() {
  grep -iE 'claude\.ai/code/session|claude-session:|generated with' "$1" || true
  grep -iE 'co-authored-by:' "$1" | while IFS= read -r line; do
    ok=1; found=0
    for e in $(printf '%s\n' "$line" | grep -o '[^<>[:space:]]*@[^<>[:space:]]*' | tr 'A-Z' 'a-z'); do
      found=1; printf '%s\n' "$allowed" | grep -qxF -- "$e" || ok=0
    done
    [ "$found" = 1 ] && [ "$ok" = 1 ] || printf '%s\n' "$line"
  done
}

# report <place> <file>: prints the place and its forbidden lines to $t/bad
report() {
  scan "$2" | while IFS= read -r line; do printf '  %s: %s\n' "$1" "$line"; done >> "$t/bad"
}

case "$1" in
  commits)
    if git rev-parse --verify -q origin/main >/dev/null; then   # with a git platform - remote branches, without it - local ones
      m=origin/main; b=origin/$2
    else
      m=main; b=$2
    fi
    for ref in "$3" "$m" "$b"; do
      git rev-parse --verify -q "$ref^{commit}" >/dev/null \
        || { echo "check-attribution: $ref not found (fetch-depth: 0 is needed)" >&2; exit 1; }
    done
    git rev-list --no-merges "$3" ^"$m" ^"$b" > "$t/commits"   # own commits: not from main, not from the target branch
    while read -r c; do
      git log -1 --format=%B "$c" > "$t/msg"
      report "commit $(git log -1 --format=%h "$c")" "$t/msg"
    done < "$t/commits"
    fix="Person: reword the commits (git rebase -i origin/main), then git push --force-with-lease into your own branch.
  AI agent (force-push is forbidden): do not push more commits on this branch. Create '<branch>-2' from origin/main, run 'git merge --squash <branch>', commit without the forbidden text, push it, open a new PR with the same title, close this PR and tell the Lead. Do not delete branches." ;;
  text)
    printf '%s\n' "$TITLE" > "$t/title"; report "PR title" "$t/title"
    printf '%s\n' "$BODY" > "$t/body"; report "PR description" "$t/body"
    fix="Edit the PR text (PR page, or gh pr edit <number> --title ... --body-file ...) and remove the forbidden text." ;;
  *) echo "usage: check-attribution.sh commits <target> <head> | text" >&2; exit 1 ;;
esac

if [ -s "$t/bad" ]; then
  {
    echo "check-attribution: tool or AI attribution is forbidden in commit messages and PR text (CLAUDE.md, Conventions). Found:"
    head -n 20 "$t/bad"
    n=$(wc -l < "$t/bad")
    [ "$n" -gt 20 ] && echo "  ... and $((n - 20)) more"
    echo "Forbidden: claude.ai session URLs, Claude-Session lines, 'Generated with', Co-authored-by (except addresses from scripts/ci/allowed-authors.txt)."
    echo "How to fix:"
    echo "  $fix"
  } >&2
  exit 1
fi
