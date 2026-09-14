using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Store;

using Lucene.Net.Analysis;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;

using CmsDirectory = CMS.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;
using RamDirectory = Lucene.Net.Store.RAMDirectory;

namespace Kentico.Xperience.Lucene.Core.Search;

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
    private bool releaseCompleted;


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
        var published = index.StorageContext.TryGetPublishedIndex();

        if (published is null)
        {
            // Nothing is published yet - either the index has never been built, or a rebuild is in progress
            // and the reset already removed the previous generation. Serve an empty in-memory index rather
            // than opening the unpublished generation the rebuild is writing into: a reader there holds
            // handles inside the folder and blocks the rename that publishes it. The entry is invalidated
            // once the generation is published, so the next search opens the real index.
            return OpenEmpty(index.LuceneAnalyzer);
        }

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


    /// <summary>
    /// Opens a cached index over an empty in-memory index, used while no generation is published. It touches
    /// no storage, so it can never lock a directory a rebuild is about to rename, and searches over it simply
    /// return no results.
    /// </summary>
    internal static CachedIndex OpenEmpty(Analyzer analyzer)
    {
        var dir = new RamDirectory();
        try
        {
            // A reader can only be opened over a directory holding a commit point.
            using (var writer = new IndexWriter(dir, new IndexWriterConfig(AnalyzerStorage.AnalyzerLuceneVersion, analyzer)))
            {
                writer.Commit();
            }

            var manager = new SearcherManager(dir, null);
            return new CachedIndex(dir, manager, OpenEmptyTaxonomy);
        }
        catch
        {
            dir.Dispose();
            throw;
        }
    }


    private static TaxonomyResources OpenEmptyTaxonomy()
    {
        var dir = new RamDirectory();
        try
        {
            using (var taxonomyWriter = new DirectoryTaxonomyWriter(dir))
            {
                taxonomyWriter.Commit();
            }

            return new TaxonomyResources(dir, new DirectoryTaxonomyReader(dir));
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


    /// <summary>
    /// Blocks until the cached resources have been disposed - that is, until every in-flight lease has been
    /// released and no reader holds handles inside the generation folder any more. Returns
    /// <see langword="false"/> when <paramref name="timeout"/> elapses first.
    /// </summary>
    /// <remarks>
    /// Only meaningful after <see cref="Retire"/>: a live entry is never disposed, so the wait would just run
    /// out the timeout. Callers use this to rename a generation folder only once the readers over it are
    /// actually gone, rather than racing the rename against them and retrying on failure.
    /// </remarks>
    internal bool WaitForRelease(TimeSpan timeout)
    {
        lock (syncLock)
        {
            long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;

            while (!releaseCompleted)
            {
                long remaining = deadline - Environment.TickCount64;
                if (remaining <= 0 || !Monitor.Wait(syncLock, (int)Math.Min(remaining, int.MaxValue)))
                {
                    return releaseCompleted;
                }
            }

            return true;
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

        // Signal last, so a waiter that observes this has also observed the handles being closed.
        lock (syncLock)
        {
            releaseCompleted = true;
            Monitor.PulseAll(syncLock);
        }
    }
}
