[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) "lucene-database-script-test-$([guid]::NewGuid())"
$project = Join-Path $work 'examples/DancingGoat'
$settingsPath = Join-Path $project 'appsettings.json'
$script:state = @{ Calls = [Collections.Generic.List[string]]::new(); FailManager = $false; FailUpdate = $false; Status = 'Ready'; SettingsPath = $settingsPath }
$global:luceneDatabaseTest = $script:state
$original = '{"ConnectionStrings":{"CMSConnectionString":"Data Source=fixture;Initial Catalog=fixture;Integrated Security=True"},"CMSHashStringSalt":"fixture-shared-salt"}'
$previousConnection = $env:ConnectionStrings__CMSConnectionString
$previousSalt = $env:CMSHashStringSalt

# The initializer sees only this mock; no server, tool, or user-secrets store is accessed.
function dotnet {
    $global:luceneDatabaseTest.Calls.Add(($args -join ' '))
    $global:LASTEXITCODE = 0
    switch ($args[0]) {
        'tool' { return }
        'user-secrets' {
            if ($args[1] -eq 'list') { return 'CMSHashStringSalt = fixture-local-override' }
            return
        }
        'kentico-xperience-dbmanager' {
            [IO.File]::WriteAllText($global:luceneDatabaseTest.SettingsPath, '{"ConnectionStrings":{"CMSConnectionString":"fixture-written-connection"},"CMSHashStringSalt":"fixture-shared-salt"}')
            if ($global:luceneDatabaseTest.FailManager) { $global:LASTEXITCODE = 1 }
            return
        }
        'run' {
            if ($args -contains '--kxp-db-status') { return "{`"state`":`"$($global:luceneDatabaseTest.Status)`",`"databaseVersion`":`"31.10.0`"}" }
            if ($args -contains '--kxp-update' -and $global:luceneDatabaseTest.FailUpdate) { $global:LASTEXITCODE = 1; return }
            if ($args -contains '--kxp-ci-enable') { return '{"success":true,"message":"fixture CI enabled"}' }
            return
        }
        default { throw "Unexpected mocked dotnet command: $($args[0])" }
    }
}

function Reset-Fixture {
    [IO.File]::WriteAllText($settingsPath, $original)
    $script:state.Calls.Clear()
    $script:state.FailManager = $false
    $script:state.FailUpdate = $false
    $script:state.Status = 'Ready'
}

function Assert-Unchanged {
    if ([IO.File]::ReadAllText($settingsPath) -cne $original) { throw 'Committed settings were changed.' }
    if ($env:ConnectionStrings__CMSConnectionString -cne 'fixture-parent-connection' -or
        $env:CMSHashStringSalt -cne 'fixture-parent-salt') { throw 'Caller environment was changed.' }
}

function Assert-Rejected([scriptblock]$Action, [string]$Expected) {
    $rejected = $false
    try { & $Action | Out-Null }
    catch {
        if (-not $_.Exception.Message.Contains($Expected)) { throw }
        $rejected = $true
    }
    if (-not $rejected) { throw "Expected failure containing: $Expected" }
    Assert-Unchanged
}

try {
    New-Item -ItemType Directory (Join-Path $work 'scripts'), (Join-Path $project 'App_Data/CIRepository') -Force | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'Initialize-Database.ps1') (Join-Path $work 'scripts')
    [IO.File]::WriteAllText((Join-Path $project 'App_Data/CIRepository/repository.config'), '<Repository />')
    $initializer = Join-Path $work 'scripts/Initialize-Database.ps1'
    $env:ConnectionStrings__CMSConnectionString = 'fixture-parent-connection'
    $env:CMSHashStringSalt = 'fixture-parent-salt'

    Reset-Fixture
    Assert-Rejected { & $initializer -HashStringSalt 'different-salt' -LicenseFile '' } 'must match'
    if ($script:state.Calls.Count) { throw 'Conflicting salt accessed tools or secrets.' }

    Reset-Fixture
    $script:state.FailManager = $true
    Assert-Rejected { & $initializer -LicenseFile '' } 'dbmanager failed'
    if ($script:state.Calls | Where-Object { $_ -like 'user-secrets set*' -or $_ -like 'user-secrets remove*' }) {
        throw 'Failed creation modified user secrets.'
    }

    Reset-Fixture
    & $initializer -SaveTo None -LicenseFile '' | Out-Null
    Assert-Unchanged
    if ($script:state.Calls | Where-Object { $_ -like 'user-secrets*' }) { throw '-SaveTo None accessed secrets.' }
    if (-not ($script:state.Calls | Where-Object { $_ -like '*--kxp-ci-restore*' })) { throw 'CI objects were not restored.' }

    Reset-Fixture
    & $initializer -SaveTo UserSecrets -LicenseFile '' | Out-Null
    Assert-Unchanged
    if (-not ($script:state.Calls | Where-Object { $_ -like 'user-secrets remove CMSHashStringSalt' })) { throw 'Local salt override was not removed.' }
    if ($script:state.Calls | Where-Object { $_ -like 'user-secrets set CMSHashStringSalt*' }) { throw 'Committed salt was overridden.' }

    Reset-Fixture
    & $initializer -IfNotReady -SaveTo UserSecrets -LicenseFile '' | Out-Null
    Assert-Unchanged
    if ($script:state.Calls | Where-Object { $_ -like 'kentico-xperience-dbmanager*' -or $_ -like '*--kxp-update*' }) { throw 'Ready database was changed.' }
    if (-not ($script:state.Calls | Where-Object { $_ -eq 'user-secrets remove CMSHashStringSalt' })) { throw 'Ready database retained the stale salt override.' }

    Reset-Fixture
    Assert-Rejected { & $initializer -LicenseFile (Join-Path $work 'missing-license.txt') } 'License file not found'

    Reset-Fixture
    & $initializer -IfNotReady -SaveTo None -LicenseFile '' | Out-Null
    Assert-Unchanged
    if ($script:state.Calls | Where-Object { $_ -like 'user-secrets*' }) { throw 'Ready None mode accessed secrets.' }

    Reset-Fixture
    $script:state.Status = 'UpdateRequired'
    & $initializer -IfNotReady -SaveTo UserSecrets -LicenseFile '' | Out-Null
    Assert-Unchanged
    $updateIndex = $script:state.Calls.FindIndex([Predicate[string]]{ param($call) $call -like '*--kxp-update*' })
    $removeIndex = $script:state.Calls.IndexOf('user-secrets remove CMSHashStringSalt')
    if ($updateIndex -lt 0 -or $removeIndex -le $updateIndex) { throw 'Salt was not normalized after successful update.' }

    Reset-Fixture
    $script:state.Status = 'UpdateRequired'
    $script:state.FailUpdate = $true
    Assert-Rejected { & $initializer -IfNotReady -SaveTo UserSecrets -LicenseFile '' } 'DancingGoat CLI failed'
    if ($script:state.Calls | Where-Object { $_ -like 'user-secrets set*' -or $_ -like 'user-secrets remove*' }) { throw 'Failed update modified secrets.' }
    Write-Host 'Database initialization: 9 mocked safety cases passed; no services or secrets accessed.'
}
finally {
    Remove-Variable luceneDatabaseTest -Scope Global
    $env:ConnectionStrings__CMSConnectionString = $previousConnection
    $env:CMSHashStringSalt = $previousSalt
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
