#!/bin/sh
# Test of sync-needed.sh in a temporary repository: local-branch mode (no origin) and origin/ mode (a clone).
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.autocrlf GIT_CONFIG_VALUE_0=false
# expect <case name> <expected exit code> <expected first line> <arguments of the script...>   (runs in the current folder)
expect() {
  name=$1; want=$2; first=$3; shift 3
  sh "$dir/sync-needed.sh" "$@" > "$tmp/out" 2> "$tmp/err"; code=$?
  if [ "$code" != "$want" ]; then echo "FAIL: $mode: $name: expected exit $want, got $code"; failed=1; fi
  if [ -n "$first" ] && [ "$(head -n 1 "$tmp/out")" != "$first" ]; then echo "FAIL: $mode: $name: expected '$first', got '$(head -n 1 "$tmp/out")'"; failed=1; fi
}
commit() { echo "$1" > "$1.txt"; git add -A; git commit -qm "$1"; }

# A criss-cross: p and q are cut from main, dev merges both, task merges both: two merge bases (noise). main has one more commit that dev lacks.
git init -q -b main "$tmp/work" && cd "$tmp/work" || exit 1
git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
commit a
git switch -q -c p; commit p
git switch -q -c q main; commit q
git switch -q -c dev p; git merge -q --no-ff q -m "dev merges q"
git switch -q -c task q; git merge -q --no-ff p -m "task merges p"; commit t
git switch -q main; commit m
git switch -q -c quiet dev; commit x                                   # one merge base with dev
git switch -q -c dev-with-main dev; git merge -q --no-ff main -m "Merge main into dev-with-main"
git branch stage dev; git branch stage-with-main dev-with-main
git branch 'x;touch${IFS}pwned' task; git branch 'y$(touch${IFS}pwned)`id`' task
git switch -q main

run_cases() {   # $1 - prefix of branch refs ("" or "origin/"), $2 - mode name
  mode=$2; p=$1
  expect "two bases, main not in dev" 0 "NEEDED" dev "${p}task"
  expect "two bases, main not in stage" 0 "NEEDED" stage "${p}task"
  expect "one merge base" 0 "NOT NEEDED" dev "${p}quiet"
  expect "branch made from main, one base" 0 "NOT NEEDED" dev main
  expect "reason line is printed" 0 "NEEDED" dev "${p}task"
  [ "$(wc -l < "$tmp/out" | tr -d ' ')" = 2 ] || { echo "FAIL: $mode: the answer must have two lines"; failed=1; }
  expect "shell characters in the branch name: not evaluated" 0 "NEEDED" dev "${p}x;touch\${IFS}pwned"
  expect "command substitution in the branch name: not evaluated" 0 "NEEDED" dev "${p}y\$(touch\${IFS}pwned)\`id\`"
  [ ! -e pwned ] || { echo "FAIL: $mode: a branch name was executed"; failed=1; rm -f pwned; }
  expect "bad target main" 2 "" main "${p}task"
  expect "empty target" 2 "" "" "${p}task"
  expect "bad branch (option)" 2 "" dev "--all"
  expect "missing branch" 2 "" dev "${p}no-such-branch"
}
run_cases "" "local branches"
# main already inside the target: NOT NEEDED even with two bases (local mode only: the target is a local branch here)
mode="local branches"
git branch -f dev dev-with-main; git branch -f stage stage-with-main
expect "main already in dev" 0 "NOT NEEDED" dev task
expect "main already in stage" 0 "NOT NEEDED" stage task
git switch -q task; expect "default branch is HEAD" 0 "NOT NEEDED" dev; git switch -q main
git branch -f dev quiet~1; git branch -f stage quiet~1                  # back to the state without main in dev and stage
git clone -q "$tmp/work" "$tmp/clone" && cd "$tmp/clone" || exit 1
git fetch -q origin
run_cases "origin/" "origin/ refs"

[ "$failed" = 0 ] && echo "sync-needed-test: OK"
exit "$failed"
