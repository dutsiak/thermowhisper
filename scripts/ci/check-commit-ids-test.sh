#!/bin/sh
# Test of check-commit-ids.sh in a temporary repository: local-branch mode (no origin) and origin/ mode (a clone).
dir=$(cd "$(dirname "$0")" && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failed=0
n=0
export GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.autocrlf GIT_CONFIG_VALUE_0=false
# expect <case name> <expected exit code> <branch> <head>   (target is dev; runs in the current folder; output goes to $tmp/out and $tmp/err)
expect() {
  sh "$dir/check-commit-ids.sh" dev "$3" "$4" > "$tmp/out" 2> "$tmp/err"; code=$?
  if [ "$code" != "$2" ]; then echo "FAIL: $mode: $1: expected exit $2, got $code"; failed=1; fi
}
# has <case name> <file> <text>: the file in $tmp contains the text
has() { grep -qF -- "$3" "$tmp/$2" || { echo "FAIL: $mode: $1: $2 lacks '$3'"; failed=1; }; }
cm() { n=$((n + 1)); echo "$n" > "f$n.txt"; git add -A; git commit -qm "$1"; }
# mk <branch> <start> <subject>...: new branch with one commit per subject
mk() { git switch -q -c "$1" "$2"; shift 2; for s in "$@"; do cm "$s"; done; }

git init -q -b main "$tmp/work" && cd "$tmp/work" || exit 1
git config user.name "github-actions[bot]"; git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
cm "chore: root"
git branch dev
mk feat/TW-7-good main "feat: TW-7 x" "fix(ui): TW-7 x" "wip: TW-7 x" "refactor(core)!: TW-7 x"
mk feat/TW-7-no-id main "feat: TW-7 x" "feat: Add form"
mk feat/TW-7-empty main "feat: TW-7 x"; git commit -q --allow-empty -m "ci: Trigger checks"
mk feat/TW-7-other-id main "feat: TW-8 x"
mk feat/TW-7-longer-id main "feat: TW-77 x"
mk feat/TW-7-id-late main "feat: Add TW-7 form"
mk feat/TW-7-no-desc main "feat: TW-7"
mk feat/TW-7-middle main "feat: TW-7 a" "feat: no id" "feat: TW-7 b"
mk feat/TW-7-shell main 'feat: TW-7 $(touch pwned) `id` ; rm x'
mk feat/TW-7-shell-bad main 'feat: $(touch pwned) `id` ; rm x'
for b in docs/Update-README ci/Add-check-base chore/Update-dependencies chore/Setup-repository \
         release/v1.0 chore/Release-v1.0.0 chore/Sync-main-into-dev-v1.0.0 chore/Merge-TW-7-Add-form-into-dev revert/TW-7-Add-form-dev hotfix/v1.0.1-TW-7-Crash; do
  mk "$b" main "no id here"
done
mk feat/TW-7-withmain main "feat: TW-7 x"
git switch -q main; cm "chore: Update notes"          # main moves on: a commit with no ID
git switch -q feat/TW-7-withmain; git merge -q --no-ff main -m "Merge main into feat/TW-7-withmain"
git switch -q dev; cm "chore: only in dev"            # the target branch holds a commit with no ID
git switch -q main

run_cases() {   # $1 - prefix of branch refs ("" or "origin/"), $2 - mode name
  mode=$2; p=$1
  expect "all good subjects" 0 feat/TW-7-good "${p}feat/TW-7-good"
  expect "one commit without ID" 1 feat/TW-7-no-id "${p}feat/TW-7-no-id"
  has "one commit without ID" err "feat: Add form"
  has "one commit without ID" err "git merge --squash"
  expect "empty Trigger checks commit" 1 feat/TW-7-empty "${p}feat/TW-7-empty"
  has "empty Trigger checks commit" err "ci: Trigger checks"
  expect "other ID" 1 feat/TW-7-other-id "${p}feat/TW-7-other-id"
  expect "longer ID (TW-77 in TW-7)" 1 feat/TW-7-longer-id "${p}feat/TW-7-longer-id"
  expect "ID not right after the type" 1 feat/TW-7-id-late "${p}feat/TW-7-id-late"
  expect "ID without description" 1 feat/TW-7-no-desc "${p}feat/TW-7-no-desc"
  expect "bad commit in the middle" 1 feat/TW-7-middle "${p}feat/TW-7-middle"
  has "bad commit in the middle" err "feat: no id"
  expect "main merged in, main has commits without ID" 0 feat/TW-7-withmain "${p}feat/TW-7-withmain"
  expect "target branch has an extra commit without ID" 0 feat/TW-7-good "${p}feat/TW-7-good"
  for b in docs/Update-README ci/Add-check-base chore/Update-dependencies chore/Setup-repository; do
    expect "no ID in the name: $b" 0 "$b" "${p}$b"
    has "no ID in the name: $b" out "skipped"
  done
  for b in release/v1.0 chore/Release-v1.0.0 chore/Sync-main-into-dev-v1.0.0 chore/Merge-TW-7-Add-form-into-dev revert/TW-7-Add-form-dev hotfix/v1.0.1-TW-7-Crash; do
    expect "exempt name: $b" 0 "$b" "${p}$b"
    has "exempt name: $b" out "skipped"
  done
  expect "exempt name: main" 0 main "${p}main"
  expect "unknown head" 1 feat/TW-7-good "${p}no-such-branch"
  has "unknown head" err "not found"
  expect "shell characters in a good subject" 0 feat/TW-7-shell "${p}feat/TW-7-shell"
  expect "shell characters in a bad subject" 1 feat/TW-7-shell-bad "${p}feat/TW-7-shell-bad"
  has "shell characters in a bad subject" err '$(touch pwned) `id` ; rm x'
  [ ! -e pwned ] || { echo "FAIL: $mode: a subject was executed"; failed=1; rm -f pwned; }
}
run_cases "" "local branches"
git clone -q "$tmp/work" "$tmp/clone" && cd "$tmp/clone" || exit 1
git fetch -q origin
run_cases "origin/" "origin/ refs"

[ "$failed" = 0 ] && echo "check-commit-ids-test: OK"
exit "$failed"
