using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Helpers.Caching.Abstractions;
using CMS.Websites;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Search;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Indexing;

/// <summary>
/// Default implementation of <see cref="ILuceneClient"/>.
/// </summary>
internal class DefaultLuceneClient : ILuceneClient
{
    private readonly ILuceneIndexService luceneIndexService;
    private readonly ILuceneSearchService luceneSearchService;
    private readonly IContentQueryExecutor executor;
    private readonly IServiceProvider serviceProvider;
    private readonly IInfoProvider<ContentLanguageInfo> languageProvider;
    private readonly IInfoProvider<ChannelInfo> channelProvider;
    private readonly IConversionService conversionService;
    private readonly IProgressiveCache cache;
    private readonly IEventLogService log;
    private readonly ICacheAccessor cacheAccessor;
    private readonly ILuceneIndexManager indexManager;

    internal const string CACHEKEY_STATISTICS = "Lucene|ListIndices";

    public DefaultLuceneClient(
        ICacheAccessor cacheAccessor,
        ILuceneIndexService luceneIndexService,
        ILuceneSearchService luceneSearchService,
        IContentQueryExecutor executor,
        IServiceProvider serviceProvider,
        IInfoProvider<ContentLanguageInfo> languageProvider,
        IInfoProvider<ChannelInfo> channelProvider,
        IConversionService conversionService,
        IProgressiveCache cache,
        IEventLogService log,
        ILuceneIndexManager indexManager
        )
    {
        this.cacheAccessor = cacheAccessor;
        this.luceneIndexService = luceneIndexService;
        this.luceneSearchService = luceneSearchService;
        this.executor = executor;
        this.serviceProvider = serviceProvider;
        this.languageProvider = languageProvider;
        this.channelProvider = channelProvider;
        this.conversionService = conversionService;
        this.cache = cache;
        this.log = log;
        this.indexManager = indexManager;
        this.indexManager = indexManager;
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
    public Task<ICollection<LuceneIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken)
    {
        var stats = indexManager.GetAllIndices().Select(i =>
        {
            var statistics = luceneSearchService.UseSearcher(i, s => new LuceneIndexStatisticsViewModel()
            {
                Name = i.IndexName,
                Entries = s.IndexReader.NumDocs
            });

            var dir = new DirectoryInfo(i.StorageContext.GetPublishedIndex().Path);
            statistics.UpdatedAt = dir.LastWriteTime;
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

        luceneIndexService.ResetIndex(luceneIndex);

        var indexedItems = new List<IndexEventWebPageItemModel>();
        foreach (var includedPathAttribute in luceneIndex.IncludedPaths)
        {
            var pathMatch =
                includedPathAttribute.AliasPath.EndsWith("/%", StringComparison.OrdinalIgnoreCase)
                    ? PathMatch.Children(includedPathAttribute.AliasPath[..^2])
                    : PathMatch.Single(includedPathAttribute.AliasPath);

            foreach (string language in luceneIndex.LanguageNames)
            {
                var queryBuilder = new ContentItemQueryBuilder();

                if (includedPathAttribute.ContentTypes != null && includedPathAttribute.ContentTypes.Count > 0)
                {
                    foreach (string contentType in includedPathAttribute.ContentTypes)
                    {
                        queryBuilder.ForContentType(contentType, config => config.ForWebsite(luceneIndex.WebSiteChannelName, includeUrlPath: true, pathMatch: pathMatch));
                    }
                }
                queryBuilder.InLanguage(language);

                var webpages = await executor.GetWebPageResult(queryBuilder, container => container, cancellationToken: cancellationToken ?? default);

                foreach (var page in webpages)
                {
                    var item = await MapToEventItem(page);
                    indexedItems.Add(item);
                }
            }
        }

        log.LogInformation(
            "Kentico.Xperience.Lucene",
            "INDEX_REBUILD",
            $"Rebuilding index [{luceneIndex.IndexName}]. {indexedItems.Count} web page items queued for re-indexing"
        );

        indexedItems.ForEach(item => LuceneQueueWorker.EnqueueLuceneQueueItem(new LuceneQueueItem(item, LuceneTaskType.PUBLISH_INDEX, luceneIndex.IndexName)));
    }

    private async Task<IndexEventWebPageItemModel> MapToEventItem(IWebPageContentQueryDataContainer content)
    {
        var languages = await GetAllLanguages();

        string languageName = languages.FirstOrDefault(l => l.ContentLanguageID == content.ContentItemCommonDataContentLanguageID)?.ContentLanguageName ?? "";

        var websiteChannels = await GetAllWebsiteChannels();

        string channelName = websiteChannels.FirstOrDefault(c => c.WebsiteChannelID == content.WebPageItemWebsiteChannelID).ChannelName ?? "";

        var item = new IndexEventWebPageItemModel(
            content.WebPageItemID,
            content.WebPageItemGUID,
            languageName,
            content.ContentTypeName,
            content.WebPageItemName,
            content.ContentItemIsSecured,
            content.ContentItemContentTypeID,
            content.ContentItemCommonDataContentLanguageID,
            channelName,
            content.WebPageItemTreePath,
            content.WebPageItemOrder);

        return item;
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
                        if (id is not null)
                        {
                            writer.DeleteDocuments(new Term(nameof(IIndexEventItemModel.ItemGuid), id));
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
                        if (id is not null)
                        {
                            // for now all changes are creates, update to be done later
                            // delete old document, there is no upsert nor update in Lucene
                            writer.DeleteDocuments(new Term(nameof(IIndexEventItemModel.ItemGuid), id));
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

    private Task<IEnumerable<ContentLanguageInfo>> GetAllLanguages() =>
        cache.LoadAsync(async cs =>
        {
            var results = await languageProvider.Get().GetEnumerableTypedResultAsync();

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency($"{ContentLanguageInfo.OBJECT_TYPE}|all");

            return results;
        }, new CacheSettings(5, nameof(DefaultLuceneClient), nameof(GetAllLanguages)));

    private Task<IEnumerable<(int WebsiteChannelID, string ChannelName)>> GetAllWebsiteChannels() =>
        cache.LoadAsync(async cs =>
        {

            var results = await channelProvider.Get()
                .Source(s => s.Join<WebsiteChannelInfo>(nameof(ChannelInfo.ChannelID), nameof(WebsiteChannelInfo.WebsiteChannelChannelID)))
                .Columns(nameof(WebsiteChannelInfo.WebsiteChannelID), nameof(ChannelInfo.ChannelName))
                .GetDataContainerResultAsync();

            cs.GetCacheDependency = () => CacheHelper.GetCacheDependency(new[] { $"{ChannelInfo.OBJECT_TYPE}|all", $"{WebsiteChannelInfo.OBJECT_TYPE}|all" });

            var items = new List<(int WebsiteChannelID, string ChannelName)>();

            foreach (var item in results)
            {
                if (item.TryGetValue(nameof(WebsiteChannelInfo.WebsiteChannelID), out object channelID) && item.TryGetValue(nameof(ChannelInfo.ChannelName), out object channelName))
                {
                    items.Add(new(conversionService.GetInteger(channelID, 0), conversionService.GetString(channelName, "")));
                }
            }

            return items.AsEnumerable();
        }, new CacheSettings(5, nameof(DefaultLuceneClient), nameof(GetAllWebsiteChannels)));
}
