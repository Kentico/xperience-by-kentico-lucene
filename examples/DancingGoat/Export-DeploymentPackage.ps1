<#
.Synopsis
    Creates a deployment package for uploading to the Xperience Cloud environment.
#>
[CmdletBinding()]
param (
    # Output path for exported deployment package.
    [Parameter(Mandatory = $false)]
    [string]$OutputPackagePath = "./DeploymentPackage.zip",

    # The name of the main web application assembly used as the starting point by the Xperience Cloud.
    [Parameter(Mandatory = $true)]
    [string]$AssemblyName,

    # If present, the custom build number won't be used as a "Product version" suffix in the format yyyyMMddHHmm.
    [switch]$KeepProductVersion,

    # Mode in which the storage assets are deployed, if present.
    [ValidateSet("Create", "CreateUpdate")]
    [String]$StorageAssetsDeploymentMode = "Create"
)
$ErrorActionPreference = "Stop"

$OutputFolderPath = "./bin/CloudDeployment/"
$MetadataFilePath = Join-Path $OutputFolderPath "cloud-metadata.json"
$CDRepositoryFolderPath = "./`$CDRepository"
$StorageAssetsFolderName = "`$StorageAssets"
$BuildNumber = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmm")

# Remove previously published website
Remove-Item -Recurse -Force $OutputFolderPath -ErrorAction SilentlyContinue

# Publish the application in the 'Release' mode
$PublishCommand = "dotnet publish --nologo -c Release -o $OutputFolderPath"

if (!$KeepProductVersion) {
    $PublishCommand += " --version-suffix $BuildNumber"
}

Invoke-Expression $PublishCommand

if ($LASTEXITCODE -ne 0) {
    throw "Publishing the website failed."
}

# Get CD repositories paths
$LocalCDRepositoryPath = Join-Path (Resolve-Path .) $CDRepositoryFolderPath
$OutputCDRepositoryPath = Join-Path $OutputFolderPath $CDRepositoryFolderPath

# Check for non-existing or empty CD repository which could corrupt the database
if (-not (Test-Path $LocalCDRepositoryPath) -or (@(Get-ChildItem -Path $LocalCDRepositoryPath -Directory).Count -le 0)) {
    throw "Cannot detect CD repository on path '$LocalCDRepositoryPath'. Make sure to run 'dotnet run --kxp-cd-store --repository-path ""```$CDRepository""' before 'Export-DeploymentPackage.ps1'."
}

# Copy content of the CD repository to the output folder
Copy-Item -Force -Recurse "$LocalCDRepositoryPath/*" -Destination $OutputCDRepositoryPath

# Get storage assets paths
$LocalStorageAssetsPath = Join-Path (Resolve-Path .) $StorageAssetsFolderName
$OutputStorageAssetsPath = Join-Path $OutputFolderPath $StorageAssetsFolderName

if (Test-Path $LocalStorageAssetsPath) {
    # Check if storage asset top-level directories have valid names
    Get-ChildItem -Path $LocalStorageAssetsPath | % {
        if ($_.Name -cnotmatch "^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$") {
            throw "Storage asset directory '$($_.FullName)' does not have a valid name. Top level storage asset directories must have names that are 3-63 characters long and contain only lowercase letters, numbers or dashes (-). Every dash symbol must be surrounded by letters or numbers."
        }
    }

    # Copy storage assets to the output folder
    New-Item -Force -ItemType Directory $OutputStorageAssetsPath | Out-Null
    Copy-Item -Force -Recurse "$LocalStorageAssetsPath/*" -Destination $OutputStorageAssetsPath

    # Deployed assets need to have lowercase names
    Get-ChildItem -Path $OutputStorageAssetsPath -Recurse | % {
        $lowercasedAssetName = $_.Name.ToLowerInvariant()

        if ($_.Name -cne $lowercasedAssetName) {
            Rename-Item -Force $_.FullName "$($_.Name).tmp"
            Rename-Item -Force "$($_.FullName).tmp" $lowercasedAssetName
        }
    }
}

$AssemblyPath = Join-Path $OutputFolderPath "$AssemblyName.dll" -Resolve
$PackageMetadata = @{
    AssemblyName = $AssemblyName
    Version      = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($AssemblyPath).ProductVersion
}

# Add necessary metadata if storage assets folder has been exported as well
if (Test-Path $OutputStorageAssetsPath) {
    $PackageMetadata.Add("StorageAssetsDirectory", $StorageAssetsFolderName)
    $PackageMetadata.Add("StorageAssetsDeploymentMode", $StorageAssetsDeploymentMode)
}

# Create all necessary metadata for cloud-based package deployment
$PackageMetadata | ConvertTo-Json -Depth 2 | Set-Content $MetadataFilePath -Encoding utf8

# Create a deployment package
if (Test-Path -Path $OutputPackagePath -PathType Container) {
    $OutputPackagePath = Join-Path -Path $OutputPackagePath -ChildPath "./DeploymentPackage.zip"
}
Compress-Archive -Force -Path "$OutputFolderPath/*" -DestinationPath $OutputPackagePath