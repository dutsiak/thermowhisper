#!/bin/sh
# Test of check-base.sh in a temporary repository: local-branch mode (no origin) and origin/ mode (a clone).
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.autocrlf GIT_CONFIG_VALUE_0=false
# expect <case name> <expected exit code> <target> <branch> <head>   (runs in the current folder)
expect() {
  sh "$dir/check-base.sh" "$3" "$4" "$5" > /dev/null 2>&1; code=$?
  if [ "$code" != "$2" ]; then echo "FAIL: $mode: $1: expected exit $2, got $code"; failed=1; fi
}
commit() { echo "$1" > "$1.txt"; git add -A; git commit -qm "$1"; }

git init -q -b main "$tmp/work" && cd "$tmp/work" || exit 1
git config user.name test; git config user.email test@example.test
commit m1
git branch dev; git switch -q dev; commit d1
git switch -q -c feat main; commit f1
git switch -q dev; git merge -q --no-ff feat -m "Merge feat into dev"   # dev now holds a merge commit that is not in main
git switch -q main; commit m2
mk() { git switch -q -c "$1" "$2"; commit "x-$(echo "$1" | tr '/' '-')"; }
mk clean main
mk withmain main~1; git merge -q --no-ff main -m "Merge main into withmain"
mk withdev main; git merge -q --no-ff dev -m "Merge dev into withdev"
mk fromdev dev
for b in chore/Merge-x chore/Sync-x revert/x chore/Release-v1.0.0 hotfix/x release/v1.0; do mk "$b" dev; done
git switch -q main

run_cases() {   # $1 - prefix of branch refs ("" or "origin/"), $2 - mode name
  mode=$2; p=$1
  expect "clean branch from main" 0 dev clean "${p}clean"
  expect "main merged in" 0 dev withmain "${p}withmain"
  expect "dev merged in (dev holds a merge)" 1 dev withdev "${p}withdev"
  expect "created from dev" 1 dev fromdev "${p}fromdev"
  expect "chore/Merge-" 0 dev chore/Merge-x "${p}chore/Merge-x"
  expect "chore/Sync-" 0 dev chore/Sync-x "${p}chore/Sync-x"
  expect "revert/" 0 dev revert/x "${p}revert/x"
  expect "chore/Release- into main" 0 main chore/Release-v1.0.0 "${p}chore/Release-v1.0.0"
  expect "chore/Release- into release/*" 0 release/v1.0 chore/Release-v1.0.0 "${p}chore/Release-v1.0.0"
  expect "chore/Release- into dev is not exempt" 1 dev chore/Release-v1.0.0 "${p}chore/Release-v1.0.0"
  expect "hotfix/ into release/*" 0 release/v1.0 hotfix/x "${p}hotfix/x"
  expect "hotfix/ into dev is not exempt" 1 dev hotfix/x "${p}hotfix/x"
  expect "release/* into main" 0 main release/v1.0 "${p}release/v1.0"
  expect "release/* into dev is not exempt" 1 dev release/v1.0 "${p}release/v1.0"
  expect "PR from main" 0 dev main "${p}main"
  expect "unknown head" 1 dev clean "${p}no-such-branch"
}
run_cases "" "local branches"
git clone -q "$tmp/work" "$tmp/clone" && cd "$tmp/clone" || exit 1
git fetch -q origin
run_cases "origin/" "origin/ refs"

[ "$failed" = 0 ] && echo "check-base-test: OK"
exit "$failed"
