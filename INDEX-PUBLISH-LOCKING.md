# Index generation locking — problem, fix, and call flow

Reference for the uncommitted work on `main` that makes publishing an index generation
reliable on Windows. Written against the working tree, not a released version.

---

## 1. The storage model

Each index lives under `<LUCENE_INDEX_PATH>/<IndexName>/` as a set of **generation
folders**. The folder *name* encodes both the generation number and whether it is
published:

```
App_Data/LuceneSearch/MyIndex/
├── i-g0000001-p_True            <- generation 1, published  (searches read this)
├── i-g0000001-p_True_taxonomy
├── i-g0000002-p_False           <- generation 2, being built (writers write here)
├── i-g0000002-p_False_taxonomy
└── .trash/                      <- generations evicted by the retention policy
```

Two consequences drive everything below:

1. **Publishing is a rename.** `p_False` → `p_True`, via `CmsDirectory.Move`.
2. **Retention is also a rename.** Evicting an old generation moves it into `.trash`.

Path formatting and parsing live in `GenerationStorageStrategy`
(`src/Kentico.Xperience.Lucene.Core/Indexing/ILuceneIndexStorageStrategy.cs`).

---

## 2. The problem

`LuceneIndexSearcherProvider` caches a `SearcherManager` per index so that searches do not
re-open a `DirectoryReader` — and, on Azure Blob Storage, do not pay a List Blobs
transaction — on every query. A cached reader holds **open OS file handles inside the
generation folder**.

On Windows, `Directory.Move` fails with `IOException` *"Access to the path … is denied"*
while any handle is open inside the folder. So:

> A rebuild finishes, tries to publish generation 2, and the rename fails because a cached
> reader is still open on generation 1 (or on generation 2 itself).

The failure was silent and cascading:

| # | Symptom | Cause |
|---|---|---|
| 1 | Generation never becomes `p_True`; searches return nothing | Invalidation ran *after* the rename, so the handles were still open during it |
| 2 | One locked index left *every* index in the batch unpublished | The publish loop had no per-index error handling; the first throw aborted the rest |
| 3 | A reader got opened over the folder being rebuilt | `GetPublishedIndex()` falls back to a **generation-1 unpublished** model when nothing is published — the exact folder the rebuild writes into and then renames |
| 4 | Index folder published, taxonomy folder not | Two independent `Move` calls with no rollback between them |
| 5 | Published generation pointed at the *unpublished* taxonomy folder | `GetExistingIndices` ignored the taxonomy folder's own published flag |
| 6 | The just-published generation was immediately trashed | `IndexRetentionPolicy(0)` — `EnforceRetentionPolicy` schedules removal of *every* generation when `kept == 0` |

---

## 3. The fix, in layers

### 3.1 Invalidate *before* the rename, and wait for the handles to close

Every site that renames a generation folder now releases the cached readers first **and
waits until they are actually disposed**, rather than invalidating afterwards and hoping.

The lease count needed for this was already tracked exactly, so the wait is a real event,
not a sleep:

- `CachedIndex` gained `releaseCompleted` + `WaitForRelease(TimeSpan)`. The flag is set
  under `syncLock` at the very end of `DisposeResources()`, followed by
  `Monitor.PulseAll` — set *after* the handles close, so a waiter that observes the signal
  has observed the close. A deadline-based loop means spurious wakeups do not shorten the
  wait. Reusing the existing `syncLock` with `Monitor` avoids allocating a kernel handle
  per generation and the "who disposes the event" problem an `ManualResetEventSlim` would
  bring.
- `LuceneIndexSearcherProvider.InvalidateAndWait(indexName, timeout)` retires the entry and
  waits. `Invalidate` is now `InvalidateAndWait(name, TimeSpan.Zero)` — still
  non-blocking.
- `LuceneSearchCacheInvalidator.InvalidateAndWaitForRelease(index)` adds the web farm
  broadcast on top and returns whether the readers drained.

Timeout: `LuceneIndexSearcherProvider.ReaderReleaseTimeout` = **10 s**. On expiry the
caller proceeds anyway and `DefaultLuceneTaskProcessor` logs a warning — indexing must not
stall behind one stuck query.

> **Scope limit.** The wait only covers *this process*. On a web farm, another server's
> reader also holds handles on shared storage, and its invalidation arrives asynchronously
> via `InvalidateSearchIndexWebFarmTask`. That is what the retry in §3.2 is now for.

### 3.2 Retry only what a retry can fix

`MoveWithRetry` is now a **backstop** for handles this process does not own (another web
farm node, an antivirus scanner, an open writer) — 3 attempts, 100 ms × attempt backoff.

The retry predicate `IsFolderLocked` matches only lock-shaped Win32 codes
(`ERROR_ACCESS_DENIED` 0x05, `ERROR_SHARING_VIOLATION` 0x20, `ERROR_LOCK_VIOLATION` 0x21)
and explicitly excludes `FileNotFoundException` / `DirectoryNotFoundException` — both
derive from `IOException`, which is why a missing folder previously burned all 5 attempts
plus ~1 s of sleeping before rethrowing. A full disk or a throttled blob request now fails
on the first attempt.

> **Blob-storage side effect, worth knowing.** CMS.IO's Azure provider raises
> `IOException`s constructed without a Win32 code, so `HResult & 0xFFFF` matches none of
> the three constants and **the retry effectively will not fire on blob storage**. That is
> the right behavior — blobs have no handle locking, and a half-completed copy-and-delete
> (which is how a "rename" is implemented there) is not safe to repeat blindly. This rests
> on an inference about how that provider raises errors; it is not verifiable from this
> repository.

### 3.3 Never open a reader over the folder being rebuilt

`GetPublishedIndex()`'s fallback model is the root cause of item 3 above. It is kept for
existing callers, but reimplemented on top of a new, honest accessor:

- `IndexStorageContext.TryGetPublishedIndex()` returns **`null`** when nothing is
  published, instead of fabricating an unpublished generation-1 model.
- `CachedIndex.Open` uses it and, on `null`, returns `OpenEmpty(analyzer)` — a
  `RAMDirectory` with a single empty commit, plus an empty in-memory taxonomy for faceted
  queries. Searches return zero results, touch no storage, and can lock nothing. The entry
  is invalidated when the real generation publishes, so the next search opens it.

### 3.4 Consistency and blast radius

- **Per-index error handling** in the publish loop: one locked folder no longer leaves the
  other indexes in the batch unpublished.
- **Taxonomy rollback**: if the index folder moves but the taxonomy folder does not,
  `PublishIndex` moves the index folder back, so the generation stays consistently
  unpublished and can be retried. A failed rollback is logged.
- **Taxonomy flag**: `GetExistingIndices` now mirrors the taxonomy folder's own published
  flag when it parsed successfully, falling back to the index folder's flag otherwise.
- **`IndexRetentionPolicy(0)` → `(1)`** in `LuceneIndex.cs`. With `0`, `kept` never
  decrements and every generation — including the one just published — is scheduled for
  removal. This is a correctness fix, but note it has an ongoing **storage cost**: one
  published generation is now retained permanently, and peak usage roughly doubles during a
  rebuild. On blob storage that is the line item to watch.

---

## 4. Call flow: building and publishing a generation

### 4.1 Rebuild is requested (admin UI, or the automatic reindexing service)

```
DefaultLuceneClient.Rebuild(indexName, ct)                  DefaultLuceneClient.cs:132
└── RebuildInternal(luceneIndex, ct)                        DefaultLuceneClient.cs:199
    ├── cacheAccessor.Remove(CACHEKEY_STATISTICS)
    ├── [local storage only] webFarmService.CreateTask(ResetIndexWebFarmTask)
    │       └── other servers → §4.4
    │
    ├── searchCacheInvalidator.InvalidateAndWaitForRelease(index)      <-- FIX §3.1
    │   ├── LuceneIndexSearcherProvider.InvalidateAndWait(name, 10s)
    │   │   ├── cache.TryRemove(name)          (under creationLock)
    │   │   ├── CachedIndex.Retire()
    │   │   │   └── DisposeResources()         if leaseCount == 0
    │   │   │       ├── taxonomyReader.Dispose() / taxonomyDir.Dispose()
    │   │   │       ├── searcherManager.Dispose()
    │   │   │       ├── indexDir.Dispose()     <-- OS handles close here
    │   │   │       └── releaseCompleted = true; Monitor.PulseAll
    │   │   └── CachedIndex.WaitForRelease(10s)
    │   │       └── blocks until the pulse above, or the deadline
    │   └── Broadcast(index)                   InvalidateSearchIndexWebFarmTask, external storage only
    │
    ├── luceneIndexService.ResetIndex(index)                 DefaultLuceneIndexService.cs:84
    │   ├── index.StorageContext.EnforceRetentionPolicy()
    │   │   ├── storageStrategy.GetExistingIndices(root)
    │   │   ├── storageStrategy.ScheduleRemoval(old)         <-- Move to .trash, needs handles closed
    │   │   └── storageStrategy.PerformCleanup(root)
    │   └── UseWriter(index, …, GetNextGeneration(), OpenMode.CREATE)
    │       ├── FileLock.WaitForLock(...)
    │       ├── CmsIODirectory.Open(storage.Path)            creates i-gNNNNNNN-p_False
    │       └── new IndexWriter(...)                         writes the empty commit
    │
    ├── executor.GetWebPageResult(...) / GetResult(...)      enumerate content to index
    ├── MapToEventItem / MapToEventReusableItem
    └── LuceneQueueWorker.EnqueueLuceneQueueItem(
            new LuceneQueueItem(item, LuceneTaskType.PUBLISH_INDEX, indexName))   for each item
```

`RebuildInternal` returns here. Nothing is published yet — and because
`TryGetPublishedIndex()` returns `null` in this window, any search that arrives now gets
the empty in-memory index from §3.3 rather than a reader over `i-gNNNNNNN-p_False`.

### 4.2 The queue worker drains (background thread, 10 s interval)

```
LuceneQueueWorker (ThreadQueueWorker, DefaultInterval = 10000)
└── DefaultLuceneTaskProcessor.ProcessLuceneTasks(items, ct, maximumBatchSize: 100)
    │
    ├── foreach batch: ProcessLuceneBatch(batch, batchResults, ct)
    │   └── foreach group (by index name)
    │       ├── GetDocument(queueItem)
    │       │   ├── indexManager.GetRequiredIndex(name)
    │       │   ├── serviceProvider.GetRequiredStrategy(index)
    │       │   ├── strategy.MapToLuceneDocumentOrNull(item)
    │       │   └── AddBaseProperties(item, document)        content type, language, guid, URL
    │       ├── luceneClient.DeleteRecords(deleteIds, name)
    │       │   └── luceneIndexService.UseWriter(..., GetLastGeneration(true))
    │       ├── luceneClient.UpsertRecords(documents, name, ct)
    │       │   └── UseIndexAndTaxonomyWriter / UseWriter(..., GetLastGeneration(true))
    │       ├── batchResults.ModifiedIndices.Add(index)      if anything changed
    │       └── batchResults.PublishedIndices.Add(index)     if any task is PUBLISH_INDEX
    │
    ├── foreach index in PublishedIndices                    <-- the publish loop
    │   ├── ModifiedIndices.Add(index)
    │   └── try                                              <-- FIX §3.4, per-index isolation
    │       ├── storage = index.StorageContext.GetNextOrOpenNextGeneration()
    │       ├── searchCacheInvalidator.InvalidateAndWaitForRelease(index)   <-- FIX §3.1
    │       │   └── (as expanded in §4.1; logs a warning on timeout)
    │       ├── index.StorageContext.PublishIndex(storage)
    │       │   └── GenerationStorageStrategy.PublishIndex(storage)
    │       │       ├── FileLock.WaitForLock(...)
    │       │       ├── MoveWithRetry(storage.Path, published.Path)         <-- FIX §3.2
    │       │       │   ├── CmsDirectory.Move(...)           p_False -> p_True
    │       │       │   └── on IOException: IsFolderLocked(ex) ? sleep+retry (max 3) : log + throw
    │       │       ├── if taxonomy exists:
    │       │       │   └── try MoveWithRetry(taxonomyPath, published.TaxonomyPath)
    │       │       │       └── catch IOException             <-- FIX §3.4, rollback
    │       │       │           ├── CmsDirectory.Move(published.Path, storage.Path)
    │       │       │           └── rethrow
    │       │       └── finally: fileLock.Release()
    │       └── catch: eventLogService.LogException(...)      keep publishing the other indexes
    │
    └── foreach index in ModifiedIndices
        └── searchCacheInvalidator.Invalidate(index)          makes the new generation visible
```

The final loop is what actually exposes the new generation: the next search misses the
cache and opens a reader over the now-`p_True` folder.

### 4.3 A search, before and after the publish

```
DefaultLuceneSearchService.UseSearcher / UseSearcherWithFacets / UseSearcherWithDrillSideways
└── LuceneIndexSearcherProvider.Acquire(index, withTaxonomy)
    └── AcquireInternal(indexName, () => CachedIndex.Open(index, indexService), withTaxonomy)
        ├── GetOrCreate(indexName, open)                     cache hit → reuse, no storage I/O
        │   └── on miss: CachedIndex.Open(index, indexService)
        │       ├── index.StorageContext.TryGetPublishedIndex()           <-- FIX §3.3
        │       │   └── storageStrategy.GetExistingIndices(root)
        │       │       .Where(IsPublished).MaxBy(Generation)
        │       ├── null?  → CachedIndex.OpenEmpty(index.LuceneAnalyzer)
        │       │            RAMDirectory + empty commit; locks nothing
        │       └── else   → CmsIODirectory.Open(published.Path)
        │                    + new SearcherManager(dir, null)
        │                    + lazy OpenTaxonomy(...) on the first faceted acquisition
        └── CachedIndex.TryAcquire(withTaxonomy)
            ├── leaseCount++            (null if already retired → caller retries, max 3)
            ├── searcherManager.Acquire()
            ├── EnsureTaxonomyReader()  if withTaxonomy
            └── new SearcherLease(searcher, taxonomy, release)
                └── on Dispose: searcherManager.Release(searcher) → ReleaseLease()
                    └── DisposeResources() if retired && leaseCount == 0
                        └── this is the path WaitForRelease is waiting on
```

### 4.4 Other web farm servers

```
ResetIndexWebFarmTask.ExecuteTask()
├── luceneIndexManager.GetRequiredIndex(IndexName)
├── searcherProvider.InvalidateAndWait(IndexName, ReaderReleaseTimeout)   <-- FIX §3.1
│       local only — every server runs this task itself, so no re-broadcast
└── luceneIndexService.ResetIndex(luceneIndex)
```

`InvalidateSearchIndexWebFarmTask` and `DeleteIndexWebFarmTask` follow the same shape:
release this server's readers, then act locally.

---

## 5. Files touched

| File | Change |
|---|---|
| `Search/CachedIndex.cs` | `TryGetPublishedIndex` usage, `OpenEmpty`, `OpenEmptyTaxonomy`, `releaseCompleted` + `WaitForRelease` |
| `Search/LuceneIndexSearcherProvider.cs` | `ReaderReleaseTimeout`, `InvalidateAndWait` |
| `Search/LuceneSearchCacheInvalidator.cs` | `InvalidateAndWaitForRelease`, `Broadcast` extracted |
| `Indexing/IndexStorageContext.cs` | `TryGetPublishedIndex`; `GetPublishedIndex` reimplemented on it |
| `Indexing/ILuceneIndexStorageStrategy.cs` | `MoveWithRetry`, `IsFolderLocked`, taxonomy rollback, taxonomy published-flag fix |
| `Indexing/DefaultLuceneClient.cs` | Invalidate-and-wait before reset and before delete |
| `Indexing/DefaultLuceneTaskProcessor.cs` | Invalidate-and-wait inside the publish loop; per-index try/catch |
| `Indexing/LuceneIndex.cs` | `IndexRetentionPolicy(0)` → `(1)` |
| `Scaling/ResetIndexWebFarmTask.cs` | Invalidate-and-wait before the local reset |

Tests: `IndexStorageContextTests` (3 cases for `TryGetPublishedIndex`) and
`LuceneIndexSearcherProviderTests` (`OpenEmpty` searchable + faceted; 4 cases for
`InvalidateAndWait`, including a lease held past the timeout and one released
concurrently). 60 tests pass; the solution builds with 0 warnings.

---

## 6. Known follow-up

The whole bug class exists because "published" is encoded in the **folder name**, making a
publish a rename. Replacing that with a marker blob — a `current.json` naming the published
generation — would make publishing a single small write: no rename, no locking, no retry, no
rollback path, and none of the per-publish full-generation copy cost that renames incur on
blob storage. It touches `GetExistingIndices`, `FormatPath` / `FormatTaxonomyPath`,
`PublishIndex`, and retention, and needs a migration for existing on-disk layouts — so it
is a separate piece of work, not part of this fix.

A smaller one: `IndexRetentionPolicy` is hardcoded in the `LuceneIndex` constructor. A
blob-hosted deployment has no way to opt down from retaining a generation; threading it
through `CMSLuceneSearch` options would fix that.
