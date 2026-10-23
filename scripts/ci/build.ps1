# Release build: self-contained MSIX into dist/ (section 13). Version comes from the .csproj
# (set by scripts/release/set-version.sh). Signing is done only by SignPath in CI.
# UNSIGNED_TEST=true (dev/stage builds): adds the Microsoft test OID to Identity Publisher for this build only,
# so the unsigned MSIX can be installed with Add-AppxPackage -AllowUnsigned (docs/architecture.md section 10).
# The manifest file is restored afterwards; release builds never use it.
# Pre-1.0 tags (v0.x.y) are built the same way (installable unsigned test package) until SignPath exists (v1.0.0).
# BUILD_DRY_RUN=true: only print the decision and exit (verifies the tag logic without a real tag or build).
# In GitHub Actions the result goes to GITHUB_OUTPUT: msix (path) and unsigned_test (true/false).
$ErrorActionPreference = 'Stop'
$tag = if ($env:TAG) { $env:TAG } else { 'local' }
$unsignedTest = ($env:UNSIGNED_TEST -eq 'true') -or ($tag -match '^v0\.\d+\.\d+')
if ($env:BUILD_DRY_RUN -eq 'true') { Write-Host "build: tag=$tag unsigned_test=$unsignedTest"; exit 0 }
$manifest = 'ThermoWhisper/Platforms/Windows/Package.appxmanifest'
$testOid = 'OID.2.25.311729368913984317654407730594956997722=1'
$original = Get-Content $manifest -Raw
$suffix = ''
try {
  if ($unsignedTest) {
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
$target = "dist/ThermoWhisper-$tag-x64$suffix.msix"
Copy-Item $msix.FullName $target
if ($env:GITHUB_OUTPUT) {
  Add-Content $env:GITHUB_OUTPUT "msix=$target"
  Add-Content $env:GITHUB_OUTPUT "unsigned_test=$(([string]$unsignedTest).ToLower())"
}
