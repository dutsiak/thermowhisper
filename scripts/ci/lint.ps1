# Lint: formatting (whitespace) and method length (CODE_STYLE_EN.md section 5).
$ErrorActionPreference = 'Stop'
dotnet format whitespace ThermoWhisper.slnx --verify-no-changes --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --file scripts/ci/method-length.cs -- .
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
