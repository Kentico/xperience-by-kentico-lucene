using CMS.Base;
using CMS.Core;
using CMS.Websites;

using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Core.Indexing;

internal class LuceneBatchResult
{
    internal int SuccessfulOperations { get; set; } = 0;
    internal HashSet<LuceneIndex> PublishedIndices { get; set; } = [];
}

internal class DefaultLuceneTaskProcessor : ILuceneTaskProcessor
{
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly IServiceProvider serviceProvider;
    private readonly ILuceneClient luceneClient;
    private readonly IEventLogService eventLogService;
    private readonly ILuceneIndexManager indexManager;

    public DefaultLuceneTaskProcessor(
        ILuceneClient luceneClient,
        IEventLogService eventLogService,
        IWebPageUrlRetriever urlRetriever,
        IServiceProvider serviceProvider,
        ILuceneIndexManager indexManager)
    {
        this.luceneClient = luceneClient;
        this.eventLogService = eventLogService;
        this.urlRetriever = urlRetriever;
        this.serviceProvider = serviceProvider;
        this.indexManager = indexManager;
    }

    /// <inheritdoc />
    public async Task<int> ProcessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken, int maximumBatchSize = 100)
    {
        LuceneBatchResult batchResults = new();

        var batches = queueItems.Batch(maximumBatchSize);

        foreach (var batch in batches)
        {
            await ProcessLuceneBatch(batch, batchResults, cancellationToken);
        }

        foreach (var index in batchResults.PublishedIndices)
        {
            var storage = index.StorageContext.GetNextOrOpenNextGeneration();
            index.StorageContext.PublishIndex(storage);
        }

        return batchResults.SuccessfulOperations;
    }

    private async Task ProcessLuceneBatch(IEnumerable<LuceneQueueItem> queueItems, LuceneBatchResult previousBatchResults, CancellationToken cancellationToken)
    {

        var groups = queueItems.GroupBy(item => item.IndexName);

        foreach (var group in groups)
        {
            try
            {
                var deleteIds = new List<string>();
                var deleteTasks = group.Where(queueItem => queueItem.TaskType == LuceneTaskType.DELETE).ToList();

                var updateTasks = group.Where(queueItem => queueItem.TaskType is LuceneTaskType.PUBLISH_INDEX or LuceneTaskType.UPDATE);
                var upsertData = new List<Document>();
                foreach (var queueItem in updateTasks)
                {
                    var document = await GetDocument(queueItem);
                    if (document is not null)
                    {
                        upsertData.Add(document);
                    }
                    else
                    {
                        deleteTasks.Add(queueItem);
                    }
                }
                deleteIds.AddRange(GetIdsToDelete(deleteTasks ?? []).Where(x => x is not null).Select(x => x ?? string.Empty));
                if (indexManager.GetIndex(group.Key) is { } index)
                {
                    previousBatchResults.SuccessfulOperations += await luceneClient.DeleteRecords(deleteIds, group.Key);
                    previousBatchResults.SuccessfulOperations += await luceneClient.UpsertRecords(upsertData, group.Key, cancellationToken);

                    if (group.Any(t => t.TaskType == LuceneTaskType.PUBLISH_INDEX) && !previousBatchResults.PublishedIndices.Any(x => x.IndexName == index.IndexName))
                    {
                        previousBatchResults.PublishedIndices.Add(index);
                    }
                }
                else
                {
                    eventLogService.LogError(nameof(DefaultLuceneTaskProcessor), nameof(ProcessLuceneTasks), "Index instance not exists");
                }
            }
            catch (Exception ex)
            {
                eventLogService.LogError(nameof(DefaultLuceneTaskProcessor), nameof(ProcessLuceneTasks), ex.Message);
            }
        }
    }

    private static IEnumerable<string?> GetIdsToDelete(IEnumerable<LuceneQueueItem> deleteTasks) => deleteTasks.Select(queueItem => queueItem.ItemToIndex.ItemGuid.ToString());

    /// <inheritdoc/>
    public async Task<Document?> GetDocument(LuceneQueueItem queueItem)
    {
        var luceneIndex = indexManager.GetRequiredIndex(queueItem.IndexName);

        var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);

        var data = await strategy.MapToLuceneDocumentOrNull(queueItem.ItemToIndex);

        if (data is null)
        {
            return null;
        }

        await AddBaseProperties(queueItem.ItemToIndex, data!);

        return data;
    }

    private async Task AddBaseProperties(IIndexEventItemModel item, Document document)
    {
        document.AddStringField(BaseDocumentProperties.CONTENT_TYPE_NAME, item.ContentTypeName, Field.Store.YES);
        document.AddStringField(BaseDocumentProperties.LANGUAGE_NAME, item.LanguageName, Field.Store.YES);
        document.AddStringField(BaseDocumentProperties.ITEM_GUID, item.ItemGuid.ToString(), Field.Store.YES);

        if (item is IndexEventWebPageItemModel webpageItem && !document.Any(x => string.Equals(x.Name, BaseDocumentProperties.URL, StringComparison.OrdinalIgnoreCase)))
        {
            string url = string.Empty;
            try
            {
                url = (await urlRetriever.Retrieve(webpageItem.WebPageItemTreePath, webpageItem.WebsiteChannelName, webpageItem.LanguageName)).RelativePath;
            }
            catch (Exception)
            {
                // Retrieve can throw an exception when processing a page update LuceneQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
            }

            document.AddStringField(BaseDocumentProperties.URL, url, Field.Store.YES);
        }
    }
}
