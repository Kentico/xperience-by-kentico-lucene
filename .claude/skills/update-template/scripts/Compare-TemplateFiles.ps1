<#
.SYNOPSIS
    Renders the template files changed between two template commits with this repository's names,
    and merges each change into this repository's copy, in a folder outside the repository.

.DESCRIPTION
    For each file changed between -Base and -Target in the template clone:
    - renders both versions as setup-repo would for this repository (tokens from the `setup` choices
      in .config/template.json, template:<tag> blocks kept or removed, template-only README and
      AGENTS.md sections removed)
    - three-way merges the change into this repository's file (git merge-file), so local changes
      such as ports and secrets are kept
    Writes <path>.base, <path>.target, and <path>.merged under -Out, and lists each file's status.
    Changes nothing in the repository unless -Apply is given.

    Status values:
      new             in the target only; .merged is the rendered target
      merged          applies cleanly; .merged holds the result
      conflict        overlaps a local change; .merged has conflict markers to resolve by hand
      applied         this repository already matches the target
      no-change       the change is only in parts setup-repo removes (template-only sections and blocks)
      deleted         removed from the template; the local file still exists
      missing         changed in the template, but this repository removed the file
      not-applicable  belongs to a part this repository's setup removed (admin, Azure, publishing)
      template-only   serves only the template repository

.EXAMPLE
    ./.claude/skills/update-template/scripts/Compare-TemplateFiles.ps1 -Template $tmpl -Base <sha> -Target v1.1.0
    ./.claude/skills/update-template/scripts/Compare-TemplateFiles.ps1 -Template $tmpl -Base <sha> -Target v1.1.0 -Apply -Paths .github/dependabot.yml, tests/e2e/.npmrc
#>
[CmdletBinding()]
param (
    # Clone of the template repository (bare is fine)
    [Parameter(Mandatory)]
    [string]$Template,

    # Template commit or tag this repository matches
    [Parameter(Mandatory)]
    [string]$Base,

    # Template commit or tag to update to
    [Parameter(Mandatory)]
    [string]$Target,

    # Output folder, outside the repository. Defaults to a new temporary folder
    [string]$Out,

    # Limit to these paths (this repository's or the template's spelling)
    [string[]]$Paths,

    # Write the "new" and "merged" results for -Paths into the repository. Needs -Paths
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'

if ($Apply -and -not $Paths) { throw '-Apply needs -Paths: apply only the changes you decided to adopt.' }

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) { throw 'Run from inside the repository.' }
$root = [IO.Path]::GetFullPath($root.Trim())
$Template = (Resolve-Path $Template).Path
if (-not $Out) { $Out = Join-Path ([IO.Path]::GetTempPath()) "template-compare-$(Get-Random)" }
$Out = [IO.Path]::GetFullPath($Out)
if ($Out.Equals($root, [StringComparison]::OrdinalIgnoreCase) -or
    $Out.StartsWith("$root$([IO.Path]::DirectorySeparatorChar)", [StringComparison]::OrdinalIgnoreCase)) {
    throw '-Out must be outside the repository.'
}
foreach ($directory in '_base', '_target') {
    if (Test-Path -LiteralPath (Join-Path $Out $directory)) { throw "-Out already contains $directory; choose a fresh output folder." }
}
New-Item -ItemType Directory -Force $Out | Out-Null

$setup = (Get-Content (Join-Path $root '.config/template.json') -Raw | ConvertFrom-Json).setup
if (-not $setup) { throw '.config/template.json has no setup choices; fill them first (update-template step 2).' }

# Names, as setup-repo's Rename-Template.ps1 derives them. Keep in step with that script.
$name = $setup.name
$kebab = ($name -creplace '(?<=[a-z])(?=[A-Z])|(?<=[A-Z0-9])(?=[A-Z][a-z])', '-').ToLowerInvariant()
$names = switch ($setup.family) {
    'Kentico' { @{ DotNet = "Kentico.Xperience.$name"; AdminOrg = 'kentico'; AdminProject = "xperience-$kebab-admin"; Prefix = 'Xperience by Kentico' } }
    'Labs' { @{ DotNet = "Kentico.Xperience.Labs.$name"; AdminOrg = 'kentico'; AdminProject = "xperience-labs-$kebab-admin"; Prefix = 'Xperience by Kentico Labs' } }
    'Community' { @{ DotNet = "XperienceCommunity.$name"; AdminOrg = 'xperience'; AdminProject = "community-$kebab-admin"; Prefix = 'Xperience Community' } }
    default { throw "Unknown family '$($setup.family)' in .config/template.json." }
}
$appName = if ($setup.family -eq 'Labs') { "$($setup.displayName) (Labs)" } else { $setup.displayName }
$dotNet = $names.DotNet
$slug = $setup.repoSlug
$org = $setup.gitHubOrg
$moduleKey = "$($names.AdminOrg)-$($names.AdminProject)"
$isLucene = $setup.family -eq 'Kentico' -and $name -eq 'Lucene'
if ($isLucene) {
    $names.AdminProject = 'xperience-integrations-lucene-admin'
    $moduleKey = 'kentico-xperience-integrations-lucene-admin'
}
$usesAzure = $setup.family -ne 'Community' -and -not $setup.example

$tokens = @(
    , @('Kentico/xperience-by-kentico-repotemplate', "$org/$slug")
    , @('xperience-by-kentico-repotemplate', $slug)
    , @('"@kentico/xperience-repotemplate-admin', "`"@$($names.AdminOrg)/$($names.AdminProject)")
    , @('RegisterClientModule("kentico", "xperience-repotemplate-admin")', "RegisterClientModule(`"$($names.AdminOrg)`", `"$($names.AdminProject)`")")
    , @("orgName: 'kentico'", "orgName: '$($names.AdminOrg)'")
    , @('<AdminOrgName>kentico</AdminOrgName>', "<AdminOrgName>$($names.AdminOrg)</AdminOrgName>")
    , @('kentico-xperience-repotemplate-admin', $moduleKey)
    , @('xperience-repotemplate-admin', $names.AdminProject)
    , @('Kentico-Xperience-RepoTemplate', $dotNet.Replace('.', '-'))
    , @('Kentico.Xperience.RepoTemplate', $dotNet)
    , @('kentico.xperience.repotemplate', $dotNet.ToLowerInvariant())
    , @('Xperience by Kentico: Repo Template', "$($names.Prefix): $($setup.displayName)")
    , @('name: "Repo Template"', "name: `"$appName`"")
    , @('const applicationName = "Repo Template";', "const applicationName = `"$appName`";")
    , @('Repo Template', $setup.displayName)
    , @('RepoTemplate', $name)
    , @('repotemplate', $kebab)
)

$removedTags = @('upstream')
if ($setup.noAdmin) { $removedTags += 'admin' }
if (-not $usesAzure) { $removedTags += 'azure' }
if ($setup.example) { $removedTags += 'package' }

function Get-Applicability([string]$path) {
    if ($path -match '^(\.github/workflows/template-smoke\.yml|\.claude/skills/setup-repo/|\.config/template\.json$)') { return 'template-only' }
    if ($setup.noAdmin -and $path -match '^(src/[^/]+\.Admin/|tests/e2e/admin/custom-template\.spec\.ts$|tests/e2e/admin/customTemplatePage\.ts$|tests/e2e/screenshots/admin\.screenshots\.ts$)') { return 'not-applicable' }
    if (-not $usesAzure -and $path -match '^(\.azuredevops/|Directory\.Build\.targets$)') { return 'not-applicable' }
    if (($setup.family -ne 'Community' -or $setup.example) -and $path -eq '.github/workflows/publish.yml') { return 'not-applicable' }
    if ($setup.example -and $path -match '^src/[^/]+\.Libs\.slnx$') { return 'not-applicable' }
    $null
}

function Get-LocalPath([string]$path) {
    if ($isLucene -and $path -eq 'tests/Kentico.Xperience.RepoTemplate.Tests/Kentico.Xperience.RepoTemplate.Tests.csproj') {
        return 'tests/Kentico.Xperience.Lucene.Tests/Kentico.Xperience.Lucene.Core.Tests.csproj'
    }
    if ($isLucene -and $path.StartsWith('src/Kentico.Xperience.RepoTemplate/')) {
        return $path.Replace('src/Kentico.Xperience.RepoTemplate/', "src/$dotNet.Core/").
            Replace('Kentico.Xperience.RepoTemplate.csproj', "$dotNet.Core.csproj").
            Replace('RepoTemplate', $name)
    }
    ($path -split '/' | ForEach-Object { $_.Replace('Kentico.Xperience.RepoTemplate', $dotNet).Replace('RepoTemplate', $name) }) -join '/'
}

$textExtensions = @(
    '.cs', '.csproj', '.props', '.targets', '.slnx', '.sln', '.json', '.yml', '.yaml', '.md',
    '.js', '.mjs', '.ts', '.tsx', '.css', '.cshtml', '.razor', '.ps1', '.xml', '.config', '.txt'
)
$textNames = @('.editorconfig', '.gitignore', '.gitattributes', 'CODEOWNERS', '.npmrc', '.prettierignore')

# Renders one template file's bytes as setup-repo would for this repository
function Format-Rendered([string]$path, [byte[]]$bytes) {
    $isText = $textExtensions -contains [IO.Path]::GetExtension($path) -or $textNames -contains [IO.Path]::GetFileName($path)
    if (-not $isText -or $bytes -contains 0) { return , $bytes }
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $c = [Text.UTF8Encoding]::new($false).GetString($bytes, $(if ($bom) { 3 } else { 0 }), $bytes.Length - $(if ($bom) { 3 } else { 0 }))

    # Not renamed by setup-repo: this skill, and the Xperience CI repository
    if ($path -notmatch '^\.claude/skills/update-template/|/App_Data/CIRepository/') {
        if ($isLucene) {
            $c = $c.Replace('Kentico.Xperience.RepoTemplate.Tests/Kentico.Xperience.RepoTemplate.Tests.csproj',
                'Kentico.Xperience.Lucene.Tests/Kentico.Xperience.Lucene.Core.Tests.csproj').
                Replace('Kentico.Xperience.RepoTemplate.Tests\Kentico.Xperience.RepoTemplate.Tests.csproj',
                'Kentico.Xperience.Lucene.Tests\Kentico.Xperience.Lucene.Core.Tests.csproj')
        }
        if ($isLucene -and $path -notmatch '^examples/DancingGoat/DancingGoat\.csproj$') {
            $c = $c.Replace('Kentico.Xperience.RepoTemplate/Kentico.Xperience.RepoTemplate.csproj',
                "$dotNet.Core/$dotNet.Core.csproj")
            $c = $c.Replace('Kentico.Xperience.RepoTemplate\Kentico.Xperience.RepoTemplate.csproj',
                "$dotNet.Core\$dotNet.Core.csproj")
        }
        foreach ($token in $tokens) { $c = $c.Replace($token[0], $token[1]) }
        if ($isLucene) {
            if ($path -eq 'tests/Kentico.Xperience.RepoTemplate.Tests/Kentico.Xperience.RepoTemplate.Tests.csproj') {
                $c = $c.Replace('<AssemblyName>Kentico.Xperience.Lucene.Tests</AssemblyName>',
                    '<AssemblyName>Kentico.Xperience.Lucene.Core.Tests</AssemblyName>')
            }
            if ($path -eq 'examples/DancingGoat/DancingGoat.csproj') {
                $c = [regex]::Replace($c, '(?m)^[ \t]*<ProjectReference Include="[^"]*Kentico\.Xperience\.Lucene\.Admin[\\/][^"]+"[ \t]*/>\r?\n', '')
            }
            if ($path.StartsWith('src/Kentico.Xperience.RepoTemplate/') -or [IO.Path]::GetExtension($path) -eq '.cs') {
                $c = [regex]::Replace($c, '\bKentico\.Xperience\.Lucene(?![.\w])', "$dotNet.Core")
            }
            if ([IO.Path]::GetExtension($path) -eq '.slnx' -and $c.Contains("$dotNet.Core/$dotNet.Core.csproj")) {
                $aggregatePath = if ($path.StartsWith('src/')) { "$dotNet/$dotNet.csproj" } else { "src/$dotNet/$dotNet.csproj" }
                if (-not $c.Contains("Path=`"$aggregatePath`"")) {
                    $c = $c.Replace('</Solution>', "  <Project Path=`"$aggregatePath`" />`n</Solution>")
                }
            }
            if ($path -eq '.azuredevops/pipelines/build-and-release.yml') {
                $c = $c.Replace("artifactName: `"$($dotNet.Replace('.', '-'))`"",
                    "artifactName: `"$($dotNet.Replace('.', '-'))-Core`"").
                    Replace("projectPath: `"src/$dotNet`"", "projectPath: `"src/$dotNet.Core`"")
                $coreArtifact = "(?m)^([ ]*)- Core:\r?\n[ ]*artifactName: `"$($dotNet.Replace('.', '-'))-Core`"\r?\n[ ]*projectPath: `"src/$dotNet.Core`""
                $c = [regex]::Replace($c, $coreArtifact, {
                    param($match)
                    $indent = $match.Groups[1].Value
                    $match.Value + "`n$indent- Aggregate:`n$indent  artifactName: `"$($dotNet.Replace('.', '-'))`"`n$indent  projectPath: `"src/$dotNet`""
                })
            }
        }
    }
    foreach ($tag in $removedTags) {
        $c = [regex]::Replace($c, "(?m)^[^\r\n]*template:${tag}:start[^\r\n]*\r?\n[\s\S]*?^[^\r\n]*template:${tag}:end[^\r\n]*(\r?\n|\z)", '')
    }
    $c = [regex]::Replace($c, '(?m)^[^\r\n]*template:\w+:(start|end)[^\r\n]*(\r?\n|\z)', '')
    # Sections setup-repo deletes (its step 7)
    if ($path -eq 'README.md') { $c = [regex]::Replace($c, '(?s)\A## ---Template info---.*?(?=^# )', '', 'Multiline') }
    if ($path -eq 'AGENTS.md') { $c = [regex]::Replace($c, '(?ms)^### Before setup\r?\n.*?(?=^## )', '') }

    $out = [Text.UTF8Encoding]::new($bom).GetPreamble() + [Text.UTF8Encoding]::new($false).GetBytes($c)
    , [byte[]]$out
}

# Extracts the template's files at a revision into a folder
function Export-Revision([string]$rev, [string[]]$files, [string]$dest) {
    New-Item -ItemType Directory -Force $dest | Out-Null
    if (-not $files) { return }
    $zip = Join-Path $Out "$([guid]::NewGuid()).zip"
    git -C $Template archive --format=zip --output $zip $rev -- @files
    if ($LASTEXITCODE -ne 0) { throw "git archive failed for $rev" }
    Expand-Archive $zip -DestinationPath $dest -Force
    Remove-Item $zip
}

$changedPaths = git -C $Template diff --name-status --no-renames $Base $Target
if ($LASTEXITCODE -ne 0) { throw 'Cannot compare the supplied template revisions.' }
$changes = $changedPaths | ForEach-Object {
    $parts = $_ -split "`t"
    [pscustomobject]@{ Change = $parts[0]; TemplatePath = $parts[1]; LocalPath = Get-LocalPath $parts[1] }
}
if ($Paths) {
    $wanted = $Paths | ForEach-Object { $_.Replace('\', '/') -replace '^\./', '' }
    $changes = $changes | Where-Object { $wanted -contains $_.TemplatePath -or $wanted -contains $_.LocalPath }
}

$baseDir = Join-Path $Out '_base'
$targetDir = Join-Path $Out '_target'
Export-Revision $Base @($changes | Where-Object Change -ne 'A' | ForEach-Object TemplatePath) $baseDir
Export-Revision $Target @($changes | Where-Object Change -ne 'D' | ForEach-Object TemplatePath) $targetDir

$results = foreach ($item in $changes) {
    $status = Get-Applicability $item.TemplatePath
    $local = Join-Path $root $item.LocalPath
    $outFile = Join-Path $Out $item.LocalPath
    New-Item -ItemType Directory -Force (Split-Path $outFile) | Out-Null

    $baseFile = Join-Path $baseDir $item.TemplatePath
    $targetFile = Join-Path $targetDir $item.TemplatePath
    $hasBase = Test-Path -LiteralPath $baseFile -PathType Leaf
    $hasTarget = Test-Path -LiteralPath $targetFile -PathType Leaf
    if ($hasBase) { [IO.File]::WriteAllBytes("$outFile.base", (Format-Rendered $item.TemplatePath ([IO.File]::ReadAllBytes($baseFile)))) }
    if ($hasTarget) { [IO.File]::WriteAllBytes("$outFile.target", (Format-Rendered $item.TemplatePath ([IO.File]::ReadAllBytes($targetFile)))) }
    $hasLocal = Test-Path -LiteralPath $local -PathType Leaf

    if (-not $status -and $hasBase -and $hasTarget -and
        (Get-FileHash -LiteralPath "$outFile.base").Hash -eq (Get-FileHash -LiteralPath "$outFile.target").Hash) {
        $status = 'no-change'
    }
    if (-not $status) {
        $status = if (-not $hasTarget) {
            if ($hasLocal) { 'deleted' } else { 'applied' }
        }
        elseif (-not $hasLocal) {
            if ($hasBase) { 'missing' } else { 'new' }
        }
        elseif ((Get-FileHash -LiteralPath $local).Hash -eq (Get-FileHash -LiteralPath "$outFile.target").Hash) { 'applied' }
        elseif (-not $hasBase) { 'conflict' } # added in the template and locally: compare by hand
        else {
            Copy-Item -LiteralPath $local "$outFile.merged" -Force
            git merge-file -L local -L base -L template "$outFile.merged" "$outFile.base" "$outFile.target" 2>$null
            if ($LASTEXITCODE -gt 127 -or $LASTEXITCODE -lt 0) { throw "git merge-file failed for $($item.LocalPath)" }
            if ($LASTEXITCODE -eq 0) { 'merged' } else { 'conflict' }
        }
    }
    if ($status -eq 'new') { Copy-Item -LiteralPath "$outFile.target" "$outFile.merged" -Force }

    [pscustomobject]@{ Status = $status; Path = $item.LocalPath }
}

Remove-Item $baseDir, $targetDir -Recurse -Force

if ($Apply) {
    foreach ($result in $results) {
        if ($result.Status -in 'new', 'merged') {
            $dest = Join-Path $root $result.Path
            New-Item -ItemType Directory -Force (Split-Path $dest) | Out-Null
            Copy-Item -LiteralPath (Join-Path $Out "$($result.Path).merged") $dest -Force
            Write-Host "Applied $($result.Path)"
        }
        elseif ($result.Status -notin 'applied', 'no-change') {
            Write-Host "Not applied ($($result.Status)): $($result.Path)" -ForegroundColor Yellow
        }
    }
}

Write-Host "Output: $Out"
$results | Sort-Object Status, Path | Format-Table -AutoSize
