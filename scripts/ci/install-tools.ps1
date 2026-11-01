# Heavy tools check (GIT_STRATEGY_EN.md section 13). The windows-latest image already has the
# MAUI Windows workload (measured once: installing it again took 3-4 min and added nothing we use),
# so nothing is installed here; the script only fails early with a clear message if the image lost it.
$ErrorActionPreference = 'Stop'
$list = dotnet workload list | Out-String
if ($LASTEXITCODE -ne 0) { throw "install-tools: 'dotnet workload list' failed.`n$list" }
if ($list -notmatch '(?m)^maui-windows\s') {
  throw "install-tools: workload maui-windows is missing on this runner image. Add 'dotnet workload install maui-windows' here (cost: 3-4 min per run) or pin an image that has it.`n$list"
}
Write-Host 'install-tools: maui-windows workload present'
