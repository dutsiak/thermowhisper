#!/bin/sh
# Test of the task order in CHANGELOG.md and release-notes.md (cliff.toml; git-cliff already sorts the
# padded task IDs numerically). A scratch repository gets commits in a mixed order; both outputs must list TW-2, TW-4, TW-11, TW-99, TW-100, then "Other".
dir=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$dir/../.." && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
. "$root/scripts/versions.env"
if command -v git-cliff >/dev/null 2>&1; then cliff=git-cliff; else cliff="npx --yes git-cliff@$GIT_CLIFF_VERSION"; fi
failed=0

set -e
git init -q -b main "$tmp/r"; cd "$tmp/r"
git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git config core.autocrlf false
mkdir -p scripts/release && cp "$root/cliff.toml" . && cp "$root/scripts/versions.env" scripts/ && cp "$dir/release-notes.sh" scripts/release/
n=0
commit() { n=$((n + 1)); echo "$n" > f; git add -A; git commit -qm "$1"; }
commit "chore: initial"; git tag v0.0.1
commit "feat: TW-11 eleven"; commit "feat: TW-2 two"; commit "fix: TW-4 four"; commit "docs: no id here"
commit "feat(ui): TW-2 two ui"; commit "feat: TW-99 ninety-nine"; commit "feat: TW-100 hundred"
git tag v0.1.0

# $cliff is unquoted on purpose: the npx fallback is a command with arguments. Errors stay visible; set -e stops the test.
$cliff --tag v0.1.0 -o CHANGELOG.md
sh scripts/release/release-notes.sh v0.1.0   # writes release-notes.md into the current folder
set +e

# line FILE TEXT - prints the number of the first line with the text
line() { grep -n -F -m1 -- "$2" "$1" | cut -d: -f1; }
check_order() {   # $1 - file; the rest - texts that must appear in this order
  file=$1; shift; prev=0
  for text in "$@"; do
    at=$(line "$file" "$text")
    if [ -z "$at" ] || [ "$at" -le "$prev" ]; then echo "FAIL: $file: '$text' is missing or out of order (line '$at', after line $prev)"; failed=1; return; fi
    prev=$at
  done
}
for f in CHANGELOG.md release-notes.md; do
  check_order "$f" "### TW-2" "feat: (ui) two ui" "### TW-4" "### TW-11" "### TW-99" "### TW-100" "### Other" "docs: No id here"
done
grep -q 'chore: Initial' release-notes.md && { echo "FAIL: release-notes.md contains the previous version"; failed=1; }

[ "$failed" -eq 0 ] && echo "test-changelog: OK"
exit $failed
