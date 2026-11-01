#!/bin/sh
# Checks the authors and co-authors of Pull Request commits (sections 12, 19 of GIT STRATEGY).
# $1 - the base branch, for example origin/dev. Allowed addresses - scripts/ci/allowed-authors.txt.
set -e
list=scripts/ci/allowed-authors.txt
[ -f "$list" ] || { echo "check-authors: no file $list (section 19)" >&2; exit 1; }
git rev-parse --verify -q "$1" >/dev/null || { echo "check-authors: branch $1 not found" >&2; exit 1; }
allowed=$(sed 's/#.*//; s/[[:space:]]//g; /^$/d' "$list" | tr 'A-Z' 'a-z')
emails=$(git log --no-merges --format='%ae%n%(trailers:key=Co-authored-by,valueonly)' "$1..HEAD" \
  | grep -o '[^<>[:space:]]*@[^<>[:space:]]*' | tr 'A-Z' 'a-z' | sort -u)
bad=
for e in $emails; do
  printf '%s\n' "$allowed" | grep -qxF -- "$e" || bad="$bad $e"
done
if [ -n "$bad" ]; then
  echo "check-authors: authors or co-authors outside the list $list:$bad (section 12)" >&2; exit 1
fi
