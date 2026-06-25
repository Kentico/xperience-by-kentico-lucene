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
