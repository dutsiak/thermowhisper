#!/bin/sh
# A red job stops the run of ci.yml of the same Pull Request and the same head commit (no CI minutes for a PR that cannot be merged).
# $1 - name of the failed job (for the log), $2 - number of the Pull Request, $3 - head commit of the Pull Request.
# Needs gh, GH_TOKEN and GITHUB_REPOSITORY. Only runs that are not finished are cancelled; other PRs (also a run shared with another PR), other workflows (sync.yml)
# and the failed run are not touched. A cancelled run is grey (cancelled), not red.
# The ci.yml run may not exist yet when the fast job fails: while there is no ci.yml run of this PR at all, the list is read again
# (STOP_CI_TRIES times, STOP_CI_SLEEP seconds apart, 12 x 5 s by default). A finished run is not waited for. A problem in this script never makes the job red: it prints a warning and exits 0.
set -e
case "$2" in ''|*[!0-9]*) echo "stop-ci-run: bad PR number" >&2; exit 1 ;; esac
case "$3" in ''|*[!0-9a-f]*) echo "stop-ci-run: bad commit" >&2; exit 1 ;; esac
tries=${STOP_CI_TRIES:-12}; pause=${STOP_CI_SLEEP:-5}; pr=$2; sha=$3

# list_runs: prints "<id> <status>" of the ci.yml runs that belong to this PR alone (a run lists all open PRs of its branch: "5,6" is not ours)
list_runs() {
  out=$(gh api "repos/$GITHUB_REPOSITORY/actions/runs?head_sha=$sha&event=pull_request&per_page=100" \
    --jq '.workflow_runs[] | "\(.id) \(.path) \(.status) \([.pull_requests[].number] | join(","))"') || return 1
  printf '%s\n' "$out" | while read -r id path status prs; do
    [ "$path" = ".github/workflows/ci.yml" ] && [ "$prs" = "$pr" ] && echo "$id $status" || true
  done
}

i=1
while :; do
  runs=$(list_runs) || { echo "::warning::stop-ci-run: cannot list the runs, nothing was cancelled"; exit 0; }
  [ -n "$runs" ] && break
  if [ "$i" -ge "$tries" ]; then
    echo "::warning::stop-ci-run: job '$1' failed in PR #$2, but no ci.yml run of this PR appeared in $tries checks; nothing was cancelled."
    exit 0
  fi
  i=$((i + 1)); sleep "$pause"
done
stopped=0
while read -r id status; do
  [ "$status" != "completed" ] || continue
  if gh run cancel "$id"; then
    echo "stop-ci-run: job '$1' failed in PR #$2, so the run $id of ci.yml (was $status) is cancelled. Grey 'cancelled' here is not a failure: fix '$1', then re-run the cancelled run."
    stopped=$((stopped + 1))
  else
    echo "::warning::stop-ci-run: could not cancel the run $id"
  fi
done <<RUNS
$runs
RUNS
[ "$stopped" -gt 0 ] || echo "stop-ci-run: job '$1' failed in PR #$2; no unfinished run of ci.yml to cancel."
