using CMS.Core;
using CMS.Helpers.Caching.Abstractions;
using CMS.IO;

using Kentico.Xperience.Lucene.Core.Scaling;
using Kentico.Xperience.Lucene.Core.Search;

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

using Microsoft.Extensions.DependencyInjection;

using CmsDirectoryInfo = CMS.IO.DirectoryInfo;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Default implementation of <see cref="ILuceneClient"/>.
/// </summary>
internal class DefaultLuceneClient : ILuceneClient
{
    private readonly ILuceneIndexService luceneIndexService;


    private readonly ILuceneSearchService luceneSearchService;


    private readonly IServiceProvider serviceProvider;


    private readonly IEventLogService log;


    private readonly ICacheAccessor cacheAccessor;


    private readonly ILuceneIndexManager indexManager;


    private readonly IWebFarmService webFarmService;


    private readonly LuceneSearchCacheInvalidator searchCacheInvalidator;


    private readonly LuceneIndexedItemEnumerator itemEnumerator;


    private readonly LuceneRebuildTracker rebuildTracker;


    internal const string CACHEKEY_STATISTICS = "Lucene|ListIndices";


    public DefaultLuceneClient(
        ICacheAccessor cacheAccessor,
        ILuceneIndexService luceneIndexService,
        ILuceneSearchService luceneSearchService,
        IServiceProvider serviceProvider,
        IEventLogService log,
        ILuceneIndexManager indexManager,
        IWebFarmService webFarmService,
        LuceneSearchCacheInvalidator searchCacheInvalidator,
        LuceneIndexedItemEnumerator itemEnumerator,
        LuceneRebuildTracker rebuildTracker
        )
    {
        this.cacheAccessor = cacheAccessor;
        this.luceneIndexService = luceneIndexService;
        this.luceneSearchService = luceneSearchService;
        this.serviceProvider = serviceProvider;
        this.log = log;
        this.indexManager = indexManager;
        this.webFarmService = webFarmService;
        this.searchCacheInvalidator = searchCacheInvalidator;
        this.itemEnumerator = itemEnumerator;
        this.rebuildTracker = rebuildTracker;
    }

    /// <inheritdoc />
    public Task<int> DeleteRecords(IEnumerable<string> itemGuids, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (itemGuids == null || !itemGuids.Any())
        {
            return Task.FromResult(0);
        }

        return DeleteRecordsInternal(itemGuids, indexName);
    }


    /// <inheritdoc/>
    public Task<ICollection<LuceneIndexStatisticsModel>> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = indexManager.GetAllIndices().Select(i =>
        {
            var statistics = luceneSearchService.UseSearcher(i, s => new LuceneIndexStatisticsModel()
            {
                Name = i.IndexName,
                Entries = s.IndexReader.NumDocs
            });

            var dir = CmsDirectoryInfo.New(i.StorageContext.IndexStoragePathRoot);
            statistics.UpdatedAt = GetLastWriteTime(dir);
            return statistics;
        }).ToList();

        return Task.FromResult<ICollection<LuceneIndexStatisticsModel>>(stats);
    }

    /// <inheritdoc />
    public Task Rebuild(string indexName, CancellationToken? cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        var luceneIndex = indexManager.GetRequiredIndex(indexName);
        return RebuildInternal(luceneIndex, cancellationToken);
    }


    /// <inheritdoc />
    public Task<int> UpsertRecords(IEnumerable<Document> documents, string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (documents == null || !documents.Any())
        {
            return Task.FromResult(0);
        }

        return UpsertRecordsInternal(documents, indexName);
    }

    public async Task<bool> DeleteIndex(LuceneIndex luceneIndex)
    {
        if (!StorageHelper.IsExternalStorage(luceneIndex.StorageContext.IndexStoragePathRoot))
        {
            webFarmService.CreateTask(new DeleteIndexWebFarmTask
            {
                IndexName = luceneIndex.IndexName,
                CreatorName = webFarmService.ServerName
            });
        }

        // Release the cached readers first and wait for them to actually close - their open handles would
        // make the deletion below burn through its retries and fail.
        if (!searchCacheInvalidator.InvalidateAndWaitForRelease(luceneIndex))
        {
            log.LogWarning(
                "Kentico.Xperience.Lucene",
                $"{nameof(DefaultLuceneClient)}.{nameof(DeleteIndex)}",
                $"A search over index [{luceneIndex.IndexName}] was still in flight when the deletion started; its folders may still be locked."
            );
        }

        return await luceneIndex.StorageContext.DeleteIndex();
    }

    private Task<int> DeleteRecordsInternal(IEnumerable<string> itemGuids, string indexName)
    {
        var index = indexManager.GetIndex(indexName);
        if (index != null)
        {
            luceneIndexService.UseWriter(index, (writer) =>
            {
                var booleanQuery = new BooleanQuery();
                foreach (string guid in itemGuids)
                {
                    var termQuery = new TermQuery(new Term(nameof(IIndexEventItemModel.ItemGuid), guid));
                    booleanQuery.Add(termQuery, Occur.SHOULD); // Match any of the object IDs
                }
                writer.DeleteDocuments(booleanQuery);
                return "OK";
            }, index.StorageContext.GetLastGeneration(true));
        }
        return Task.FromResult(0);
    }

    private async Task RebuildInternal(LuceneIndex luceneIndex, CancellationToken? cancellationToken)
    {
        // Clear statistics cache so listing displays updated data after rebuild
        cacheAccessor.Remove(CACHEKEY_STATISTICS);

        if (!StorageHelper.IsExternalStorage(luceneIndex.StorageContext.IndexStoragePathRoot))
        {
            webFarmService.CreateTask(new ResetIndexWebFarmTask
            {
                IndexName = luceneIndex.IndexName,
                CreatorName = webFarmService.ServerName
            });
        }

        // Drop cached searchers *before* the reset and wait for them to close: the reset enforces the
        // retention policy, which moves the previous generation to .trash, and that rename fails while a
        // cached reader holds handles inside the folder.
        if (!searchCacheInvalidator.InvalidateAndWaitForRelease(luceneIndex))
        {
            log.LogWarning(
                "Kentico.Xperience.Lucene",
                $"{nameof(DefaultLuceneClient)}.{nameof(RebuildInternal)}",
                $"A search over index [{luceneIndex.IndexName}] was still in flight when the rebuild started; its folders may still be locked."
            );
        }

        luceneIndexService.ResetIndex(luceneIndex);

        var indexedItems = await itemEnumerator.GetItemsAsync(luceneIndex, cancellationToken ?? default);

        log.LogInformation(
            "Kentico.Xperience.Lucene",
            "INDEX_REBUILD",
            $"Rebuilding index [{luceneIndex.IndexName}]. {indexedItems.Count} web page items queued for re-indexing"
        );

        indexedItems.ForEach(item => LuceneQueueWorker.EnqueueLuceneQueueItem(new LuceneQueueItem(item, LuceneTaskType.PUBLISH_INDEX, luceneIndex.IndexName)));
        rebuildTracker.RecordRequest(luceneIndex.IndexName, indexedItems.Count);
    }

    private Task<int> UpsertRecordsInternal(IEnumerable<Document> documents, string indexName)
    {
        var index = indexManager.GetIndex(indexName);
        if (index != null)
        {
            var strategy = serviceProvider.GetRequiredStrategy(index);
            // indexing facet requires separate index for toxonomy
            if (strategy.FacetsConfigFactory() is { } facetsConfig)
            {
                int result = luceneIndexService.UseIndexAndTaxonomyWriter(index, (writer, taxonomyWriter) =>
                {
                    int count = 0;
                    foreach (var document in documents)
                    {
                        // for now all changes are creates, update to be done later
                        // delete old document, there is no upsert nor update in Lucene

                        string? id = document.Get(nameof(IIndexEventItemModel.ItemGuid));
                        string? language = document.Get(nameof(IIndexEventItemModel.LanguageName));
                        if (id is not null && language is not null)
                        {
                            // for now all changes are creates, update to be done later
                            // delete old document, there is no upsert nor update in Lucene
                            var multiTermQuery = new BooleanQuery
                            {
                                { new TermQuery(new Term(nameof(IIndexEventItemModel.ItemGuid), id)), Occur.MUST },
                                { new TermQuery(new Term(nameof(IIndexEventItemModel.LanguageName), language)), Occur.MUST }
                            };

                            writer.DeleteDocuments(multiTermQuery);
                        }

                        // add new one
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                        if (document is not null)
                        {
                            writer.AddDocument(facetsConfig.Build(taxonomyWriter, document));
                            count++;
                        }
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
#pragma warning disable S2583 // Conditionally executed code should be reachable
                        if (count % 1000 == 0)
                        {
                            taxonomyWriter.Commit();
                        }
#pragma warning restore S2583 // Conditionally executed code should be reachable
                    }
                    taxonomyWriter.Commit();

                    return count;
                }, index.StorageContext.GetLastGeneration(true));

                return Task.FromResult(result);
            }
            else // no facets, only index writer opened
            {
                int result = luceneIndexService.UseWriter(index, (writer) =>
                {
                    int count = 0;
                    foreach (var document in documents)
                    {
                        string? id = document.Get(nameof(IIndexEventItemModel.ItemGuid));
                        string? language = document.Get(nameof(IIndexEventItemModel.LanguageName));
                        if (id is not null && language is not null)
                        {
                            // for now all changes are creates, update to be done later
                            // delete old document, there is no upsert nor update in Lucene
                            var multiTermQuery = new BooleanQuery
                            {
                                { new TermQuery(new Term(nameof(IIndexEventItemModel.ItemGuid), id)), Occur.MUST },
                                { new TermQuery(new Term(nameof(IIndexEventItemModel.LanguageName), language)), Occur.MUST }
                            };

                            writer.DeleteDocuments(multiTermQuery);
                        }
                        // add new one
#pragma warning disable S2589 // Boolean expressions should not be gratuitous
                        if (document is not null)
                        {
                            writer.AddDocument(document);
                            count++;
                        }
#pragma warning restore S2589 // Boolean expressions should not be gratuitous
                    }
                    return count;
                }, index.StorageContext.GetLastGeneration(true));

                return Task.FromResult(result);
            }
        }
        return Task.FromResult(0);
    }

    /// <summary>
    /// Gets the last write time of the directory, if the directory itself doesn't have a valid last write time,
    /// it will return the latest last write time of the files within the directory.
    /// If there are no files or an error occurs, it returns DateTime.MinValue.
    /// </summary>
    private static DateTime GetLastWriteTime(CmsDirectoryInfo dir)
    {
        try
        {
            var lastWriteTime = dir.LastWriteTime;
            if (lastWriteTime != DateTime.MinValue)
            {
                return lastWriteTime;
            }

            var files = dir.GetFiles("*", CMS.IO.SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                return DateTime.MinValue;
            }

            return files.Max(f => f.LastWriteTime);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
