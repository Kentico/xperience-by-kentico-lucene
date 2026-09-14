using CMS.Core;
using CMS.IO;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Scaling;

namespace Kentico.Xperience.Lucene.Core.Search;

/// <summary>
/// Invalidates cached search readers for an index after its contents change (publish, reset, delete, or
/// in-place upserts/deletes). Drops the local cached searcher and, when the index lives on external
/// (shared) storage, broadcasts the invalidation to the other web farm servers - where the change itself
/// is not replicated. For non-external storage the change is replicated to each server via its own web
/// farm task, so each server already invalidates its own cache locally and no broadcast is needed.
/// </summary>
internal sealed class LuceneSearchCacheInvalidator
{
    private readonly LuceneIndexSearcherProvider searcherProvider;
    private readonly IWebFarmService webFarmService;

    public LuceneSearchCacheInvalidator(LuceneIndexSearcherProvider searcherProvider, IWebFarmService webFarmService)
    {
        this.searcherProvider = searcherProvider;
        this.webFarmService = webFarmService;
    }

    /// <summary>
    /// Invalidates the local cached searcher for the index and, when the index lives on external (shared)
    /// storage, broadcasts the invalidation to the other web farm servers.
    /// </summary>
    public void Invalidate(LuceneIndex index)
    {
        searcherProvider.Invalidate(index.IndexName);

        Broadcast(index);
    }


    /// <summary>
    /// Invalidates the cached searcher like <see cref="Invalidate"/> and additionally waits until the local
    /// readers have been disposed. Use this before renaming an index generation's folders - the rename fails
    /// while a reader still holds handles inside them.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> when a local lease was still in flight after the timeout, meaning the folders
    /// may still be locked. The caller may proceed regardless; the storage strategy retries a locked rename.
    /// </returns>
    public bool InvalidateAndWaitForRelease(LuceneIndex index)
    {
        bool released = searcherProvider.InvalidateAndWait(index.IndexName, LuceneIndexSearcherProvider.ReaderReleaseTimeout);

        Broadcast(index);

        return released;
    }


    private void Broadcast(LuceneIndex index)
    {
        if (StorageHelper.IsExternalStorage(index.StorageContext.IndexStoragePathRoot))
        {
            webFarmService.CreateTask(new InvalidateSearchIndexWebFarmTask
            {
                IndexName = index.IndexName,
                CreatorName = webFarmService.ServerName
            });
        }
    }
}
