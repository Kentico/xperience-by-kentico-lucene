# Auto-scaling Support

This integration supports Xperience's [Auto-scaling support](https://docs.kentico.com/x/SI3WCQ). This means that your application can distribute computing among multiple applications and therefore increase the number of requests the project can handle.

## Local File System Storage

When indexes are stored on the **local file system**, each application instance maintains its own copy of the index. The integration uses Xperience web farm tasks to synchronize index operations (rebuild, delete) across all instances automatically.

## External Storage (Azure Blob Storage, Amazon S3)

When Lucene indexes are mapped to **external shared storage** via CMS.IO (e.g., Azure Blob Storage or Amazon S3), all instances share the same underlying index files. In this case, **web farm synchronization tasks are automatically disabled** — the system detects external storage and skips creating web farm tasks for index operations, since all instances already see the same data.

To configure external storage for Lucene indexes, create a CMS module that maps the Lucene index path to the appropriate storage provider. See the [DancingGoat `LuceneStorageModule`](../examples/DancingGoat/LuceneStorageModule.cs) for a complete reference implementation showing how to map the `App_Data/LuceneSearch` path to Azure Blob Storage in cloud environments:

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
            var provider = AzureStorageProvider.Create();
            provider.CustomRootPath = CONTAINER_NAME;
            provider.PublicExternalFolderObject = false;
            StorageHelper.MapStoragePath($"~/{LuceneStorageConstants.LUCENE_INDEX_PATH}/", provider);
        }
    }
}
```

With external storage, there is no need to configure web farm synchronization for Lucene index operations — all instances share the same index data automatically.