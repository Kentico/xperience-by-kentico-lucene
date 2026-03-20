# Auto-scaling Support

This integration supports Xperience's [Auto-scaling support](https://docs.kentico.com/x/SI3WCQ). This means that your application can distribute computing among multiple applications and therefore increase the number of requests the project can handle.

## Local File System Storage

When indexes are stored on the **local file system**, each application instance maintains its own copy of the index. The integration uses Xperience web farm tasks to synchronize index operations (rebuild, delete) across all instances automatically.

## External Storage (Azure Blob Storage, Amazon S3)

When Lucene indexes are mapped to an **external shared storage** via [CMS.IO](https://docs.kentico.com/x/4YfWCQ) (e.g., [Azure Blob Storage](https://docs.kentico.com/x/5IfWCQ) or [Amazon S3](https://docs.kentico.com/x/5YfWCQ)), all instances share the same underlying index files. In this case, **web farm synchronization tasks are automatically disabled** — the system detects external storage and skips creating web farm tasks for index operations, since all instances already see the same data.

For instructions on configuring external storage, see [Index Storage](Index-Storage.md).