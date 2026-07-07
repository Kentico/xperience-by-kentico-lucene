# Index Storage

## Overview

Lucene indexes are stored under `App_Data/LuceneSearch` by default. This integration uses the [CMS.IO](https://docs.kentico.com/x/4YfWCQ) abstraction layer for all file operations, which means index storage is not limited to the local file system — it can be mapped to any `CMS.IO`-compatible [storage provider](https://docs.kentico.com/x/44fWCQ) such as [Azure Blob Storage](https://docs.kentico.com/x/5IfWCQ), [Amazon S3](https://docs.kentico.com/x/5YfWCQ) or [custom file system providers](https://docs.kentico.com/x/5ofWCQ).

This makes it possible to:

- **Persist indexes across deployments and restarts**
- **Share indexes across multiple application instances** — all instances read from and write to the same external storage, eliminating the need for synchronization.

## External Storage (Recommended)

Mapping Lucene indexes to an external storage provider is the recommended approach for all cloud and multi-instance deployments.

When external storage is configured:

- Index data **persists across deployments and instance restarts**.
- All application instances **share the same index files** — no duplication, no sync needed.
- Web farm synchronization tasks for index operations are **automatically disabled** (the system detects external storage and skips them).
- There is **no need for reindexing** after deployment.

### Using Automatic Storage Path Mapping

Xperience by Kentico provides a [storage path mapping](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/storage-path-mapping) system that automatically routes registered paths to external storage based on the hosting environment. This is the simplest approach if your project already uses `AddXperienceCloudStoragePathMapping()` (SaaS) or `AddAppServiceStoragePathMapping()` (private cloud).

Since the Lucene index path is not a system-registered path, you need to register it as a custom path **before** the mapping call in `Program.cs`:

#### SaaS deployments

```csharp
using CMS.IO;
using Kentico.Xperience.Cloud;
using Kentico.Xperience.Lucene.Core.Store;

// Register the Lucene index path so it participates in automatic storage mapping
builder.Services.AddStoragePathRegistration(
    $"~/{LuceneStorageConstants.LUCENE_INDEX_PATH}",
    PathType.SharedPersistent);

// Activate automatic storage path mapping (maps all registered SharedPersistent paths to Azure Blob Storage)
builder.Services.AddXperienceCloudStoragePathMapping();
```

#### Private cloud (Azure App Service) deployments

```csharp
using CMS.IO;
using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Lucene.Core.Store;

// Register the Lucene index path so it participates in automatic storage mapping
builder.Services.AddStoragePathRegistration(
    $"~/{LuceneStorageConstants.LUCENE_INDEX_PATH}",
    PathType.SharedPersistent);

// Activate automatic storage path mapping (maps all registered SharedPersistent paths to Azure Blob Storage)
builder.Services.AddAppServiceStoragePathMapping(options =>
{
    // Disable mapping in local development to avoid connecting to Azure
    options.IsMappingEnabled = !builder.Environment.IsDevelopment();

    // Route Lucene indexes to a dedicated container with public access disabled
    options.CreateProviderForPath = (PathRegistration registration) =>
    {
        if (registration.MappedPath.Contains(LuceneStorageConstants.LUCENE_INDEX_PATH))
        {
            return AzureStorageProvider.Create("lucene", publicExternalFolderObject: false);
        }

        // Return null to use the default provider for other paths
        return null;
    };
});
```

With this approach, the Lucene indexes are automatically mapped alongside all other system paths — no custom module is needed. The `CreateProviderForPath` callback lets you route Lucene indexes to a dedicated container and explicitly disable public access (`publicExternalFolderObject: false`).

> **Note:** The `AddStoragePathRegistration` call must appear **before** the mapping method call (`AddXperienceCloudStoragePathMapping` / `AddAppServiceStoragePathMapping`) so that the path is included in the mapping.

### Using a Custom Storage Module

If your project does not use automatic storage path mapping, or you need more control over which environments and containers are used, you can configure storage in a custom CMS module. This approach uses `StorageHelper.MapStoragePath()` directly to route the Lucene index path to your chosen provider.

See the [DancingGoat `LuceneStorageModule`](../examples/DancingGoat/LuceneStorageModule.cs) for a complete reference implementation.

```csharp
using CMS;
using CMS.DataEngine;
using CMS.IO;

using Kentico.Xperience.AzureStorage;
using Kentico.Xperience.Lucene.Core.Store;

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

The `StorageHelper.MapStoragePath()` call redirects all CMS.IO file operations under the specified path to the external provider. In development or local environments, you can skip the mapping and indexes will be stored on the local file system by default.

> **Note:** For general examples of mapping files to external [storage providers](https://docs.kentico.com/x/44fWCQ), see the Kentico documentation:
>
> - [Azure Blob Storage (SaaS)](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/azure-blob-storage#azure-blob-storage-for-kenticos-saas)
> - [Azure Blob Storage (private cloud / self-hosted)](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/azure-blob-storage#azure-blob-storage-for-private-cloud-deployments)
> - [Amazon S3](https://docs.kentico.com/x/5YfWCQ)
> - [Custom file system providers](https://docs.kentico.com/x/5ofWCQ)

## Local File System

When no external storage is configured, indexes are stored on the local file system under `App_Data/LuceneSearch`. This is suitable for **local development** and **self-hosted single-instance deployments** with persistent disk storage.

Limitations of local storage:

- **Index data is lost on deployment or restart** in environments without persistent disk storage (e.g., [Kentico Xperience SaaS](https://docs.kentico.com/x/saas_overview_xp)).
- **Each instance maintains its own copy** of the index in multi-instance deployments, requiring web farm synchronization.
