using CMS.ContentEngine;
using CMS.Helpers.Caching.Abstractions;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Default implementation of <see cref="ILuceneClient"/>.
/// </summary>
internal class DefaultLuceneClient : ILuceneClient
{
    private readonly ILuceneIndexService luceneIndexService;

    private readonly IContentQueryExecutor executor;
    private readonly IServiceProvider serviceProvider;
    private readonly ICacheAccessor cacheAccessor;

    internal const string CACHEKEY_STATISTICS = "Lucene|ListIndices";

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneClient"/> class.
    /// </summary>
    public DefaultLuceneClient(
        ICacheAccessor cacheAccessor,
        ILuceneIndexService luceneIndexService,
        IContentQueryExecutor executor,
        IServiceProvider serviceProvider
        )
    {
        this.cacheAccessor = cacheAccessor;
        this.luceneIndexService = luceneIndexService;
        this.executor = executor;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<int> DeleteRecords(IEnumerable<string> webPageItemGuids, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (webPageItemGuids == null || !webPageItemGuids.Any())
        {
            return Task.FromResult(0);
        }

        return DeleteRecordsInternal(webPageItemGuids, indexName);
    }


    /// <inheritdoc/>
    public Task<ICollection<LuceneIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = IndexStore.Instance.GetAllIndices().Select(i =>
        {
            var statistics = luceneIndexService.UseSearcher(i, s => new LuceneIndexStatisticsViewModel()
            {
                Name = i.IndexName,
                Entries = s.IndexReader.NumDocs,
            });
            return statistics;
        }).ToList();

        return Task.FromResult<ICollection<LuceneIndexStatisticsViewModel>>(stats);
    }

    /// <inheritdoc />
    public Task Rebuild(string indexName, CancellationToken? cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        var luceneIndex = IndexStore.Instance.GetRequiredIndex(indexName);
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

    private Task<int> DeleteRecordsInternal(IEnumerable<string> webPageItemGuids, string indexName)
    {
        var index = IndexStore.Instance.GetIndex(indexName);
        if (index != null)
        {
            luceneIndexService.UseWriter(index, (writer) =>
            {
                var booleanQuery = new BooleanQuery();
                foreach (string guid in webPageItemGuids)
                {
                    var termQuery = new TermQuery(new Term(nameof(IndexedItemModel.WebPageItemGuid), guid));
                    booleanQuery.Add(termQuery, Occur.SHOULD); // Match any of the object IDs
                }
                // TODO use batches
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

        luceneIndexService.ResetIndex(luceneIndex);

        var indexedItems = new List<IndexedItemModel>();
        foreach (var includedPathAttribute in luceneIndex.IncludedPaths)
        {
            foreach (string language in luceneIndex.LanguageCodes)
            {
                var queryBuilder = new ContentItemQueryBuilder();

                if (includedPathAttribute.ContentTypes != null && includedPathAttribute.ContentTypes.Length > 0)
                {
                    foreach (string contentType in includedPathAttribute.ContentTypes)
                    {
                        queryBuilder.ForContentType(contentType, config => config.WithLinkedItems(1).ForWebsite(luceneIndex.WebSiteChannelName, includeUrlPath: true));
                    }
                }
                queryBuilder.InLanguage(language);

                var webPageItems = (await executor.GetWebPageResult(queryBuilder, container => container, cancellationToken: cancellationToken ?? default))
                    .Select(x => new IndexedItemModel()
                    {
                        LanguageCode = language,
                        ClassName = x.ContentTypeName,
                        ChannelName = luceneIndex.WebSiteChannelName,
                        WebPageItemGuid = x.WebPageItemGUID,
                        WebPageItemTreePath = x.WebPageItemTreePath
                    });

                foreach (var item in webPageItems)
                {
                    indexedItems.Add(item);
                }
            }
        }

        indexedItems.ForEach(item => LuceneQueueWorker.EnqueueLuceneQueueItem(new LuceneQueueItem(item, LuceneTaskType.PUBLISH_INDEX, luceneIndex.IndexName)));
    }

    private Task<int> UpsertRecordsInternal(IEnumerable<Document> documents, string indexName)
    {
        var index = IndexStore.Instance.GetIndex(indexName);
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

                        string? id = document.Get(nameof(IndexedItemModel.WebPageItemGuid));
                        if (id is not null)
                        {
                            writer.DeleteDocuments(new Term(nameof(IndexedItemModel.WebPageItemGuid), id));
                        }

                        // add new one
                        if (document is not null)
                        {
                            writer.AddDocument(facetsConfig.Build(taxonomyWriter, document));
                            count++;
                        }
                        if (count % 1000 == 0)
                        {
                            taxonomyWriter.Commit();
                        }
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
                        var bytes = document.GetBinaryValue(nameof(IndexedItemModel.WebPageItemGuid));
                        if (bytes is not null)
                        {
                            // for now all changes are creates, update to be done later
                            // delete old document, there is no upsert nor update in Lucene
                            writer.DeleteDocuments(new Term(nameof(IndexedItemModel.WebPageItemGuid), bytes));
                        }
                        // add new one
                        if (document is not null)
                        {
                            writer.AddDocument(document);
                            count++;
                        }
                    }
                    return count;
                }, index.StorageContext.GetLastGeneration(true));

                return Task.FromResult(result);
            }
        }
        return Task.FromResult(0);
    }
}
