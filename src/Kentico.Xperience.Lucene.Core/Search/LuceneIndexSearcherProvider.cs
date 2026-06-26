using System.Collections.Concurrent;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Store;

using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;

using CmsDirectory = CMS.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;

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


    public LuceneIndexSearcherProvider(ILuceneIndexService indexService)
    {
        this.indexService = indexService;
    }


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
        if (cache.TryRemove(indexName, out var cached))
        {
            cached.Retire();
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


/// <summary>
/// Holds the open reader resources for a single published index generation and tracks the number of active
/// leases so the resources are disposed only once they are no longer in use.
/// </summary>
internal sealed class CachedIndex
{
    /// <summary>The taxonomy directory and reader opened together for faceted search.</summary>
    internal readonly record struct TaxonomyResources(LuceneDirectory Directory, DirectoryTaxonomyReader Reader);

    private readonly object syncLock = new();
    private readonly LuceneDirectory indexDir;
    private readonly SearcherManager searcherManager;
    private readonly Func<TaxonomyResources>? openTaxonomy;

    private volatile DirectoryTaxonomyReader? taxonomyReader;
    private LuceneDirectory? taxonomyDir;
    private int leaseCount;
    private bool retired;


    /// <summary>
    /// Creates a cached index over already-opened resources. <paramref name="openTaxonomy"/> is invoked
    /// lazily on the first faceted acquisition; pass <see langword="null"/> when taxonomy is not supported.
    /// </summary>
    internal CachedIndex(LuceneDirectory indexDir, SearcherManager searcherManager, Func<TaxonomyResources>? openTaxonomy)
    {
        this.indexDir = indexDir;
        this.searcherManager = searcherManager;
        this.openTaxonomy = openTaxonomy;
    }


    public static CachedIndex Open(LuceneIndex index, ILuceneIndexService indexService)
    {
        var published = index.StorageContext.GetPublishedIndex();

        // Cold start: ensure the index directory exists so the reader can be opened. This enumeration
        // happens once per generation (on cache miss), not on every search.
        if (!CmsDirectory.Exists(published.Path))
        {
            indexService.UseWriter(index, writer =>
            {
                writer.Commit();
                return true;
            }, published);
        }

        var dir = CmsIODirectory.Open(published.Path);
        try
        {
            var manager = new SearcherManager(dir, null);
            return new CachedIndex(dir, manager, () => OpenTaxonomy(index, indexService, published));
        }
        catch
        {
            dir.Dispose();
            throw;
        }
    }


    private static TaxonomyResources OpenTaxonomy(LuceneIndex index, ILuceneIndexService indexService, IndexStorageModel storage)
    {
        // Cold start: ensure the taxonomy directory exists before opening a reader over it.
        if (!CmsDirectory.Exists(storage.TaxonomyPath))
        {
            indexService.UseIndexAndTaxonomyWriter(index, (writer, taxonomyWriter) =>
            {
                writer.Commit();
                taxonomyWriter.Commit();
                return true;
            }, storage);
        }

        var dir = CmsIODirectory.Open(storage.TaxonomyPath);
        try
        {
            return new TaxonomyResources(dir, new DirectoryTaxonomyReader(dir));
        }
        catch
        {
            dir.Dispose();
            throw;
        }
    }


    /// <summary>
    /// Attempts to acquire a lease. Returns <see langword="null"/> if this entry has already been retired,
    /// signalling the caller to recreate the cache entry.
    /// </summary>
    internal SearcherLease? TryAcquire(bool withTaxonomy)
    {
        lock (syncLock)
        {
            if (retired)
            {
                return null;
            }

            leaseCount++;
        }

        IndexSearcher searcher;
        try
        {
            searcher = searcherManager.Acquire();
        }
        catch
        {
            ReleaseLease();
            throw;
        }

        DirectoryTaxonomyReader? taxonomy = null;
        if (withTaxonomy)
        {
            try
            {
                taxonomy = EnsureTaxonomyReader();
            }
            catch
            {
                searcherManager.Release(searcher);
                ReleaseLease();
                throw;
            }
        }

        return new SearcherLease(searcher, taxonomy, () =>
        {
            try
            {
                searcherManager.Release(searcher);
            }
            finally
            {
                ReleaseLease();
            }
        });
    }


    private DirectoryTaxonomyReader EnsureTaxonomyReader()
    {
        var existing = taxonomyReader;
        if (existing is not null)
        {
            return existing;
        }

        lock (syncLock)
        {
            if (taxonomyReader is null)
            {
                if (openTaxonomy is null)
                {
                    throw new InvalidOperationException("This cached index was created without taxonomy support.");
                }

                var resources = openTaxonomy();
                taxonomyDir = resources.Directory;
                taxonomyReader = resources.Reader;
            }

            return taxonomyReader;
        }
    }


    internal void Retire()
    {
        bool dispose;
        lock (syncLock)
        {
            if (retired)
            {
                return;
            }

            retired = true;
            dispose = leaseCount == 0;
        }

        if (dispose)
        {
            DisposeResources();
        }
    }


    private void ReleaseLease()
    {
        bool dispose;
        lock (syncLock)
        {
            leaseCount--;
            dispose = retired && leaseCount == 0;
        }

        if (dispose)
        {
            DisposeResources();
        }
    }


    private void DisposeResources()
    {
        // Disposal only runs once all leases are released, so no searcher/reader is in flight here.
        try
        {
            taxonomyReader?.Dispose();
        }
        catch
        {
            // best effort - a failed reader disposal must not prevent the rest from being released
        }

        try
        {
            taxonomyDir?.Dispose();
        }
        catch
        {
            // best effort
        }

        try
        {
            searcherManager.Dispose();
        }
        catch
        {
            // best effort
        }

        try
        {
            indexDir.Dispose();
        }
        catch
        {
            // best effort
        }
    }
}


/// <summary>
/// A reference to a cached <see cref="IndexSearcher"/> (and optional taxonomy reader). Dispose to return
/// the references to the owning <see cref="LuceneIndexSearcherProvider"/>.
/// </summary>
internal sealed class SearcherLease : IDisposable
{
    private readonly Action releaseCallback;
    private bool disposed;


    internal SearcherLease(IndexSearcher searcher, DirectoryTaxonomyReader? taxonomyReader, Action releaseCallback)
    {
        Searcher = searcher;
        TaxonomyReader = taxonomyReader;
        this.releaseCallback = releaseCallback;
    }


    /// <summary>
    /// The searcher over the current published index generation. Valid until the lease is disposed.
    /// </summary>
    public IndexSearcher Searcher { get; }


    /// <summary>
    /// The taxonomy reader for faceted search, or <see langword="null"/> when the lease was acquired
    /// without taxonomy support.
    /// </summary>
    public DirectoryTaxonomyReader? TaxonomyReader { get; }


    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        releaseCallback();
    }
}
