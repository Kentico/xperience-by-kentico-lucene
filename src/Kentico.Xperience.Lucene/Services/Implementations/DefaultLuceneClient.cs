using CMS.DocumentEngine;
using CMS.Helpers.Caching.Abstractions;
using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Default implementation of <see cref="ILuceneClient"/>.
/// </summary>
internal class DefaultLuceneClient : ILuceneClient
{
    private readonly ILuceneIndexService luceneIndexService;
    private readonly ILuceneSearchModelToDocumentMapper luceneSearchModelToDocumentMapper;

    private readonly ICacheAccessor cacheAccessor;
    private readonly IPageRetriever pageRetriever;

    internal const string CACHEKEY_STATISTICS = "Lucene|ListIndices";

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneClient"/> class.
    /// </summary>
    public DefaultLuceneClient(
        ICacheAccessor cacheAccessor,
        IPageRetriever pageRetriever,
        ILuceneIndexService luceneIndexService,
        ILuceneSearchModelToDocumentMapper luceneSearchModelToDocumentMapper)
    {
        this.cacheAccessor = cacheAccessor;
        this.pageRetriever = pageRetriever;
        this.luceneIndexService = luceneIndexService;
        this.luceneSearchModelToDocumentMapper = luceneSearchModelToDocumentMapper;
    }

    /// <inheritdoc />
    public Task<int> DeleteRecords(IEnumerable<string> objectIds, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (objectIds == null || !objectIds.Any())
        {
            return Task.FromResult(0);
        }

        int result = DeleteRecordsInternal(objectIds, indexName);

        return Task.FromResult(result);
    }


    /// <inheritdoc/>
    public Task<ICollection<LuceneIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken)
    {
        var statistics = IndexStore.Instance.GetAllIndexes()
            .Select(i =>
            {
                var statistics = luceneIndexService.UseSearcher(i, s => new LuceneIndexStatisticsViewModel()
                {
                    Name = i.IndexName,
                    Entries = s.IndexReader.NumDocs,
                });
                return statistics;
            })
            .ToList();

        return Task.FromResult<ICollection<LuceneIndexStatisticsViewModel>>(statistics);
    }



    /// <inheritdoc />
    public Task Rebuild(string indexName, CancellationToken? cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        var luceneIndex = IndexStore.Instance.GetIndex(indexName);
        return luceneIndex == null
            ? throw new InvalidOperationException($"The index '{indexName}' is not registered.")
            : RebuildInternal(luceneIndex, cancellationToken);
    }


    /// <inheritdoc />
    public Task<int> UpsertRecords(IEnumerable<LuceneSearchModel> dataObjects, string indexName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        if (dataObjects == null || !dataObjects.Any())
        {
            return Task.FromResult(0);
        }

        int result = UpsertRecordsInternal(dataObjects, indexName);

        return Task.FromResult(result);
    }

    private int DeleteRecordsInternal(IEnumerable<string> objectIds, string indexName)
    {
        var index = IndexStore.Instance.GetIndex(indexName);
        if (index != null)
        {
            luceneIndexService.UseWriter(index, (writer) =>
            {
                var booleanQuery = new BooleanQuery();
                foreach (string objectId in objectIds)
                {
                    var termQuery = new TermQuery(new Term(nameof(LuceneSearchModel.ObjectID), objectId));
                    booleanQuery.Add(termQuery, Occur.SHOULD); // Match any of the object IDs
                }
                // todo use batches
                writer.DeleteDocuments(booleanQuery);
                return "OK";
            }, index.StorageContext.GetLastGeneration(true));
        }

        return 0;
    }

    private async Task RebuildInternal(LuceneIndex luceneIndex, CancellationToken? cancellationToken)
    {
        // Clear statistics cache so listing displays updated data after rebuild
        cacheAccessor.Remove(CACHEKEY_STATISTICS);

        luceneIndexService.ResetIndex(luceneIndex);

        var indexedNodes = new List<TreeNode>();
        foreach (var includedPathAttribute in luceneIndex.IncludedPaths)
        {
            var nodes = (await pageRetriever.RetrieveMultipleAsync(q =>
            {
                if (includedPathAttribute.ContentTypes != null && includedPathAttribute.ContentTypes.Length > 0)
                {
                    q.Types(includedPathAttribute.ContentTypes);
                }

                q.Path(includedPathAttribute.AliasPath)
                    .PublishedVersion()
                    .WithCoupledColumns();

                q.AllCultures();
            }, cancellationToken: cancellationToken))
            .Where(node => luceneIndex.LuceneIndexingStrategy.ShouldIndexNode(node));

            indexedNodes.AddRange(nodes);
        }

        indexedNodes.ForEach(node => LuceneQueueWorker.EnqueueLuceneQueueItem(new LuceneQueueItem(node, LuceneTaskType.CREATE, luceneIndex.IndexName)));
        LuceneQueueWorker.EnqueueIndexPublication(luceneIndex.IndexName);
    }

    private int UpsertRecordsInternal(IEnumerable<LuceneSearchModel> dataObjects, string indexName)
    {
        var index = IndexStore.Instance.GetIndex(indexName);
        if (index != null)
        {
            // indexing facet requires separate index for toxonomy
            if (index.LuceneIndexingStrategy.FacetsConfigFactory() is { } facetsConfig)
            {
                return luceneIndexService.UseIndexAndTaxonomyWriter(index, (writer, taxonomyWriter) =>
                {
                    int count = 0;
                    foreach (var dataObject in dataObjects)
                    {
                        // for now all changes are creates, update to be done later
                        // delete old document, there is no upsert nor update in Lucene
                        writer.DeleteDocuments(new Term(nameof(LuceneSearchModel.ObjectID), dataObject.ObjectID));

                        var document = luceneSearchModelToDocumentMapper.MapModelToDocument(index, dataObject);
                        // add new one
                        writer.AddDocument(facetsConfig.Build(taxonomyWriter, document));
                        count++;

                        if (count % 1000 == 0)
                        {
                            taxonomyWriter.Commit();
                        }
                    }
                    taxonomyWriter.Commit();

                    return count;
                }, index.StorageContext.GetLastGeneration(true));
            }
            else // no facets, only index writer opened
            {
                return luceneIndexService.UseWriter(index, (writer) =>
                {
                    int count = 0;
                    foreach (var dataObject in dataObjects)
                    {
                        // for now all changes are creates, update to be done later
                        // delete old document, there is no upsert nor update in Lucene
                        writer.DeleteDocuments(new Term(nameof(LuceneSearchModel.ObjectID), dataObject.ObjectID));

                        var document = luceneSearchModelToDocumentMapper.MapModelToDocument(index, dataObject);
                        // add new one
                        writer.AddDocument(document);
                        count++;
                    }
                    return count;
                }, index.StorageContext.GetLastGeneration(true));
            }
        }

        return 0;
    }
}
