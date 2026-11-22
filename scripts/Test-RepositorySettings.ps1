<#
.SYNOPSIS
    Checks the GitHub repository settings listed in "Repository settings" in docs/Contributing-Setup.md.

.DESCRIPTION
    Read-only: uses the GitHub CLI (gh), signed in as a repository admin (collaborator and team lists need
    admin access). Prints one row per setting and exits 1 when any check fails. Settings the API does not
    expose are listed as "Check in UI".

.EXAMPLE
    ./scripts/Test-RepositorySettings.ps1
    ./scripts/Test-RepositorySettings.ps1 -Repository Kentico/xperience-by-kentico-cool-thing
#>
[CmdletBinding()]
param (
    # owner/name; defaults to the repository of the current folder
    [string]$Repository = $(gh repo view --json nameWithOwner --jq .nameWithOwner)
)

$ErrorActionPreference = 'Stop'

if (-not $Repository) { throw 'No repository: pass -Repository owner/name or run inside a clone.' }
$api = "repos/$Repository"
$results = [System.Collections.Generic.List[object]]::new()

function Add-Result([string]$Setting, [string]$Status, [string]$Evidence) {
    $results.Add([pscustomobject]@{ Setting = $Setting; Status = $Status; Evidence = $Evidence })
}

# Returns the parsed JSON, or $null when the endpoint answers with an error (404: off or no access)
function Invoke-GitHubApi([string]$Path, [switch]$Paginate) {
    if ($Paginate) {
        $output = gh api --method GET --paginate --slurp $Path 2>$null
    } else {
        $output = gh api --method GET $Path 2>$null
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Cannot inspect $Path (gh exit $LASTEXITCODE): disabled, unavailable, or insufficient access. Verify in GitHub settings."
        return $null
    }
    if (-not $output) { return @{} }
    # -NoEnumerate (twice): an empty list stays a list instead of $null
    if ($Paginate) {
        $pages = $output | ConvertFrom-Json -NoEnumerate
        $items = [System.Collections.Generic.List[object]]::new()
        foreach ($page in $pages) {
            foreach ($item in $page) {
                $items.Add($item)
            }
        }
        Write-Output -NoEnumerate $items.ToArray()
    } else {
        Write-Output -NoEnumerate ($output | ConvertFrom-Json -NoEnumerate)
    }
}

$repo = Invoke-GitHubApi $api
if (-not $repo) { throw "Repository $Repository not found, or gh is not signed in with access to it." }
$teams = Invoke-GitHubApi "$api/teams" -Paginate
$owner = $repo.owner.login

# CODEOWNERS: GitHub validates the file; an owner it cannot resolve is an error
$codeowners = Invoke-GitHubApi "$api/codeowners/errors"
$owners = @()
if ($null -eq $codeowners) {
    Add-Result '.github/CODEOWNERS lists the owners' 'Check in UI' 'CODEOWNERS endpoint unavailable; absence not confirmed'
} else {
    $file = Invoke-GitHubApi "$api/contents/.github/CODEOWNERS"
    if (-not $file) { throw 'Cannot read CODEOWNERS; its owners cannot be validated.' }
    $text = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($file.content))
    $owners = @([regex]::Matches($text, '(?m)^\*\s+(.+)$') | ForEach-Object { $_.Groups[1].Value -split '\s+' } |
        Where-Object { $_ -like '@*' })
    if ($codeowners.errors.Count -gt 0) {
        # "Unknown owner" also means a team exists but lacks write access to this repository
        $details = $codeowners.errors | ForEach-Object {
            "line $($_.line): $($_.kind)$(if ($_.kind -eq 'Unknown owner') { ' (owner missing, or a team without write access)' })"
        }
        Add-Result '.github/CODEOWNERS lists the owners' 'Fail' ($details -join '; ')
    } elseif ($owners.Count -eq 0) {
        Add-Result '.github/CODEOWNERS lists the owners' 'Fail' 'No "* @owner" line'
    } else {
        Add-Result '.github/CODEOWNERS lists the owners' 'Pass' ($owners -join ', ')
    }
}

# Owner teams (@org/team) need the Admin role; user owners are not checked
$ownerTeams = @($owners | Where-Object { $_ -like "@$owner/*" } | ForEach-Object { $_.Split('/')[1] })
if ($ownerTeams.Count -eq 0 -or $null -eq $teams) {
    Add-Result 'Owners added as Admins' 'Check in UI' 'No team owner in CODEOWNERS'
} else {
    $missing = @($ownerTeams | Where-Object { $slug = $_; -not ($teams | Where-Object { $_.slug -eq $slug -and $_.permission -eq 'admin' }) })
    $access = ($teams | ForEach-Object { "$($_.slug) ($($_.permission))" }) -join ', '
    if ($missing.Count -gt 0) {
        Add-Result 'Owners added as Admins' 'Fail' "Not admin: $($missing -join ', '). Teams: $access"
    } else {
        Add-Result 'Owners added as Admins' 'Pass' $access
    }
}

$outside = Invoke-GitHubApi "$api/collaborators?affiliation=outside"
if ($null -eq $outside) {
    Add-Result 'No outside collaborators added directly' 'Check in UI' 'Needs admin access'
} elseif (@($outside).Count -gt 0) {
    Add-Result 'No outside collaborators added directly' 'Fail' ((@($outside) | ForEach-Object login) -join ', ')
} else {
    Add-Result 'No outside collaborators added directly' 'Pass' 'None'
}

Add-Result 'Automatically delete head branches' $(if ($repo.delete_branch_on_merge) { 'Pass' } else { 'Fail' }) "delete_branch_on_merge: $($repo.delete_branch_on_merge.ToString().ToLower())"

# Rulesets (.github/rulesets): the rules in effect on the default branch
$branch = $repo.default_branch
$rules = Invoke-GitHubApi "$api/rules/branches/$branch"
$types = @($rules | ForEach-Object type)
$checks = @($rules | Where-Object type -eq 'required_status_checks' |
    ForEach-Object { $_.parameters.required_status_checks.context })
$expected = @{
    'deletion'               = 'deletion blocked'
    'non_fast_forward'       = 'force pushes blocked'
    'required_status_checks' = 'required checks'
    'merge_queue'            = 'merge queue'
}
$missingRules = @($expected.Keys | Where-Object { $_ -notin $types } | ForEach-Object { $expected[$_] })
$missingChecks = @('Build and Test', 'E2E Tests' | Where-Object { $_ -notin $checks })
if ($null -eq $rules) {
    Add-Result "Rulesets imported ($branch)" 'Check in UI' 'Effective rules endpoint unavailable'
} elseif ($missingRules.Count -gt 0 -or $missingChecks.Count -gt 0) {
    $missingText = @($missingRules) + @($missingChecks | ForEach-Object { "check '$_'" })
    Add-Result "Rulesets imported ($branch)" 'Fail' "Missing: $($missingText -join ', ')"
} else {
    $method = ($rules | Where-Object type -eq 'merge_queue').parameters.merge_method
    Add-Result "Rulesets imported ($branch)" 'Pass' "checks: $($checks -join ', '); merge queue: $method"
}

# The SBOM endpoint answers only while the dependency graph is on and has found manifests
$sbom = Invoke-GitHubApi "$api/dependency-graph/sbom"
if ($sbom) {
    Add-Result 'Dependency graph' 'Pass' "$(@($sbom.sbom.packages).Count) packages in SBOM"
} else {
    Add-Result 'Dependency graph' 'Check in UI' 'No accessible SBOM; verify Settings > Advanced Security'
}

$null = gh api --method GET "$api/vulnerability-alerts" 2>$null
Add-Result 'Dependabot alerts' $(if ($LASTEXITCODE -eq 0) { 'Pass' } else { 'Check in UI' }) $(if ($LASTEXITCODE -eq 0) { 'Enabled' } else { 'Disabled or inaccessible; not confirmed' })

$fixes = Invoke-GitHubApi "$api/automated-security-fixes"
$fixesOn = $fixes -and $fixes.enabled -and -not $fixes.paused
Add-Result 'Dependabot security updates' $(if ($null -eq $fixes) { 'Check in UI' } elseif ($fixesOn) { 'Pass' } else { 'Fail' }) $(if (-not $fixes) { 'Unknown' } elseif ($fixes.paused) { 'Paused' } elseif ($fixes.enabled) { 'Enabled' } else { 'Disabled' })

$ui = "Settings > Advanced Security > Dependabot"
Add-Result 'Grouped security updates' 'Check in UI' "$ui (needs security updates on)"
Add-Result 'Auto-dismiss low-impact alerts for dev dependencies' 'Check in UI' "$ui > Dependabot auto-triage rules"

Write-Host "Repository settings: $Repository"
$results | Format-Table -AutoSize -Wrap | Out-String -Width 200 | Write-Host

$failed = @($results | Where-Object Status -eq 'Fail').Count
if ($failed -gt 0) {
    Write-Host "$failed failed. See ""Repository settings"" in docs/Contributing-Setup.md."
    exit 1
}
$unverified = @($results | Where-Object Status -eq 'Check in UI').Count
if ($unverified -gt 0) {
    Write-Host "$unverified settings require manual verification; this is not a complete settings validation."
    exit 2
}
