# Deploy (section 19). $args[0] - environment: dev, stage or prod.
# dev/stage: CI keeps the built MSIX as a workflow-run artifact (job deploy in ci.yml); nothing to do here.
# prod = publish the draft GitHub Release of $env:TAG (needs GH_TOKEN). Unsigned builds are refused.
$ErrorActionPreference = 'Stop'
$target = $args[0]
if ($target -ne 'prod') { Write-Host "deploy: no $target environment for a desktop app"; exit 0 }
if (-not $env:TAG) { Write-Error 'deploy: TAG is not set'; exit 1 }
$assets = gh release view $env:TAG --json assets --jq '.assets[].name'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($assets -match 'unsigned') { Write-Error "deploy: $env:TAG has an unsigned build - not published"; exit 1 }
gh release edit $env:TAG --draft=false --latest
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
