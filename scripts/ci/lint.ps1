# Lint: formatting (whitespace), method length (CODE_STYLE_EN.md section 5), commit identity in automation, script tests (PR rules, CI reviewer, sync, changelog order).
$ErrorActionPreference = 'Stop'
./scripts/ci/check-identity.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$sh = (Get-Command sh -ErrorAction SilentlyContinue).Source
if (-not $sh) { $sh = Join-Path (Split-Path (Split-Path (Get-Command git).Source)) 'bin/sh.exe' }
foreach ($test in 'ci/check-attribution-test.sh', 'ci/check-base-test.sh', 'ci/check-commit-ids-test.sh', 'ci/check-pr-rules-test.sh', 'ci/review-test.sh', 'ci/stop-ci-run-test.sh', 'ci/sync-main-test.sh', 'release/test-changelog.sh') {
  & $sh "scripts/$test"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
dotnet format whitespace ThermoWhisper.slnx --verify-no-changes --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --file scripts/ci/method-length.cs -- .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
