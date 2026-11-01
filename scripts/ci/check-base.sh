#!/bin/sh
# A task branch does not contain foreign merges from dev, stage or release/* (section 4 of GIT STRATEGY): it is created from main and merges only main into itself.
# $1 - target branch of the Pull Request, $2 - name of the Pull Request branch, $3 - head of the Pull Request (commit or branch).
set -e
case "$2" in                                  # branches that by the rules are created not from main
  main|chore/Merge-*|chore/Sync-*|revert/*) exit 0 ;;
  chore/Release-*) case "$1" in main|release/*) exit 0 ;; esac ;;
  hotfix/*)        case "$1" in release/*) exit 0 ;; esac ;;
  release/*)       case "$1" in main) exit 0 ;; esac ;;
esac
if git rev-parse --verify -q origin/main >/dev/null; then   # with a git platform - remote branches, without it - local ones
  m=origin/main; refs='refs/remotes/origin/dev refs/remotes/origin/stage refs/remotes/origin/release/*'
else
  m=main; refs='refs/heads/dev refs/heads/stage refs/heads/release/*'
fi
git rev-parse --verify -q "$3^{commit}" >/dev/null && git rev-parse --verify -q "$m" >/dev/null \
  || { echo "check-base: $3 or $m not found" >&2; exit 1; }
t=$(mktemp -d); trap 'rm -rf "$t"' EXIT
git rev-list "$3" ^"$m" > "$t/own"           # commits of the branch that are not in main
# foreign merges: merge commits on the "first line" (first-parent) of permanent branches that are not in main and that merged something other than main there
# (a fast-forward adds only the task's own commits - this does not affect them)
for b in $(git for-each-ref --format='%(refname:short)' $refs); do
  git rev-list --first-parent --merges --parents "$b" ^"$m" | while read c p1 p2 rest; do
    git merge-base --is-ancestor "$p2" "$m" || echo "$c"
  done
done > "$t/perm"
bad=$(grep -Fxf "$t/perm" "$t/own" || true)
if [ -n "$bad" ]; then
  echo "check-base: branch $2 contains foreign merges from a permanent branch (dev, stage or release/*) - it was created not from main or a permanent branch was merged into it (section 4). Foreign merges:" >&2
  for c in $bad; do git log -1 --format='  %h %s' "$c" >&2; done
  exit 1
fi
