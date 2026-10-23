# Lint: formatting (whitespace), method length (CODE_STYLE_EN.md section 5), commit identity in automation.
$ErrorActionPreference = 'Stop'
./scripts/ci/check-identity.ps1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet format whitespace ThermoWhisper.slnx --verify-no-changes --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --file scripts/ci/method-length.cs -- .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
