#!/bin/sh
# Must `main` be merged into dev or stage before this task branch is opened as a Pull Request? (section 2 of GIT STRATEGY, the "noise" rule.)
# Prints NEEDED or NOT NEEDED, then one line with the reason. Exit 0 for both answers, 2 for a wrong argument or a missing ref.
# $1 - target branch (dev or stage), $2 - the task branch or commit (default: HEAD). Used by sync-main.sh (mode prepare) and by hand before opening a PR.
# With origin/main present the remote branches are used (CI, a clone), without it the local ones.
set -e
case "$1" in dev|stage) ;; *) echo "sync-needed: the target must be dev or stage" >&2; exit 2 ;; esac
case "$2" in -*) echo "sync-needed: bad branch" >&2; exit 2 ;; esac
dir=$(cd "$(dirname "$0")" && pwd)
if git rev-parse --verify -q origin/main >/dev/null; then m=origin/main; t=origin/$1; else m=main; t=$1; fi
head=${2:-HEAD}
for ref in "$m" "$t" "$head"; do
  git rev-parse --verify -q "$ref^{commit}" >/dev/null || { echo "sync-needed: $ref not found" >&2; exit 2; }
done
if sh "$dir/check-noise.sh" "$t" "$head" 2>/dev/null; then echo "NOT NEEDED"; echo "only one merge base with $1: no noise"; exit 0; fi
if git merge-base --is-ancestor "$m" "$t"; then echo "NOT NEEDED"; echo "main is already in $1"; exit 0; fi
echo "NEEDED"; echo "several merge bases with $1 and main is not in $1 yet"
