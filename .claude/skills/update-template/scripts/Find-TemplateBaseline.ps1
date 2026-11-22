<#
.SYNOPSIS
    Ranks template commits by how many of their files this repository still has unchanged,
    as an inference aid for an adopted tooling baseline, not proof of repository generation.

.DESCRIPTION
    For repositories without a recorded `commit` whose first commit's tree matches no template commit:
    squashed history, or a repository created from an earlier copy of the template. Compares Git blob
    hashes path by path (template names in paths mapped to this repository's), so only files without
    template tokens can match; that is enough to rank. Lists the best commits, highest score first.
    Changes nothing.

.EXAMPLE
    ./.claude/skills/update-template/scripts/Find-TemplateBaseline.ps1 -Template $tmpl
#>
[CmdletBinding()]
param (
    # Clone of the template repository (bare is fine)
    [Parameter(Mandatory)]
    [string]$Template,

    # Template branch to search
    [string]$Branch = 'main',

    # Number of best commits to list
    [int]$Top = 5
)

$ErrorActionPreference = 'Stop'

$root = (git rev-parse --show-toplevel).Trim()
$setup = (Get-Content (Join-Path $root '.config/template.json') -Raw | ConvertFrom-Json).setup
if (-not $setup) { throw '.config/template.json has no setup choices; fill them first (update-template step 2).' }
$dotNet = switch ($setup.family) {
    'Kentico' { "Kentico.Xperience.$($setup.name)" }
    'Labs' { "Kentico.Xperience.Labs.$($setup.name)" }
    'Community' { "XperienceCommunity.$($setup.name)" }
}

# This repository's committed files: path -> blob
$local = @{}
git -C $root ls-tree -r HEAD | ForEach-Object {
    $meta, $path = $_ -split "`t", 2
    $local[$path] = ($meta -split ' ')[2]
}
$firstDate = [datetimeoffset]::Parse((git -C $root log --reverse --format=%aI | Select-Object -First 1))

$scores = git -C $Template log --format='%H %aI %s' $Branch | ForEach-Object {
    $sha, $date, $subject = $_ -split ' ', 3
    $same = 0; $differ = 0
    git -C $Template ls-tree -r $sha | ForEach-Object {
        $meta, $path = $_ -split "`t", 2
        if ($path -match '/App_Data/CIRepository/') { return }
        if ($setup.family -eq 'Kentico' -and $setup.name -eq 'Lucene' -and
            $path.StartsWith('src/Kentico.Xperience.RepoTemplate/')) {
            $path = $path.Replace('src/Kentico.Xperience.RepoTemplate/', "src/$dotNet.Core/").
                Replace('Kentico.Xperience.RepoTemplate.csproj', "$dotNet.Core.csproj")
        }
        $path = $path.Replace('Kentico.Xperience.RepoTemplate', $dotNet).Replace('RepoTemplate', $setup.name)
        if ($local.ContainsKey($path)) {
            if ($local[$path] -eq ($meta -split ' ')[2]) { $same++ } else { $differ++ }
        }
    }
    [pscustomobject]@{
        Commit       = $sha.Substring(0, 9)
        Same         = $same
        Different    = $differ
        Date         = $date
        AfterRepo    = [datetimeoffset]::Parse($date) -gt $firstDate
        Subject      = $subject
    }
}

Write-Host "This repository's first commit: $($firstDate.ToString('u'))"
Write-Host 'Similarity is inference only; it does not prove template generation or completed adoption.'
$scores | Sort-Object @{ Expression = 'Same'; Descending = $true }, Different | Select-Object -First $Top | Format-Table -AutoSize
