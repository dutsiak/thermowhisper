#!/bin/sh
# For a Pull Request into stage and main: all commits have already passed the previous stage (section 13 of GIT STRATEGY).
# $1 - the target branch (stage or main), $2 - the name of the Pull Request branch, $3 - the head of the Pull Request (commit).
set -e
case "$2" in
  main|chore/Release-*|chore/Merge-*|chore/Sync-*|chore/Setup-repository|revert/*) exit 0 ;;   # new commits are expected here
esac
case "$1" in
  stage) prev=dev ;;
  main)  prev=stage ;;
  *)     exit 0 ;;
esac
git rev-parse --verify -q "origin/$prev" >/dev/null || { echo "check-promoted: branch origin/$prev not found" >&2; exit 1; }
# git log X ^A ^B means: commits that are in X, but are in neither A nor B.
# Goal: find new code in the Pull Request that has not passed the previous stage yet (dev -> stage -> main).
# PR into stage: ^origin/dev   - remove commits that are already in dev: they passed the reviewer and the check in dev.
# PR into main:  ^origin/stage - remove commits that are already in stage: they passed dev and testing on stage.
# In both:       ^origin/main  - remove commits that are already in main (for example, the release commit chore(release)):
#                the task branch gets them when it is created from main or merges main into itself; this is not task code,
#                and it is already in production (for a PR into main this is also the target branch itself).
new=$(git log --oneline --no-merges "$3" "^origin/$prev" ^origin/main)
if [ -n "$new" ]; then
  echo "check-promoted: these commits have not passed $prev yet - first merge the branch $2 into $prev (section 13):" >&2
  echo "$new" >&2
  exit 1
fi
