#!/bin/sh
# Creates the DRAFT GitHub release for a tag (sections 13, 19 of GIT STRATEGY). Runs in CI on the tag commit:
#   sh scripts/release/create-draft-release.sh v0.1.0 dist/ThermoWhisper-v0.1.0-x64-unsigned-test.msix
# $1 = tag, $2 = the MSIX built by scripts/ci/build.ps1. A signed MSIX in signed/ (from SignPath) wins over $2.
# DRY_RUN=true: print what would be attached and the notes, create nothing.
set -e
tag=$1
built=$2
sh scripts/release/release-notes.sh "$tag"
if ls signed/*.msix >/dev/null 2>&1; then
  file=$(ls signed/*.msix | head -n1)
else
  case "$built" in
    *-unsigned-test.msix) file=$built ;;   # pre-1.0 test package, already named by build.ps1
    *) file="${built%.msix}-unsigned.msix"; [ "${DRY_RUN:-}" = true ] || mv "$built" "$file" ;;
  esac
fi
case "$file" in
  *-unsigned-test.msix)
    name=$(basename "$file")
    {
      printf '\n---\n'
      printf '**Unsigned test build (pre-1.0).** Not signed, for testing only. Windows 11 only. Install in PowerShell as administrator:\n\n'
      printf '`Add-AppxPackage -Path .\\%s -AllowUnsigned`\n\n' "$name"
      printf 'Double-click install does not work for unsigned packages. A test install is a separate app from the future signed release.\n'
    } >> release-notes.md
    ;;
esac
if [ "${DRY_RUN:-}" = true ]; then
  echo "create-draft-release: tag=$tag file=$file"
  exit 0
fi
gh release create "$tag" --draft --notes-file release-notes.md "$file"
