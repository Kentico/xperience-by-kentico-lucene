[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = (git rev-parse --show-toplevel).Trim()
$work = Join-Path ([IO.Path]::GetTempPath()) "lucene-render-test-$([guid]::NewGuid())"
$template = Join-Path $work 'template'
$output = Join-Path $work 'rendered'

function Invoke-Git([string[]]$Arguments) {
    $result = git -C $template @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Fixture git command failed: $($Arguments[0])" }
    $result
}

function Assert-Contains([string]$Path, [string[]]$Expected, [string[]]$Rejected = @()) {
    $text = [IO.File]::ReadAllText((Join-Path $output "$Path.target"))
    foreach ($value in $Expected) {
        if (-not $text.Contains($value)) { throw "$Path does not contain $value" }
    }
    foreach ($value in $Rejected) {
        if ($text.Contains($value)) { throw "$Path unexpectedly contains $value" }
    }
}

$files = @{
    'src/Kentico.Xperience.RepoTemplate/Kentico.Xperience.RepoTemplate.csproj' = @'
<Project><!-- __VERSION__ --><PropertyGroup><RootNamespace>Kentico.Xperience.RepoTemplate</RootNamespace><PackageId>Kentico.Xperience.RepoTemplate</PackageId></PropertyGroup></Project>
'@
    'src/Kentico.Xperience.RepoTemplate.Admin/Client/package.json' = @'
{"name":"@kentico/xperience-repotemplate-admin","module":"kentico-xperience-repotemplate-admin","fixture":"__VERSION__"}
'@
    'src/Kentico.Xperience.RepoTemplate.Admin/Kentico.Xperience.RepoTemplate.Admin.csproj' = @'
<Project><!-- __VERSION__ -->
  <ProjectReference Include="..\Kentico.Xperience.RepoTemplate\Kentico.Xperience.RepoTemplate.csproj" />
  <ProjectName>xperience-repotemplate-admin</ProjectName>
</Project>
'@
    'Kentico.Xperience.RepoTemplate.slnx' = @'
<Solution><!-- __VERSION__ -->
  <Project Path="src/Kentico.Xperience.RepoTemplate/Kentico.Xperience.RepoTemplate.csproj" />
  <Project Path="src/Kentico.Xperience.RepoTemplate.Admin/Kentico.Xperience.RepoTemplate.Admin.csproj" />
</Solution>
'@
    'src/Kentico.Xperience.RepoTemplate.Libs.slnx' = @'
<Solution><!-- __VERSION__ -->
  <Project Path="Kentico.Xperience.RepoTemplate/Kentico.Xperience.RepoTemplate.csproj" />
  <Project Path="Kentico.Xperience.RepoTemplate.Admin/Kentico.Xperience.RepoTemplate.Admin.csproj" />
</Solution>
'@
    'examples/DancingGoat/DancingGoat.csproj' = @'
<Project><!-- __VERSION__ -->
  <ProjectReference Include="..\..\src\Kentico.Xperience.RepoTemplate\Kentico.Xperience.RepoTemplate.csproj" />
  <ProjectReference Include="..\..\src\Kentico.Xperience.RepoTemplate.Admin\Kentico.Xperience.RepoTemplate.Admin.csproj" />
</Project>
'@
    '.azuredevops/pipelines/build-and-release.yml' = @'
# __VERSION__
artifacts:
  - Core:
    artifactName: "Kentico-Xperience-RepoTemplate"
    projectPath: "src/Kentico.Xperience.RepoTemplate"
  - Admin:
    artifactName: "Kentico-Xperience-RepoTemplate-Admin"
    projectPath: "src/Kentico.Xperience.RepoTemplate.Admin"
'@
}

try {
    New-Item -ItemType Directory $template -Force | Out-Null
    Invoke-Git @('init', '-q')
    Invoke-Git @('config', 'user.name', 'Template rendering fixture')
    Invoke-Git @('config', 'user.email', 'fixture@localhost')
    Invoke-Git @('config', 'core.autocrlf', 'false')
    foreach ($version in 'base', 'target') {
        foreach ($file in $files.GetEnumerator()) {
            $path = Join-Path $template $file.Key
            New-Item -ItemType Directory (Split-Path $path) -Force | Out-Null
            [IO.File]::WriteAllText($path, $file.Value.Replace('__VERSION__', $version))
        }
        Invoke-Git @('add', '-A')
        Invoke-Git @('commit', '-q', '-m', $version)
        Invoke-Git @('tag', $version)
    }

    & (Join-Path $PSScriptRoot 'Compare-TemplateFiles.ps1') -Template $template -Base base -Target target -Out $output | Out-Null
    Assert-Contains 'src/Kentico.Xperience.Lucene.Core/Kentico.Xperience.Lucene.Core.csproj' @(
        '<RootNamespace>Kentico.Xperience.Lucene.Core</RootNamespace>',
        '<PackageId>Kentico.Xperience.Lucene.Core</PackageId>') @('Lucene.Core.Core', 'RepoTemplate')
    Assert-Contains 'src/Kentico.Xperience.Lucene.Admin/Client/package.json' @(
        '@kentico/xperience-integrations-lucene-admin', 'kentico-xperience-integrations-lucene-admin') @('xperience-lucene-admin')
    Assert-Contains 'src/Kentico.Xperience.Lucene.Admin/Kentico.Xperience.Lucene.Admin.csproj' @(
        '..\Kentico.Xperience.Lucene.Core\Kentico.Xperience.Lucene.Core.csproj',
        '<ProjectName>xperience-integrations-lucene-admin</ProjectName>')
    foreach ($path in 'Kentico.Xperience.Lucene.slnx', 'src/Kentico.Xperience.Lucene.Libs.slnx') {
        Assert-Contains $path @(
            'Kentico.Xperience.Lucene.Core/Kentico.Xperience.Lucene.Core.csproj',
            'Kentico.Xperience.Lucene.Admin/Kentico.Xperience.Lucene.Admin.csproj',
            'Kentico.Xperience.Lucene/Kentico.Xperience.Lucene.csproj')
    }
    Assert-Contains 'examples/DancingGoat/DancingGoat.csproj' @(
        '..\..\src\Kentico.Xperience.Lucene\Kentico.Xperience.Lucene.csproj') @(
        'Lucene.Admin.csproj', 'Lucene.Core.csproj')
    Assert-Contains '.azuredevops/pipelines/build-and-release.yml' @(
        'artifactName: "Kentico-Xperience-Lucene-Core"',
        'projectPath: "src/Kentico.Xperience.Lucene.Core"',
        'artifactName: "Kentico-Xperience-Lucene"',
        'projectPath: "src/Kentico.Xperience.Lucene"',
        'artifactName: "Kentico-Xperience-Lucene-Admin"')
    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Compare-TemplateFiles.ps1') -Template $template -Base base -Target target -Out (Join-Path $root 'unsafe-render-output') | Out-Null
    }
    catch {
        if (-not $_.Exception.Message.Contains('outside the repository')) { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw 'An in-repository output folder was accepted.' }
    Write-Host 'Lucene template rendering: 7 representative files and output safety passed.'
}
finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
