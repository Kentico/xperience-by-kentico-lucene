# Automatic Lucene Reindexing after Deployment

## Overview

### Using External Storage (Recommended for SaaS/Cloud)

When Lucene indexes are stored on **external shared storage** (such as Azure Blob Storage or Amazon S3) via the CMS.IO abstraction, index data persists across deployments and instance restarts. In this case, **automatic reindexing based on assembly version is not required** — all application instances share the same index files, so a new deployment does not result in lost index data.

To configure external storage for Lucene indexes, register a CMS module that maps the Lucene index path to the appropriate storage provider. See the [DancingGoat `LuceneStorageModule`](../examples/DancingGoat/LuceneStorageModule.cs) for a reference implementation:

```csharp
[assembly: RegisterModule(typeof(LuceneStorageModule))]

public class LuceneStorageModule : Module
{
    private const string CONTAINER_NAME = "lucene";

    public LuceneStorageModule() : base(nameof(LuceneStorageModule)) { }

    protected override void OnInit()
    {
        base.OnInit();

        if (Environment.IsQa() || Environment.IsProduction() /* ... */)
        {
            // Map Lucene indexes to Azure Blob Storage in cloud environments
            var provider = AzureStorageProvider.Create();
            provider.CustomRootPath = CONTAINER_NAME;
            provider.PublicExternalFolderObject = false;
            StorageHelper.MapStoragePath($"~/{LuceneStorageConstants.LUCENE_INDEX_PATH}/", provider);
        }
    }
}
```

### Using Local File System

Without external storage, Lucene integration **requires persistent file system access**. In environments like **Kentico Xperience SaaS**, which do not provide persistent file system storage for indexes, the index data is lost during deployments or restarts. As a result, indexes must be rebuilt after each deployment to ensure correct search functionality.

To address this issue, a configuration option is available to enable automatic reindexing based on assembly version. The solution adds a hosted service that periodically checks the application's assembly version, compares it with the stored assembly version for each index, and updates it after a rebuild. If the versions differ, the index is automatically rebuilt.

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
