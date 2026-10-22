#!/bin/sh
# Prepares a release (section 12 of GIT STRATEGY): generates CHANGELOG.md and sets the version in the project files.
# Runs in the branch chore/Release-vX.Y.Z - automatically in CI or manually.
set -e
branch=$(git rev-parse --abbrev-ref HEAD)
tag=${branch#chore/Release-}
case "$branch" in
  chore/Release-v[0-9]*.[0-9]*.[0-9]*) ;;
  *) echo "prepare-release: run only in the branch chore/Release-vX.Y.Z (now: $branch)" >&2; exit 1 ;;
esac
if ! printf '%s' "$tag" | grep -Eq '^v[0-9]+\.[0-9]+\.[0-9]+$'; then
  echo "prepare-release: the version in the branch name must be in the format vX.Y.Z (now: $tag)" >&2; exit 1
fi
if ! git diff --quiet || ! git diff --cached --quiet; then
  echo "prepare-release: there are uncommitted changes - commit or remove them first" >&2; exit 1
fi
if command -v git-cliff >/dev/null 2>&1; then
  git-cliff --tag "$tag" -o CHANGELOG.md
else
  npx --yes git-cliff@latest --tag "$tag" -o CHANGELOG.md
fi
sh scripts/release/set-version.sh "${tag#v}"
git add CHANGELOG.md
git add -u          # all changed files that are already in the repository (including those changed by set-version.sh)
if git diff --cached --quiet; then
  echo "prepare-release: no changes - release $tag is already prepared"
  exit 0
fi
git commit -m "chore(release): $tag"
