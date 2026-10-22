#!/bin/sh
# Creates release-notes.md - a description of the changes of one version (sections 13, 19 of GIT STRATEGY).
# Runs in CI on the tag commit: sh scripts/release/release-notes.sh v2.18.0
# The second parameter (optional) - the previous published tag, if the nearest previous tag was not released (its CI/CD failed).
set -e
tag=$1
prev=${2:-$(git describe --tags --abbrev=0 "$tag^" 2>/dev/null || true)}
if command -v git-cliff >/dev/null 2>&1; then cliff=git-cliff; else cliff="npx --yes git-cliff@latest"; fi
if [ -n "$prev" ]; then
  $cliff "$prev..$tag" -o release-notes.md
else
  $cliff -o release-notes.md
fi
