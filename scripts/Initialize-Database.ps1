<#
.SYNOPSIS
    Creates the DancingGoat database with kentico-xperience-dbmanager.

.DESCRIPTION
    Defaults come from the CMSConnectionString in examples/DancingGoat/appsettings.json.
    dbmanager writes the connection string and the CMSHashStringSalt to that file; -SaveTo decides
    where they end up:
      UserSecrets  (default) DancingGoat user secrets; appsettings.json is restored, so nothing local is committed
      Salt         only the salt, to DancingGoat user secrets (the Aspire AppHost supplies the connection string)
      AppSettings  (default in CI) appsettings.json keeps them; user secrets only load in Development
      None         appsettings.json is restored and nothing is saved

    The salt is the CMSHashStringSalt committed in appsettings.json, shared by every machine so the
    macro signatures in the Continuous Integration repository stay valid. A salt in DancingGoat user
    secrets would override it, so it is removed. Without a committed salt, a salt already in user
    secrets is reused, otherwise dbmanager generates one.

    When examples/DancingGoat/App_Data/CIRepository exists, the new database gets the repository's
    objects (--kxp-ci-restore) and Continuous Integration is enabled. DancingGoat must be built in
    -Configuration first.

    Without a license file the database is still created; add the license on first sign-in
    to the administration (/admin, user "administrator").

.EXAMPLE
    ./scripts/Initialize-Database.ps1
    ./scripts/Initialize-Database.ps1 -LicenseFile ~/xperience/license.txt -Recreate
    ./scripts/Initialize-Database.ps1 -ServerName "localhost\SQLEXPRESS" -Username "" # Windows authentication
#>
[CmdletBinding()]
param (
    # SQL Server, e.g. "localhost,1433" (Docker) or "localhost\SQLEXPRESS". Defaults to the connection string's Data Source
    [string]$ServerName,

    # Defaults to the connection string's Initial Catalog
    [string]$DatabaseName,

    # SQL authentication. Defaults to the connection string's User ID / Password (or XPERIENCE_SQL_PASSWORD); empty Username uses Windows authentication
    [string]$Username,
    [string]$Password,

    # The repository's "administrator" password. The CI repository restore sets the committed
    # password hash, so this only matters for a database without a CI repository. Keep it equal
    # to adminPassword in tests/e2e/shared/config.ts.
    [string]$AdminPassword = 'Pass@12345',

    # Xperience license file; optional
    [string]$LicenseFile = $env:XPERIENCE_LICENSE_FILE,

    # Salt for macro signatures; defaults to CMSHashStringSalt in appsettings.json, then DancingGoat user secrets, otherwise dbmanager generates one
    [string]$HashStringSalt,

    # Build configuration DancingGoat was built in; its Xperience CLI runs without building
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    # Drop and recreate the database if it exists
    [switch]$Recreate,

    # Do nothing when the database is ready; apply the Xperience update when it is behind the application
    [switch]$IfNotReady,

    [ValidateSet('UserSecrets', 'Salt', 'AppSettings', 'None')]
    [string]$SaveTo = $(if ($env:CI) { 'AppSettings' } else { 'UserSecrets' })
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'examples/DancingGoat'
$appsettingsPath = Join-Path $project 'appsettings.json'
$appsettingsOriginal = [IO.File]::ReadAllText($appsettingsPath)
$appsettings = $appsettingsOriginal | ConvertFrom-Json

$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
# set_ConnectionString: PowerShell treats the builder as a dictionary, so property assignment would add a key instead
$connection.set_ConnectionString($appsettings.ConnectionStrings.CMSConnectionString)

function Get-ConnectionValue([string]$key) {
    if ($connection.ContainsKey($key)) { return [string]$connection[$key] }
    return ''
}

if (-not $PSBoundParameters.ContainsKey('ServerName')) { $ServerName = Get-ConnectionValue 'Data Source' }
if (-not $PSBoundParameters.ContainsKey('DatabaseName')) { $DatabaseName = Get-ConnectionValue 'Initial Catalog' }
if (-not $PSBoundParameters.ContainsKey('Username')) {
    $Username = if ((Get-ConnectionValue 'Integrated Security') -ieq 'True') { '' } else { Get-ConnectionValue 'User ID' }
}
if (-not $PSBoundParameters.ContainsKey('Password')) {
    $Password = if ($env:XPERIENCE_SQL_PASSWORD) { $env:XPERIENCE_SQL_PASSWORD } else { Get-ConnectionValue 'Password' }
}

$useUserSecrets = $SaveTo -in 'UserSecrets', 'Salt'
$committedSalt = [string]$appsettings.CMSHashStringSalt
if ($committedSalt -and $HashStringSalt -and $HashStringSalt -cne $committedSalt) {
    throw 'HashStringSalt must match the committed CMSHashStringSalt; changing it invalidates CI macro signatures.'
}
if (-not $HashStringSalt) { $HashStringSalt = $committedSalt }
if ($useUserSecrets) {
    Push-Location $project
    try {
        $stored = dotnet user-secrets list 2>$null | Where-Object { $_ -like 'CMSHashStringSalt = *' } | Select-Object -First 1
        if ($LASTEXITCODE -ne 0) { throw 'dotnet user-secrets list failed' }
        if ($stored -and -not $HashStringSalt) { $HashStringSalt = $stored.Substring('CMSHashStringSalt = '.Length) }
    }
    finally {
        Pop-Location
    }
}

if (-not $ServerName) { throw 'No SQL Server: pass -ServerName or set Data Source in examples/DancingGoat/appsettings.json.' }
if (-not $DatabaseName) { throw 'No database name: pass -DatabaseName or set Initial Catalog in examples/DancingGoat/appsettings.json.' }

# Runs DancingGoat's Xperience CLI against the target database (DancingGoat must be built)
function Invoke-DancingGoatCli([string[]]$cliArguments) {
    $previousConnection = $env:ConnectionStrings__CMSConnectionString
    $previousSalt = $env:CMSHashStringSalt
    $targetConnection = [System.Data.Common.DbConnectionStringBuilder]::new()
    $targetConnection['Data Source'] = $ServerName
    $targetConnection['Initial Catalog'] = $DatabaseName
    $targetConnection['TrustServerCertificate'] = 'True'
    $targetConnection['Encrypt'] = 'False'
    if ($Username) {
        $targetConnection['User ID'] = $Username
        $targetConnection['Password'] = $Password
    } else { $targetConnection['Integrated Security'] = 'True' }
    $env:ConnectionStrings__CMSConnectionString = $targetConnection.ConnectionString
    if ($HashStringSalt) { $env:CMSHashStringSalt = $HashStringSalt }
    # From the project folder: the content root, where App_Data/CIRepository resolves
    Push-Location $project
    try {
        dotnet run --project $project --no-build --configuration $Configuration --no-launch-profile -- @cliArguments
        if ($LASTEXITCODE -ne 0) { throw "DancingGoat CLI failed ($LASTEXITCODE): $($cliArguments[0])" }
    }
    finally {
        Pop-Location
        $env:ConnectionStrings__CMSConnectionString = $previousConnection
        $env:CMSHashStringSalt = $previousSalt
    }
}

if ($IfNotReady) {
    $statusJson = Invoke-DancingGoatCli @('--kxp-db-status', '--format', 'json') | Select-Object -Last 1
    $status = $statusJson | ConvertFrom-Json
    switch ($status.state) {
        'Ready' {
            Write-Host "Database '$DatabaseName' is ready ($($status.databaseVersion))."
            return
        }
        'UpdateRequired' {
            Write-Host $status.message
            Invoke-DancingGoatCli @('--kxp-update', '--skip-confirmation')
            if ($LASTEXITCODE -ne 0) { throw "Xperience database update failed ($LASTEXITCODE)" }
            return
        }
        default {
            # NotAvailable: no database yet (the server itself is up for callers that wait on it)
            Write-Host $status.message
        }
    }
}

$arguments = @('--server-name', $ServerName, '--database-name', $DatabaseName, '--admin-password', $AdminPassword)
if ($Username) { $arguments += @('--username', $Username, '--password', $Password) }
if ($HashStringSalt) { $arguments += @('--hash-string-salt', $HashStringSalt) }
if ($Recreate) { $arguments += '--recreate-existing-database' }

if ($LicenseFile) {
    if (Test-Path $LicenseFile -PathType Leaf) {
        $arguments += @('--license-file', (Resolve-Path $LicenseFile).Path)
    }
    else {
        throw "License file not found: $LicenseFile."
    }
}

Write-Host "Creating database '$DatabaseName' on '$ServerName' ($(if ($Username) { "SQL login $Username" } else { 'Windows authentication' }))"

$databaseCreated = $false
Push-Location $root
try {
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed' }

    Push-Location $project
    try {
        $null = dotnet kentico-xperience-dbmanager -- @arguments
        if ($LASTEXITCODE -ne 0) { throw "kentico-xperience-dbmanager failed ($LASTEXITCODE)" }
        $databaseCreated = $true

        if ($useUserSecrets -and $stored -and $committedSalt) {
            dotnet user-secrets remove 'CMSHashStringSalt' | Out-Null
            if ($LASTEXITCODE -ne 0) { throw 'dotnet user-secrets remove failed' }
            Write-Host 'Removed the local salt override; using the committed CI macro salt.'
        }

        switch ($SaveTo) {
            'UserSecrets' {
                $written = [IO.File]::ReadAllText($appsettingsPath) | ConvertFrom-Json
                dotnet user-secrets set 'ConnectionStrings:CMSConnectionString' $written.ConnectionStrings.CMSConnectionString | Out-Null
                if ($LASTEXITCODE -ne 0) { throw 'dotnet user-secrets set failed' }
                if (-not $committedSalt) {
                    dotnet user-secrets set 'CMSHashStringSalt' $written.CMSHashStringSalt | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw 'dotnet user-secrets set failed' }
                }
                [IO.File]::WriteAllText($appsettingsPath, $appsettingsOriginal)
                Write-Host "Moved the connection string$(if (-not $committedSalt) { ' and CMSHashStringSalt' }) to DancingGoat user secrets."
            }
            'Salt' {
                if (-not $committedSalt) {
                    $written = [IO.File]::ReadAllText($appsettingsPath) | ConvertFrom-Json
                    dotnet user-secrets set 'CMSHashStringSalt' $written.CMSHashStringSalt | Out-Null
                    if ($LASTEXITCODE -ne 0) { throw 'dotnet user-secrets set failed' }
                    Write-Host 'Saved CMSHashStringSalt to DancingGoat user secrets.'
                }
                [IO.File]::WriteAllText($appsettingsPath, $appsettingsOriginal)
            }
            'None' {
                [IO.File]::WriteAllText($appsettingsPath, $appsettingsOriginal)
            }
        }
    }
    finally {
        if ($SaveTo -ne 'AppSettings' -or -not $databaseCreated) {
            [IO.File]::WriteAllText($appsettingsPath, $appsettingsOriginal)
        }
        Pop-Location
    }
}
finally {
    Pop-Location
}

# Objects from the committed Continuous Integration repository, then CI on so changes are written back to it
if (Test-Path (Join-Path $project 'App_Data/CIRepository/repository.config')) {
    Write-Host 'Restoring the Continuous Integration repository (examples/DancingGoat/App_Data/CIRepository)'
    Invoke-DancingGoatCli @('--kxp-ci-restore')
    if ($LASTEXITCODE -ne 0) { throw "Continuous Integration restore failed ($LASTEXITCODE). Is DancingGoat built in $Configuration?" }
    $enabled = Invoke-DancingGoatCli @('--kxp-ci-enable', '--format', 'json') | Select-Object -Last 1 | ConvertFrom-Json
    if (-not $enabled.success) { throw "Enabling Continuous Integration failed: $($enabled.message)" }
    Write-Host $enabled.message
}

Write-Host "Database ready. Sign in at /admin as 'administrator'."
