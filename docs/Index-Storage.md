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
- There is **no need for automatic reindexing** after deployment.

### Configuration

To configure external storage, register a CMS module that maps the Lucene index path to your storage provider. See the [DancingGoat `LuceneStorageModule`](../examples/DancingGoat/LuceneStorageModule.cs) for a complete reference implementation.

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

The `StorageHelper.MapStoragePath()` call redirects all CMS.IO file operations under the specified path to the external provider. In development or local environments, you can skip the mapping and indexes will be stored on the local file system by default.

> **Note:** For general examples of mapping files to external [storage providers](https://docs.kentico.com/x/44fWCQ), see the Kentico documentation:
>
> - [Azure Blob Storage (SaaS)](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/azure-blob-storage#map-folders-to-a-kentico-managed-azure-blob-storage)
> - [Azure Blob Storage (private cloud / self-hosted)](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/azure-blob-storage#azure-blob-storage-for-private-cloud-deployments)
> - [Amazon S3](https://docs.kentico.com/documentation/developers-and-admins/api/files-api-and-cms-io/file-system-providers/amazon-s3#map-files-to-amazon-storage)

## Local File System

When no external storage is configured, indexes are stored on the local file system under `App_Data/LuceneSearch`. This is suitable for **local development** and **self-hosted single-instance deployments** with persistent disk storage.

Limitations of local storage:

- **Index data is lost on deployment or restart** in environments without persistent disk storage (e.g., [Kentico Xperience SaaS](https://docs.kentico.com/x/saas_overview_xp)).
- **Each instance maintains its own copy** of the index in multi-instance deployments, requiring web farm synchronization.

If you must use local storage in an environment where files do not persist, see [Auto-Reindexing](Auto-Reindexing-After-Deployment.md) for a workaround that rebuilds indexes after deployment based on assembly version changes.

## Related Topics

- [Auto-Scaling Support](Auto-Scaling.md) — how index operations are distributed across multiple instances.
- [Auto-Reindexing](Auto-Reindexing-After-Deployment.md) — workaround for rebuilding indexes when local storage does not persist.
