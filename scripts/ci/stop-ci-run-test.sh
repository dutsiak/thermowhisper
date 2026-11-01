#!/bin/sh
# Test of stop-ci-run.sh with a fake gh: which runs are cancelled and which are not.
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
mkdir "$tmp/bin"
cat > "$tmp/bin/gh" <<'EOF'
#!/bin/sh
# fake gh: "api" counts its calls and prints the runs from $FAKE_RUNS (already in the form the script asks for) from the call number $FAKE_RUNS_FROM_CALL on; "run cancel <id>" is recorded
case "$1" in
  api) [ -z "$FAKE_API_FAIL" ] || exit 1; c=$(($(cat "$FAKE_COUNT" 2>/dev/null || echo 0) + 1)); echo "$c" > "$FAKE_COUNT"
       [ "$c" -ge "${FAKE_RUNS_FROM_CALL:-1}" ] && cat "$FAKE_RUNS"; exit 0 ;;
  run) echo "$3" >> "$FAKE_LOG"; [ -z "$FAKE_CANCEL_FAIL" ] || exit 1 ;;
esac
EOF
export PATH="$tmp/bin:$PATH" GITHUB_REPOSITORY=o/r FAKE_RUNS="$tmp/runs" FAKE_LOG="$tmp/log" FAKE_COUNT="$tmp/count" STOP_CI_TRIES=3 STOP_CI_SLEEP=0
sha=0123456789abcdef0123456789abcdef01234567
ci=.github/workflows/ci.yml
# expect <case name> <expected exit code> <expected cancelled ids, space separated> <PR number>   (the runs are in $tmp/runs)
expect() {
  : > "$tmp/log"; rm -f "$tmp/count"
  sh "$dir/stop-ci-run.sh" rules "$4" "$sha" > "$tmp/out" 2> "$tmp/err"; code=$?
  got=$(tr '\n' ' ' < "$tmp/log" | sed 's/ $//')
  if [ "$code" != "$2" ]; then echo "FAIL: $1: expected exit $2, got $code"; failed=1; fi
  if [ "$got" != "$3" ]; then echo "FAIL: $1: expected cancelled '$3', got '$got'"; failed=1; fi
}
has() { grep -qF -- "$3" "$tmp/$2" || { echo "FAIL: $1: $2 lacks '$3'"; failed=1; }; }
# calls <case name> <expected number of gh api calls>
calls() { c=$(cat "$tmp/count" 2>/dev/null || echo 0); [ "$c" = "$2" ] || { echo "FAIL: $1: expected $2 gh api calls, got $c"; failed=1; }; }

printf '%s\n' "11 $ci in_progress 5" "12 $ci queued 5" "13 $ci completed 5" "14 .github/workflows/sync.yml in_progress 5" \
  "15 .github/workflows/pr-rules.yml in_progress 5" "16 $ci in_progress 6" "17 $ci in_progress 5,6" "18 $ci in_progress 55" "19 $ci in_progress " > "$tmp/runs"
expect "cancels only unfinished ci.yml runs of this PR" 0 "11 12" 5
has "log line names the job and the run" out "job 'rules' failed in PR #5, so the run 11 of ci.yml"
has "log line says it is not a failure" out "not a failure"
expect "PR 6 is not mixed up with PR 5, 55 or the run of both (5,6)" 0 "16" 6
: > "$tmp/runs"
expect "no run appears" 0 "" 5
has "no run appears" out "no ci.yml run of this PR appeared"
calls "no run appears: bounded" 3
printf '%s\n' "11 $ci in_progress 5" > "$tmp/runs"
FAKE_RUNS_FROM_CALL=3; export FAKE_RUNS_FROM_CALL
expect "the run appears at the 3rd check" 0 "11" 5
calls "the run appears at the 3rd check" 3
FAKE_RUNS_FROM_CALL=4
expect "the run appears too late: bounded, warning" 0 "" 5
has "the run appears too late" out "::warning::"
calls "the run appears too late" 3
unset FAKE_RUNS_FROM_CALL
printf '%s\n' "13 $ci completed 5" > "$tmp/runs"
expect "only a finished run: no waiting, no cancel" 0 "" 5
has "only a finished run" out "no unfinished run"
calls "only a finished run" 1
printf '%s\n' "16 $ci in_progress 6" "17 $ci in_progress 5,6" > "$tmp/runs"
expect "only runs of other PRs: waits, cancels nothing" 0 "" 5
calls "only runs of other PRs" 3
printf '%s\n' "11 $ci in_progress 5" > "$tmp/runs"
FAKE_API_FAIL=1; export FAKE_API_FAIL; expect "list fails: warning, exit 0" 0 "" 5; unset FAKE_API_FAIL
has "list fails" out "cannot list the runs"
FAKE_CANCEL_FAIL=1; export FAKE_CANCEL_FAIL; expect "cancel fails: warning, exit 0" 0 "11" 5; unset FAKE_CANCEL_FAIL
has "cancel fails" out "::warning::"
expect "bad PR number" 1 "" "5;x"
expect "empty PR number" 1 "" ""
sh "$dir/stop-ci-run.sh" rules 5 'abc;touch pwned' > /dev/null 2>&1; code=$?
[ "$code" = 1 ] && [ ! -e pwned ] || { echo "FAIL: bad commit was accepted or executed"; failed=1; }

[ "$failed" = 0 ] && echo "stop-ci-run-test: OK"
exit "$failed"
