# Build all projects (CA1822 and warnings as errors apply) and run the *.Tests projects.
$ErrorActionPreference = 'Stop'
dotnet build ThermoWhisper.slnx -c Release -p:WindowsPackageType=None --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
foreach ($p in Get-ChildItem *.Tests\*.Tests.csproj) {
  dotnet test $p.FullName -c Release --no-build --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
