#!/bin/sh
# Every own commit of a task branch starts with '<type>: <ID> ' - the task ID from the branch name (section 12 of GIT STRATEGY), so the changelog can group by ID.
# $1 - target branch of the Pull Request, $2 - name of the Pull Request branch, $3 - head of the Pull Request (commit or branch).
set -e
case "$2" in                                  # moved or generated commits, not written for a task
  main|release/*|chore/Release-*|chore/Sync-*|chore/Merge-*|revert/*|hotfix/*) echo "check-commit-ids: skipped: branch '$2' is exempt"; exit 0 ;;
esac
type=${2%%/*}
case "$type" in
  feat|fix|refactor|perf|docs|test|ci|chore|build|style) ;;
  *) echo "check-commit-ids: skipped: branch '$2' has no conventional type"; exit 0 ;;
esac
id=$(printf '%s\n' "${2#*/}" | grep -oE '^[A-Z][A-Z0-9]*-[0-9]+-' | sed 's/-$//' || true)
if [ -z "$id" ]; then echo "check-commit-ids: skipped: branch '$2' has no task ID"; exit 0; fi
if git rev-parse --verify -q origin/main >/dev/null; then   # with a git platform - remote branches, without it - local ones
  m=origin/main; b=origin/$1
else
  m=main; b=$1
fi
for ref in "$3" "$m" "$b"; do
  git rev-parse --verify -q "$ref^{commit}" >/dev/null \
    || { echo "check-commit-ids: $ref not found (fetch-depth: 0 is needed)" >&2; exit 1; }
done
t=$(mktemp -d); trap 'rm -rf "$t"' EXIT
pattern="^[a-z]+(\([a-z0-9][a-z0-9._-]*\))?!?: $id "
git rev-list --no-merges "$3" ^"$m" ^"$b" | while read -r c; do   # own commits: not from main, not from the target branch
  git log -1 --format=%s "$c" | grep -qE "$pattern" || git log -1 --format='  %h %s' "$c"
done > "$t/bad"
if [ -s "$t/bad" ]; then
  {
    echo "check-commit-ids: these commits of branch '$2' do not start with '$type: $id ' (task ID of the branch, GIT_STRATEGY_EN.md section 12):"
    head -n 20 "$t/bad"
    n=$(wc -l < "$t/bad")
    [ "$n" -gt 20 ] && echo "  ... and $((n - 20)) more"
    echo "How to fix:"
    echo "  Person: reword the commits (git rebase -i origin/main, change each subject to '$type: $id <Description>'), then git push --force-with-lease into your own branch."
    echo "  AI agent (force-push is forbidden): do not push more commits on this branch. Create '$2-2' from origin/main, run 'git merge --squash $2', commit as '$type: $id <Description>', push it, open a new PR with the same title, close this PR and tell the Lead. Do not delete branches."
    echo "  Do not push empty 'Trigger checks' commits: re-run the CI job on the PR page (or 'gh run rerun <run-id>') instead."
  } >&2
  exit 1
fi
