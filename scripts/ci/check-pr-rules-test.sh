#!/bin/sh
# Table test for check-pr-rules.sh. Each row: expected (ok|bad) | head branch | base branch | title.
dir=$(dirname "$0")
failed=0

while IFS='|' read -r expected head base title; do
  [ -n "$expected" ] || continue
  if sh "$dir/check-pr-rules.sh" "$head" "$base" "$title" >/dev/null 2>&1; then actual=ok; else actual=bad; fi
  if [ "$actual" != "$expected" ]; then
    echo "FAIL: expected $expected, got $actual: $head -> $base : $title"
    failed=1
  fi
done <<'TABLE'
ok|feat/TW-7-Add-form|dev|feat: TW-7 Add form
ok|feat/Add-form|dev|feat: Add form
ok|fix/TW-7-Crash|stage|fix(ui): TW-7 Fix crash
ok|refactor/TW-7-Split|main|style: TW-7 Reformat
ok|ci/TW-3-PR-rules|dev|ci: TW-3 Check PR rules
ok|docs/Update-README|dev|docs: Update README
ok|chore/Update-dependencies|dev|build: Update dependencies
ok|feat/TW-7-Add-form|dev|feat(api)!: TW-7 Change format
bad|feat/TW-7-Add-form|dev|feat: Add form
bad|feat/TW-7-Add-form|dev|feat: TW-8 Add form
bad|feat/Add-form|dev|feat: TW-7 Add form
bad|feat/TW-7-Add-form|dev|wip: TW-7 Draft
bad|feat/TW-7-Add-form|dev|Add form
bad|feat/TW-7-Add-form|dev|added: TW-7 Add form
bad|feat/TW-7-Add-form|dev|feat:TW-7 Add form
bad|feat/TW-7-Add-form|release/v1.0|feat: TW-7 Add form
bad|tmp/TW-7-Add-form|dev|feat: TW-7 Add form
ok|hotfix/v1.2.3-TW-9-Crash|release/v1.2|fix: TW-9 Fix crash
ok|hotfix/v1.2.3-Crash|release/v1.2|fix: Fix crash
bad|hotfix/v1.2.3-TW-9-Crash|dev|fix: TW-9 Fix crash
bad|hotfix/v1.2.3-TW-9-Crash|main|fix: TW-9 Fix crash
bad|hotfix/v1.2.3-TW-9-Crash|release/v1.3|fix: TW-9 Fix crash
bad|hotfix/TW-9-Crash|release/v1.2|fix: TW-9 Fix crash
bad|hotfix/v1.2.3-TW-9-Crash|release/v1.2|fix: Fix crash
ok|revert/TW-7-Add-form-dev|dev|revert: TW-7 Revert add form
ok|revert/TW-7-Add-form-stage|stage|revert: TW-7 Revert add form
bad|revert/TW-7-Add-form-dev|stage|revert: TW-7 Revert add form
bad|revert/TW-7-Add-form|dev|revert: TW-7 Revert add form
ok|chore/Merge-TW-7-Add-form-into-dev|dev|feat: TW-7 Add form
ok|chore/Merge-Add-form-into-stage|stage|fix: Fix form
bad|chore/Merge-TW-7-Add-form-into-main|main|feat: TW-7 Add form
bad|chore/Merge-TW-7-Add-form-into-dev|stage|feat: TW-7 Add form
bad|chore/Merge-TW-7-Add-form-into-dev|dev|feat: TW-8 Add form
bad|chore/Merge-TW-7-Add-form-into-dev|dev|feat: Add form
ok|chore/Sync-main-into-dev-v0.2.0|dev|chore: Sync main into dev
ok|chore/Sync-main-into-stage-v0.2.0|stage|chore: Sync main into stage
bad|chore/Sync-main-into-dev-v0.2.0|stage|chore: Sync main into stage
bad|chore/Sync-main-into-dev-v0.2.0|dev|chore: Sync main into stage
bad|chore/Sync-main-into-dev-v0.2.0|dev|chore: TW-1 Sync main into dev
bad|chore/Sync-main-into-main-v0.2.0|main|chore: Sync main into main
ok|main|dev|chore: Sync main into dev
ok|main|stage|chore: Sync main into stage
bad|main|dev|chore: Sync main into stage
bad|main|main|chore: Sync main into main
bad|main|dev|chore: Sync main into dev now
ok|main|release/v1.2|chore: Merge main into release/v1.2
bad|main|release/v1.2|chore: Sync main into release/v1.2
ok|chore/Release-v1.2.0|main|chore(release): v1.2.0
ok|chore/Release-v1.2.1|release/v1.2|chore(release): v1.2.1
bad|chore/Release-v1.2.1|release/v1.3|chore(release): v1.2.1
bad|chore/Release-v1.2.0|dev|chore(release): v1.2.0
bad|chore/Release-v1.2.0|main|chore(release): v1.2.1
bad|chore/Release-v1.2.0|main|chore: Release v1.2.0
bad|chore/Release-v1.2|main|chore(release): v1.2
ok|release/v1.2|main|chore: Merge release/v1.2 into main
bad|release/v1.2|dev|chore: Merge release/v1.2 into dev
bad|release/v1.2|main|chore: Merge release/v1.3 into main
ok|chore/Setup-repository|main|ci: SETUP-1 Align hooks
bad|chore/Setup-repository|dev|ci: SETUP-1 Align hooks
bad|chore/Setup-repository|main|setup repository
TABLE

[ "$failed" -eq 0 ] && echo "check-pr-rules-test: OK"
exit $failed
