using System.Collections.Concurrent;

using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// Caches and reuses Lucene <see cref="IndexSearcher"/> instances per index so that searches do not
/// open a fresh <see cref="DirectoryReader"/> (and re-enumerate the storage backend) on every query.
/// </summary>
/// <remarks>
/// On Azure Blob Storage every directory enumeration performed while opening a reader is billed as a
/// (relatively expensive) List Blobs transaction. A published index generation is immutable - its files
/// never change until a new generation is published - so a reader opened over it stays valid for the
/// whole lifetime of that generation. This provider keeps a <see cref="SearcherManager"/> per index and
/// only rebuilds it when <see cref="Invalidate"/> is called (after a publish, reset, or delete), turning
/// per-search List Blobs traffic into per-generation traffic.
/// </remarks>
internal sealed class LuceneIndexSearcherProvider : IDisposable
{
    private readonly ILuceneIndexService indexService;
    private readonly ConcurrentDictionary<string, CachedIndex> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object creationLock = new();
    private bool disposed;


    public LuceneIndexSearcherProvider(ILuceneIndexService indexService) => this.indexService = indexService;


    /// <summary>
    /// Acquires a searcher (and optionally a taxonomy reader) for the given index. The returned lease must
    /// be disposed to release the underlying references back to the cache.
    /// </summary>
    /// <param name="index">The index to search.</param>
    /// <param name="withTaxonomy">When <see langword="true"/>, the lease also exposes a taxonomy reader for faceted search.</param>
    public SearcherLease Acquire(LuceneIndex index, bool withTaxonomy = false)
        => AcquireInternal(index.IndexName, () => CachedIndex.Open(index, indexService), withTaxonomy);


    /// <summary>
    /// Marks the cached searcher for the given index as stale. The underlying resources are disposed once
    /// all in-flight leases have been released. The next acquisition rebuilds the searcher over the current
    /// published generation.
    /// </summary>
    public void Invalidate(string indexName)
    {
        lock (creationLock)
        {
            if (cache.TryRemove(indexName, out var cached))
            {
                cached.Retire();
            }
        }
    }


    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        foreach (var key in cache.Keys.ToArray())
        {
            if (cache.TryRemove(key, out var cached))
            {
                cached.Retire();
            }
        }
    }


    /// <summary>
    /// Core acquisition logic, decoupled from how the cached index is opened so it can be exercised in tests
    /// with an in-memory directory. <paramref name="open"/> is only invoked on a cache miss.
    /// </summary>
    internal SearcherLease AcquireInternal(string indexName, Func<CachedIndex> open, bool withTaxonomy)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var cached = GetOrCreate(indexName, open);
            var lease = cached.TryAcquire(withTaxonomy);
            if (lease is not null)
            {
                return lease;
            }

            // The entry was retired (invalidated) between lookup and acquisition - drop it and retry.
            cache.TryRemove(new KeyValuePair<string, CachedIndex>(indexName, cached));
        }

        throw new InvalidOperationException(
            $"Could not acquire a searcher for index '{indexName}' after {maxAttempts} attempts due to repeated concurrent invalidation.");
    }


    private CachedIndex GetOrCreate(string indexName, Func<CachedIndex> open)
    {
        if (cache.TryGetValue(indexName, out var existing))
        {
            return existing;
        }

        lock (creationLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (cache.TryGetValue(indexName, out existing))
            {
                return existing;
            }

            var created = open();
            cache[indexName] = created;
            return created;
        }
    }
}
