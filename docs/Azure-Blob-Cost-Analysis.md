# Azure Blob Storage Cost Analysis — Excessive List Blobs Operations

## Summary

The Lucene index is stored on Azure Blob Storage through Kentico's `CMS.IO`
abstraction (`CmsIODirectory`, `CmsIOIndexInput`). On Azure Blob, the `CMS.IO`
operations `Directory.Exists`, `Directory.GetDirectories`, and
`DirectoryInfo.GetFiles` are all backed by **List Blobs** calls — there is no
real directory concept on blob storage, so listing by prefix is the only way to
answer "does this folder exist / what's in it".

**List Blobs is billed under the "List and Create Container operations"
category — the expensive transaction tier (~10× the cost of a basic read).**

The root cause of the cost spike: **every search request opens a brand-new
`DirectoryReader` with no caching**, triggering multiple List Blobs operations
per query.

## Root cause: every search opens a new `DirectoryReader`, with no caching

In `DefaultLuceneSearchService.UseSearcher`
(`src/Kentico.Xperience.Lucene.Core/Search/DefaultLuceneSearchService.cs:30-47`),
**every single search request** does the following, with nothing cached:

```csharp
var storage = index.StorageContext.GetPublishedIndex();   // (1) enumerates dirs
if (!CmsDirectory.Exists(storage.Path)) { ... }            // (2) List Blobs
using LuceneDirectory indexDir = CmsIODirectory.Open(storage.Path);  // (3) Exists check
using var reader = DirectoryReader.Open(indexDir);         // (4) ListAll + read segments
```

### List Blobs operations per search

1. **`GetPublishedIndex()`** → `GenerationStorageStrategy.GetExistingIndices`
   (`ILuceneIndexStorageStrategy.cs:78-103`) calls `CmsDirectory.Exists(root)`
   **and** `CmsDirectory.GetDirectories(root)` — at least one List Blobs, every
   call.
2. **`CmsDirectory.Exists(storage.Path)`** — another List Blobs.
3. **`CmsIODirectory.Open`** → constructor calls `EnsureDirectoryExists()` →
   `CmsDirectory.Exists(DirectoryPath)` (`CmsIODirectory.cs:68, 261-267`) —
   another List Blobs.
4. **`DirectoryReader.Open`** → calls `ListAll()` → `freshInfo.GetFiles()`
   (`CmsIODirectory.cs:86-93`) — another List Blobs, then reads `segments_N` and
   opens each segment file.

So **one search query = ~4+ List Blobs operations**, plus blob reads. The
faceted/drill-sideways variants (`UseSearcherWithFacets`,
`UseSearcherWithDrillSideways`) repeat the whole thing **twice** because they
also `Open` and read the taxonomy directory the same way.

### No reader reuse

Critically, the `reader` is wrapped in `using` and **disposed at the end of
every call**. There is no `SearcherManager`/`ReferenceManager`, no
`DirectoryReader.OpenIfChanged`, and no NRT reader reuse anywhere. The only
`IProgressiveCache` usage is for index *config* metadata in
`DefaultLuceneIndexManager`/`DefaultLuceneClient` — never for the Lucene
reader/directory.

On a busy site, List Blobs scales linearly with search traffic:
**10 searches/sec ≈ 40+ list ops/sec ≈ ~3.5M list ops/day** on the priciest
meter.

## Why this matches the bill

List Blobs is billed under the "List and Create Container operations" category —
the expensive transaction tier (~10× a basic read). This design turns it into
per-request overhead, which is exactly the "an app re-listing the container
instead of caching" anti-pattern.

## Secondary contributors

- **`ListAll()` always re-lists.** It deliberately calls
  `CmsDirectoryInfo.New(...)` fresh each time (intentional per the comment), so
  even within a reader's lifetime there's no reuse.
- **`Sync()`** (`CmsIODirectory.cs:178-204`) opens+closes a stream per file —
  extra read transactions on every commit, though smaller than the list cost.
- **Retention/cleanup** (`EnforceRetentionPolicy` → `GetExistingIndices` +
  `PerformCleanup`) lists directories too, but that's infrequent.

## Recommended fixes (in impact order)

1. **Cache and reuse the reader/searcher.** Replace the per-call
   `DirectoryReader.Open(...)` with a `SearcherManager` (or cached
   `DirectoryReader` refreshed via `DirectoryReader.OpenIfChanged`) held per
   index, refreshed only after an index commit/publish. This is the single
   biggest win — it collapses steps (3) and (4) from per-request to per-refresh.
2. **Cache `GetPublishedIndex()` / `GetExistingIndices()`** result and
   invalidate it on publish, instead of enumerating blobs on every search
   (steps 1 and 2).
3. **Drop the redundant `Exists` checks** in the search path — the "ensure
   index" branch only needs to run on a cold/missing index, not on the hot path.

Start with #1 — it directly removes the `ListAll()` storm and the redundant
`Exists` calls per query.

## Caveat / verification

This analysis infers that `CMS.IO`'s `Directory.Exists` / `GetDirectories` /
`GetFiles` map to Azure List Blobs — that is the standard behavior of Kentico's
Azure storage provider, but it is **not defined in this repository**. Confirm it
in the Azure portal via **Metrics → Transactions** split by **API Name** (and by
**caller IP** / **Authentication** if available) to see which operation and
client drives the count. Enabling **diagnostic logs / resource logs** lets you
trace individual operations back to a REST call and caller.

The per-operation prices referenced above are Microsoft's *sample* documentation
figures, not live pricing for a specific region/account tier — check the
[Azure Blob Storage pricing page](https://azure.microsoft.com/pricing/details/storage/blobs/)
for actual rates.
