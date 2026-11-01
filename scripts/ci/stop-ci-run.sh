#!/bin/sh
# A red job stops the run of ci.yml of the same Pull Request and the same head commit (no CI minutes for a PR that cannot be merged).
# $1 - name of the failed job (for the log), $2 - number of the Pull Request, $3 - head commit of the Pull Request.
# Needs gh, GH_TOKEN and GITHUB_REPOSITORY. Only runs that are not finished are cancelled; other PRs, other workflows (sync.yml) and the failed run are not touched.
# A cancelled run is grey (cancelled), not red. A problem in this script never makes the job red: it prints a warning and exits 0.
set -e
case "$2" in ''|*[!0-9]*) echo "stop-ci-run: bad PR number" >&2; exit 1 ;; esac
case "$3" in ''|*[!0-9a-f]*) echo "stop-ci-run: bad commit" >&2; exit 1 ;; esac
runs=$(gh api "repos/$GITHUB_REPOSITORY/actions/runs?head_sha=$3&event=pull_request&per_page=100" \
  --jq '.workflow_runs[] | "\(.id) \(.path) \(.status) \([.pull_requests[].number] | join(","))"') \
  || { echo "::warning::stop-ci-run: cannot list the runs, nothing was cancelled"; exit 0; }
stopped=0
while read -r id path status prs; do
  [ "$path" = ".github/workflows/ci.yml" ] && [ "$status" != "completed" ] || continue
  case ",$prs," in *",$2,"*) ;; *) continue ;; esac
  if gh run cancel "$id"; then
    echo "stop-ci-run: job '$1' failed in PR #$2, so the run $id of ci.yml (was $status) is cancelled. Grey 'cancelled' here is not a failure: fix '$1', then re-run the cancelled run."
    stopped=$((stopped + 1))
  else
    echo "::warning::stop-ci-run: could not cancel the run $id"
  fi
done <<EOF
$runs
EOF
[ "$stopped" -gt 0 ] || echo "stop-ci-run: job '$1' failed in PR #$2; no unfinished run of ci.yml to cancel."
