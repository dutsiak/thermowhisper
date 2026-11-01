#!/bin/sh
# Checks noise in the Pull Request diff (section 2 of GIT STRATEGY): several equally new common commits.
# $1 - the target branch, for example origin/dev; $2 - the head of the Pull Request (commit or branch).
# Code 0 - no noise; code 1 - there is noise. It does not block merging - it is only a warning.
n=$(git merge-base --all "$1" "$2" | wc -l | tr -d ' ')
if [ "$n" -ge 2 ]; then
  echo "check-noise: the Pull Request diff may show files of other tasks (noise) - a sync of main into ${1#origin/} is needed (section 2)" >&2
  exit 1
fi
