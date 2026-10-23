# Guard: automation must commit only as an allowed identity (TW-33).
# Fails when a tracked workflow, script or hook sets a git user.name / user.email that is not allowed,
# or writes a Co-authored-by line.
$ErrorActionPreference = 'Stop'
$allowedEmails = @(
    '41898282+github-actions[bot]@users.noreply.github.com',  # GitHub Actions bot
    '131253149+mdutsiak@users.noreply.github.com'             # repository owner
)
$allowedDomains = @()   # domains the project owns (none yet)
$allowedNames = @('github-actions[bot]', 'MDutsiak')

$self = 'scripts/ci/check-identity.ps1'
$files = git ls-files -- .github scripts .githooks | Where-Object { $_ -ne $self -and (Test-Path -LiteralPath $_ -PathType Leaf) }
$errors = @()
foreach ($file in $files) {
    $n = 0
    foreach ($line in Get-Content -LiteralPath $file) {
        $n++
        if ($line -match '(?i)co-authored-by') { $errors += "${file}:${n}: Co-authored-by must not be written by automation" }
        if ($line -match '(?i)user\.email[\s=]+["'']?([^"''\s]+)') {
            $email = $Matches[1]
            $domain = ($email -split '@')[-1]
            if (($allowedEmails -cnotcontains $email) -and ($allowedDomains -notcontains $domain)) {
                $errors += "${file}:${n}: git user.email '$email' is not allowed"
            }
        }
        if ($line -match '(?i)user\.name[\s=]+(?:"([^"]*)"|''([^'']*)''|(\S+))') {
            $name = @($Matches[1], $Matches[2], $Matches[3] | Where-Object { $_ })[0]
            if ($allowedNames -cnotcontains $name) { $errors += "${file}:${n}: git user.name '$name' is not allowed" }
        }
        if ($line -match '(?i)GIT_(AUTHOR|COMMITTER)_(NAME|EMAIL)|--author[= ]') {
            $errors += "${file}:${n}: author/committer override is not allowed, use the configured identity"
        }
    }
}
if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    Write-Error "Identity check failed. Allowed: github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com> or the owner. Never invent a noreply address (TW-33)." -ErrorAction Continue
    exit 1
}
