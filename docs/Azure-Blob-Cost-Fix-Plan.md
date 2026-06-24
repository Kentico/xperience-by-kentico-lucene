# Plan: Eliminate Per-Search Azure List Blobs Storms

> Companion to [Azure-Blob-Cost-Analysis.md](./Azure-Blob-Cost-Analysis.md), which
> documents the root cause. This document is the implementation plan for the fix.

## Goal

Stop opening a fresh `DirectoryReader` (and re-enumerating blobs) on every
search. Serve searches from a cached `IndexSearcher`/reader that is rebuilt only
when the index's published generation changes.

## Key lifecycle facts that shape the design

- All search/index services are **singletons**
  (`LuceneStartupExtensions.cs:69-79`) — safe to hold a shared cache.
- `LuceneIndex` instances are themselves cached/recreated by
  `DefaultLuceneIndexManager` (10-min `IProgressiveCache`), so a reader cache
  must be keyed by **index name**, not by `LuceneIndex` object identity.
- **Publishing creates a *new generation directory***
  (`DefaultLuceneTaskProcessor.cs:54-55` → `GetNextOrOpenNextGeneration` +
  `PublishIndex`; path goes `i-g0000001-p_true` → `i-g0000002-p_true`). So
  `DirectoryReader.OpenIfChanged` alone will **not** pick up new content — the
  *path itself* changes. The cache must be invalidated when a new generation is
  published.
- The web farm tasks (`ResetIndexWebFarmTask`, `DeleteIndexWebFarmTask`,
  `ProcessLuceneTasksWebFarmTask`) all run the publish/reset/delete logic
  **locally on each server**, so invalidation can be purely local — no extra web
  farm messaging needed.

## Phase 1 — Cached searcher provider (the big win)

**New file:**
`src/Kentico.Xperience.Lucene.Core/Search/LuceneIndexSearcherProvider.cs`
(registered as singleton).

Responsibilities, keyed by `IndexName`:

- Cache the resolved **published `IndexStorageModel`** (path + generation) so the
  per-search `GetPublishedIndex()` → `GetExistingIndices()` blob enumeration is
  skipped on the hot path.
- Hold a `SearcherManager` bound to that published path, plus (when needed) the
  `DirectoryTaxonomyReader` and the two `CmsIODirectory` instances.
- Expose an acquire/release API:

  ```csharp
  SearcherLease Acquire(LuceneIndex index);   // returns IndexSearcher (+ optional taxonomyReader)
  void Release(SearcherLease lease);           // ref-counted; safe under concurrency
  void Invalidate(string indexName);           // marks entry stale
  ```

- On `Acquire`: if a non-stale cached entry exists, `SearcherManager.Acquire()`
  from it — **zero List Blobs**. If stale/missing, resolve the published path
  (one bounded blob enumeration), open directories + managers, cache, then
  acquire.
- Use `ConcurrentDictionary` + per-index lock (or `Lazy`) for thread safety.
  Defer disposal of a swapped-out `SearcherManager`/readers until in-flight
  leases are released (ref count), so concurrent searches never hit a disposed
  stream.

**Why immutable-generation makes this safe:** a published generation directory
is read-only until the next publish, so a cached reader stays valid for that
generation's whole lifetime — no `OpenIfChanged` polling required; invalidation
is purely event-driven.

## Phase 2 — Rewire the search service

**File:** `Search/DefaultLuceneSearchService.cs`

- Inject `LuceneIndexSearcherProvider`.
- Replace the body of `UseSearcher`, `UseSearcherWithFacets`,
  `UseSearcherWithDrillSideways` to `Acquire`/`Release` from the provider instead
  of `CmsIODirectory.Open(...)` + `DirectoryReader.Open(...)` +
  `new DirectoryTaxonomyReader(...)`.
- Keep the cold-start "ensure index exists" branch, but run it only on cache
  miss, not on every call.

## Phase 3 — Invalidation hooks

Call `Invalidate(indexName)` at the three points that change the published
generation. To avoid a DI cycle (`LuceneIndexSearcherProvider` depends on
`ILuceneIndexService`), the provider is **not** injected into
`DefaultLuceneIndexService`; instead the hooks live at the call sites:

1. **Publish** — `DefaultLuceneTaskProcessor.ProcessLuceneTasks`, after
   `PublishIndex(storage)`.
2. **Reset** — `DefaultLuceneClient.RebuildInternal` after `ResetIndex`
   (originator) and `ResetIndexWebFarmTask.ExecuteTask` (non-external replicas).
3. **Delete** — `DefaultLuceneClient.DeleteIndex` (originator) and
   `DeleteIndexWebFarmTask.ExecuteTask` (non-external replicas).

### External-storage multi-server gap (added during implementation)

For **external (shared) storage** — the Azure Blob scenario — Kentico skips web
farm replication of the publish/reset/delete operation (only the originating
server performs it). A purely local invalidation would therefore leave the other
servers serving a stale (or moved-to-`.trash`) generation. To close this, a new
lightweight broadcast task **`InvalidateSearchIndexWebFarmTask`** (registered in
`LuceneSearchModule`) is created by the originator **only when storage is
external**; each receiving server calls `Invalidate` and does not re-broadcast.

Rule applied consistently at all three chokepoints: *invalidate locally on the
acting server; if storage is external, broadcast `InvalidateSearchIndexWebFarmTask`
to the others.* For non-external storage the operation is already replicated to
each server via its own web farm task, which invalidates that server's cache
locally.

## Phase 4 — Lifetime & disposal

- Implement `IDisposable` on the provider; dispose all cached
  managers/directories on shutdown.
- Register in `LuceneStartupExtensions.cs` as `AddSingleton`.

## Phase 5 — Tests

Implemented in `tests/.../Search/LuceneIndexSearcherProviderTests.cs` (12 tests).
Because a real `LuceneIndex` cannot be constructed without the Kentico service
host, the provider was given a small testable seam: `CachedIndex` is decoupled
from `LuceneIndex`/CMS.IO (an injected "open taxonomy" delegate) and the provider
exposes an internal string-keyed `AcquireInternal(name, openFactory, withTaxonomy)`.
Tests drive the lifecycle with **real Lucene readers over an in-memory
`RAMDirectory`** (a `TrackingRamDirectory` records disposal) — no disk, no CMS.IO.

Coverage:
- Cache hit returns the same searcher instance (no reopen).
- Invalidate → next acquire opens a new generation; old generation disposed.
- Invalidate/retire with an active lease defers disposal until the lease is
  released (in-use reader never disposed).
- Retire with no leases disposes immediately; `TryAcquire` after retire returns
  `null`; faceted acquire without a taxonomy factory throws without leaking a lease.
- `Dispose()` retires all entries; `AcquireInternal` after dispose throws.
- Concurrency stress: 8 readers acquiring/releasing while another thread
  invalidates — never observes a disposed reader, and every opened generation is
  disposed once drained.

## Expected outcome

List Blobs on the search path drops from **~4+ per query** to **~0 between
publishes** (one bounded burst per generation change). On a site doing thousands
of searches between reindexes, that's roughly a 3–4 orders-of-magnitude cut in
List-tier transactions.

## Risks / decisions

- **Memory:** cached readers keep file handles/segment data resident per index —
  acceptable and standard for Lucene; bounded by index count.
- **Retention policy is `IndexRetentionPolicy(0)`** (`LuceneIndex.cs:109`) — old
  generations get moved to `.trash` aggressively, so a stale cached reader would
  break. The Phase 3 invalidation-on-publish is what makes this safe; this
  ordering must be correct (invalidate *after* publish, dispose old reader
  *after* leases drain).
- **Taxonomy readers** must be cached/invalidated in lockstep with the main
  reader (same generation).

## Scope options

- **Full (recommended):** Phases 1–5 — eliminates both the reader re-open *and*
  the `GetPublishedIndex` enumeration per search.
- **Minimal interim:** cache only the published `IndexStorageModel` with a short
  TTL + invalidation, and drop the redundant `Exists` checks — smaller change,
  removes ~2 of the ~4 list ops per search but still re-opens the reader each
  time.

---

# Follow-up: Round 2 — reducing residual List Blobs & Get Blob Properties

> Added after testing showed the searcher cache (Phases 1–5 above) did **not**
> meaningfully lower the Azure bill. This section records the re-evaluation and
> the additional optimizations applied.

## Re-evaluation — why the searcher cache alone didn't move the bill

The searcher cache is correct, but it only covers the **search read path**. Real
metrics showed **List Blobs** and **Get Blob Properties** still dominating, which
traces to paths the cache never touches:

- **Get Blob Properties scales with file count.** Lucene calls `FileLength()` and
  `OpenInput()` **per segment file** on every reader/writer open and every merge
  (e.g. `IndexFileDeleter` checks every file when a writer opens). A many-file
  index means many HEAD requests on each open.
- **List Blobs scales with reopen + publish frequency.** Every publish →
  cache invalidate → next search reopens the reader → `ListAll()` + per-file
  opens. The indexing path also enumerates (`GetExistingIndices` =
  `Directory.Exists` + `GetDirectories`), and `PublishIndex` performs a
  `CmsDirectory.Move` (no native rename on Azure Blob → list + copy + delete of
  every file in the generation).

So the dominant driver is **file count × open/publish frequency**, not searches.

## Changes applied

| Area | File | Change | Saves |
| --- | --- | --- | --- |
| Durability | `Store/CmsIODirectory.cs` | `Sync()` is now a true no-op (blobs are durable on stream close; files are already closed before Sync runs) | one **read** transaction per file on every commit/merge |
| Enumeration | `Store/CmsIODirectory.cs` | `ListAll()` no longer re-checks `Directory.Exists` (the ctor already ensured it; `GetFiles()` does the listing) | one **List Blobs** per `ListAll()` |
| Enumeration | `Store/CmsIODirectory.cs` + `Search/LuceneIndexSearcherProvider.cs` | New `CmsIODirectory.OpenForRead(path)` skips the ctor `Directory.Exists` for read opens; the searcher/taxonomy readers use it (existence already ensured on cold start) | one **List Blobs** per reader/taxonomy open |
| Per-file open | `Store/CmsIODirectory.cs` + `Store/CmsIOIndexInput.cs` | `OpenInput()` drops the `File.Exists` pre-check; `CmsIOIndexInput` rethrows `FileNotFoundException` so Lucene's not-found semantics are preserved | one **Get Blob Properties** per file open |
| File count | `Indexing/DefaultLuceneIndexService.cs` | Force compound file format on both writers: `UseCompoundFile = true` and `MergePolicy.NoCFSRatio = 1.0` — each segment becomes a single `.cfs`/`.cfe` pair instead of ~10+ loose files | **List Blobs** (smaller listings) and **Get Blob Properties** (far fewer per-file checks), proportional to file-count reduction |

### Notes / caveats

- **Compound files take effect only on newly written segments.** Existing indexes
  keep their loose-file segments until rewritten — a **rebuild/reindex** is needed
  to realize the full benefit (or wait for natural merges). `NoCFSRatio = 1.0`
  ensures even large merged segments stay compound (the Lucene default `0.1`
  leaves big segments — the bulk of file count — as loose files).
- The mapping of CMS.IO operations to Azure transaction types is **inferred**:
  `Directory.*` enumerations → List Blobs; `File.Exists` / `FileInfo.Length` →
  Get Blob Properties; `Directory.Move` → list + copy + delete. Confirm via
  **Azure portal → Metrics → Transactions split by API Name** (and caller).
- These overrides (`Sync`, `ListAll`, `OpenInput`, `FileLength`) are invoked by
  the Lucene.NET library through the abstract `Directory` base type, so a static
  "find references" in this repo shows no callers even though they run at runtime.

## Remaining levers (not yet applied)

1. **Reduce publish/reopen frequency** — batch indexing so publishes (and the
   invalidate→reopen burst that follows each one) happen less often.
2. **Enable Kentico's Azure local file cache** (`CMSAzureCachePath` + file
   caching) — serves repeated reads/metadata from local disk, cutting Get Blob
   Properties / Read transactions. Configuration only, no code.
3. **Publish without `CmsDirectory.Move`** — track the published generation via a
   small marker (stable `i-g{n}` directory) instead of encoding `p_true/p_false`
   in the directory name, so publishing is one small write instead of copying the
   whole index. Highest-impact List Blobs reduction on the publish path; changes
   the on-blob layout and needs a migration + correctness review (deferred).

## How to verify

Reindex, then compare **Azure Metrics → Transactions by API Name** before/after:

- **Get Blob Properties dominant** → per-file opens (merges/reopens); the
  compound-file change is the biggest lever.
- **List Blobs dominant** → enumerations/publishes; levers #1 and #3 apply.
