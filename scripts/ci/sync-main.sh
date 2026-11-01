#!/bin/sh
# Automatic sync of main into dev or stage, when the task Pull Request has noise (section 2 of GIT STRATEGY). GitHub (gh).
# $1 - the target branch (dev or stage), $2 - the number of the task Pull Request, $3 - the head of the task Pull Request (commit).
# MODE (required), three jobs of the workflow sync:
#   prepare - Linux: noise, sync Pull Request, trial merge; the result - into $GITHUB_OUTPUT (need, sync_pr, base_sha, main_sha, tree);
#   verify  - Windows: recreate the merge from BASE_SHA and MAIN_SHA and compare the fingerprint TREE (lint and tests run after it, as workflow steps);
#   finish  - Linux: merge the sync Pull Request or write a comment (NEED, SYNC_PR, BASE_SHA, MAIN_SHA, VERIFY - the result of verify).
# ASSIGNEE - who to put into Assignees of the sync Pull Request (if it fails - the Pull Request is created anyway).
set -e
# the script switches the working folder to other commits - so it runs from a temporary copy
if [ -z "$SYNC_COPY" ]; then c=$(mktemp); cp "$0" "$c"; SYNC_COPY=1 exec sh "$c" "$@"; fi
base=$1; pr=$2; head=$3; mode=$MODE; out=${GITHUB_OUTPUT:-/dev/null}
bot() { git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"; }
try_merge() {   # $1 - the commit of the target branch, $2 - the commit of main; 1 - conflict
  git switch -q --detach "$1"
  git merge -q --no-ff "$2" -m "Merge main into $base" >/dev/null 2>&1 && return 0
  git merge --abort; return 1
}
merge_sync() {  # $1 - the commit of the target branch, $2 - the commit of main that were checked; if they changed meanwhile - the sync will repeat
  git fetch -q origin
  if [ "$(git rev-parse "origin/$base")" != "$1" ] || ! gh pr merge "$sync" --merge --match-head-commit "$2"; then
    echo "sync: $base or main changed during the check - the sync will repeat on the next Pull Request"; return 0
  fi
  gh workflow run ci.yml --ref "$base"
  echo "sync: main merged into $base (Pull Request #$sync)"
}
comment() {     # $1 - the reason; once per task Pull Request
  mark="<!-- git-strategy:sync -->"
  if gh pr view "$pr" --json comments --jq '.comments[].body' | grep -qF -- "$mark"; then return 0; fi
  link=$(gh pr view "$sync" --json url --jq .url)
  gh pr comment "$pr" --body "$mark
⚠️ **First sync \`main\` → \`$base\`: $1**
The diff of this PR contains files of other tasks (noise). The automatic sync stopped: $link
Before any actions with this PR:
1. Resolve the problem: a human - by merging on the computer; an AI agent - via the branch \`chore/Sync-main-into-$base-…\` (section 2 of GIT STRATEGY).
2. Merge the sync. The PR at the link will close by itself when \`main\` gets into \`$base\`.
After this the diff of this PR will update automatically, the task branch does not need to be updated."
}

case "$mode" in
verify)
  bot
  try_merge "$BASE_SHA" "$MAIN_SHA" || { echo "sync: the merge was not recreated" >&2; exit 1; }
  [ "$(git rev-parse 'HEAD^{tree}')" = "$TREE" ] || { echo "sync: the merge fingerprint is different from the one in prepare" >&2; exit 1; }
  ;;
finish)
  git fetch -q origin
  sync=$SYNC_PR
  if [ "$NEED" = verify ] && [ "$VERIFY" = cancelled ]; then echo "sync: the check was cancelled - nothing merged"; exit 0; fi
  if [ "$NEED" = verify ] && [ "$VERIFY" = success ]; then merge_sync "$BASE_SHA" "$MAIN_SHA"; exit 0; fi
  if [ "$NEED" = conflict ]; then comment "conflict"; else comment "the linter or tests do not pass on the merge result (see the log of the job verify)"; fi
  ;;
prepare)
  git fetch -q origin
  if sh scripts/ci/check-noise.sh "origin/$base" "$head" 2>/dev/null; then echo "sync: no noise"; exit 0; fi
  if git merge-base --is-ancestor origin/main "origin/$base"; then echo "sync: main is already in $base"; exit 0; fi
  bot
  sync=$(gh pr list --base "$base" --head main --state open --json number --jq '.[0].number // empty')
  if [ -z "$sync" ]; then
    url=$(gh pr create --base "$base" --head main --title "chore: Sync main into $base" --body "Automatic sync (section 2 of GIT STRATEGY).")
    sync=${url##*/}
  fi
  [ -z "$ASSIGNEE" ] || gh pr edit "$sync" --add-assignee "$ASSIGNEE" >/dev/null 2>&1 || echo "sync: could not assign $ASSIGNEE (the Pull Request is kept)" >&2
  base_sha=$(git rev-parse "origin/$base"); main_sha=$(git rev-parse origin/main)
  tree=
  if try_merge "$base_sha" "$main_sha"; then need=verify; tree=$(git rev-parse 'HEAD^{tree}'); else need=conflict; fi
  printf 'need=%s\nsync_pr=%s\nbase_sha=%s\nmain_sha=%s\ntree=%s\n' "$need" "$sync" "$base_sha" "$main_sha" "$tree" >> "$out"
  ;;
*) echo "sync: MODE must be prepare, verify or finish" >&2; exit 2 ;;
esac
