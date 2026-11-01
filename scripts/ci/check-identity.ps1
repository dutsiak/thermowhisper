# Guard: automation must commit only as an allowed identity.
# Fails when a tracked workflow, script or hook sets a git user.name / user.email that is not allowed,
# or writes a Co-authored-by line.
# Limits: text scan of tracked files under .github, scripts, .githooks only; a non-literal value
# (user.name "$(whoami)", "$VAR") is reported as not allowed on purpose (use the literal bot identity).
# $allowedDomains is empty by PO decision; fill it only when the project owns a domain.
$ErrorActionPreference = 'Stop'
$allowedEmails = @(
    '41898282+github-actions[bot]@users.noreply.github.com',  # GitHub Actions bot
    '131253149+mdutsiak@users.noreply.github.com'             # repository owner
)
$allowedDomains = @()   # domains the project owns (none yet)
$allowedNames = @('github-actions[bot]', 'MDutsiak')

$selfFiles = @('scripts/ci/check-identity.ps1', 'scripts/ci/check-authors.sh', 'scripts/ci/check-attribution.sh', 'scripts/ci/check-attribution-test.sh')   # these read or test for Co-authored-by, never write it into a commit
$files = git ls-files -- .github scripts .githooks | Where-Object { $selfFiles -notcontains $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) }
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
    Write-Error "Identity check failed. Allowed: github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com> or the owner. Never invent a noreply address." -ErrorAction Continue
    exit 1
}
