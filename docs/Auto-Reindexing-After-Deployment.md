# Auto-Reindexing after Deployment

> **Note:** This is a workaround for environments where indexes are stored on the **local file system** and local storage does not persist across deployments (e.g., [Kentico Xperience SaaS](https://docs.kentico.com/x/saas_overview_xp)). The recommended approach is to use an [external storage provider](Index-Storage.md) (such as [Azure Blob Storage](https://docs.kentico.com/x/5IfWCQ) or [Amazon S3](https://docs.kentico.com/x/5YfWCQ)), which eliminates the need for automatic reindexing entirely.

## Overview

When Lucene indexes are stored on the local file system and the environment does not provide persistent file storage, index data is lost during deployments or restarts. To address this, a configuration option is available to enable automatic reindexing based on assembly version changes.

The solution adds a hosted service that periodically checks the application's assembly version, compares it with the stored assembly version for each index, and updates it after a rebuild. If the versions differ, the index is automatically rebuilt.

**Note:** For this feature to work correctly, your project must have a build-time assembly version that changes with each deployment. One approach is to add the following to your `.csproj` file:
```xml
<PropertyGroup>
    <VersionSuffix>$([System.DateTime]::UtcNow.ToString("yyyyMMdd-HHmmss"))</VersionSuffix>
</PropertyGroup>
```
You can also use any other assembly versioning method, as long as it produces a unique value for each deployment.

## Configuration

To enable the automatic assembly check and index rebuilding, follow these steps:

### 1. Add Configuration to appsettings.json

Add the following configuration to your `appsettings.json` file:

```json
"CMSLuceneSearch": {
    // Other settings ...
    "PostStartupReindexingOptions": {
        "Enabled": true,
        "IndexesExcludedFromAutomaticReindexing": [ "ExampleExcludedIndex" ], // Indexes to exclude from automatic rebuilding and version checks.
        "CheckIntervalMinutes": 2 // Frequency of assembly version checks (in minutes). Default is 1 minute and the value can not be less than one minute.
    }
}
```

### 2. Update Service Registration

You must pass the configuration as a parameter to the `AddKenticoLucene` method in your service registration:

```csharp
services.AddKenticoLucene(builder =>
{
    builder.RegisterStrategy<AdvancedSearchIndexingStrategy>("DancingGoatExampleStrategy");
    builder.RegisterStrategy<SimpleSearchIndexingStrategy>("DancingGoatMinimalExampleStrategy");
    builder.RegisterStrategy<ReusableContentItemsIndexingStrategy>(nameof(ReusableContentItemsIndexingStrategy));
    builder.RegisterAnalyzer<CzechAnalyzer>("Czech analyzer");
}, configuration);
```

The `configuration` parameter allows the Lucene service to access the `PostStartupReindexingOptions` settings from your `appsettings.json` file.
