# Release build: self-contained MSIX into dist/ (section 13). Version comes from the .csproj
# (set by scripts/release/set-version.sh). Signing is done only by SignPath in CI.
# UNSIGNED_TEST=true (dev/stage builds): adds the Microsoft test OID to Identity Publisher for this build only,
# so the unsigned MSIX can be installed with Add-AppxPackage -AllowUnsigned (docs/architecture.md section 10).
# The manifest file is restored afterwards; release builds never use it.
$ErrorActionPreference = 'Stop'
$tag = if ($env:TAG) { $env:TAG } else { 'local' }
$manifest = 'ThermoWhisper/Platforms/Windows/Package.appxmanifest'
$testOid = 'OID.2.25.311729368913984317654407730594956997722=1'
$original = Get-Content $manifest -Raw
$suffix = ''
try {
  if ($env:UNSIGNED_TEST -eq 'true') {
    $patched = $original -replace '(<Identity\b[^>]*\bPublisher=")([^"]*)"', ('$1$2, ' + $testOid + '"')
    if ($patched -eq $original) { Write-Error 'build: Identity Publisher not found in the manifest'; exit 1 }
    Set-Content $manifest $patched -NoNewline
    $suffix = '-unsigned-test'
  }
  dotnet publish ThermoWhisper/ThermoWhisper.csproj -c Release -f net10.0-windows10.0.19041.0 `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=false -p:AppxPackageDir="$PWD/out/" --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
  Set-Content $manifest $original -NoNewline
}
$msix = Get-ChildItem out -Recurse -Filter *.msix | Select-Object -First 1
if (-not $msix) { Write-Error 'No .msix produced'; exit 1 }
New-Item -ItemType Directory dist -Force | Out-Null
Copy-Item $msix.FullName "dist\ThermoWhisper-$tag-x64$suffix.msix"
