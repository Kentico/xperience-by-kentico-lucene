using CMS.AutomationEngine.Internal;
using CMS.Base;
using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services;

internal class LuceneBatchResult
{
    internal int SuccessfulOperations { get; set; } = 0;
    internal HashSet<LuceneIndex> PublishedIndices { get; set; } = new HashSet<LuceneIndex>();
}

internal class DefaultLuceneTaskProcessor : ILuceneTaskProcessor
{
    internal const string URL_NAME = "Url";

    private readonly IWebPageUrlRetriever urlRetriever;

    private readonly ILuceneClient luceneClient;
    private readonly IEventLogService eventLogService;
    private readonly IWorkflowStepInfoProvider workflowStepInfoProvider;


    public DefaultLuceneTaskProcessor(ILuceneClient luceneClient,
        IEventLogService eventLogService,
        IWorkflowStepInfoProvider workflowStepInfoProvider,
        IWebPageUrlRetriever urlRetriever)
    {
        this.luceneClient = luceneClient;
        this.eventLogService = eventLogService;
        this.workflowStepInfoProvider = workflowStepInfoProvider;
        this.urlRetriever = urlRetriever;
    }


    /// <inheritdoc />
    public int ProcessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken, int maximumBatchSize = 100)
    {
        LuceneBatchResult batchResults = new();

        var batches = queueItems.Batch(maximumBatchSize);

        foreach (var batch in batches)
        {
            ProcessLuceneBatch(batch, cancellationToken, batchResults).Wait();
        }

        foreach (var index in batchResults.PublishedIndices)
        {
            var storage = index.StorageContext.GetNextOrOpenNextGeneration();
            index.StorageContext.PublishIndex(storage);
        }

        return batchResults.SuccessfulOperations;
    }

    private async Task ProcessLuceneBatch(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken, LuceneBatchResult previousBatchResults)
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
                deleteIds.AddRange(GetIdsToDelete(deleteTasks));
                if (IndexStore.Instance.GetIndex(group.Key) is { } index)
                {
                    previousBatchResults.SuccessfulOperations += await luceneClient.DeleteRecords(deleteIds, group.Key);
                    previousBatchResults.SuccessfulOperations += await luceneClient.UpsertRecords(upsertData, group.Key, cancellationToken);

                    if (group.Any(t => t.TaskType == LuceneTaskType.PUBLISH_INDEX))
                    {
                        if (!previousBatchResults.PublishedIndices.Any(x => x.IndexName == index.IndexName))
                        {
                            previousBatchResults.PublishedIndices.Add(index);   
                        }
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

    private IEnumerable<string> GetIdsToDelete(IEnumerable<LuceneQueueItem> deleteTasks) => deleteTasks.Select(queueItem => queueItem.IndexedItemModel.WebPageItemGuid.ToString());

    /// <inheritdoc/>
    public async Task<Document> GetDocument(LuceneQueueItem queueItem)
    {
        var luceneIndex = IndexStore.Instance.GetIndex(queueItem.IndexName) ?? throw new Exception($"LuceneIndex {queueItem.IndexName} not found!");

        var data = await luceneIndex.LuceneIndexingStrategy.MapToLuceneDocumentOrNull(queueItem.IndexedItemModel);

        if (data is null)
        {
            return null;
        }

        await AddBaseProperties(queueItem.IndexedItemModel, data!);

        return data;
    }

    /// <summary>
    /// Sets values in the <paramref name="data"/> object using the common search model properties
    /// located within the <see cref="LuceneSearchModel"/> class.
    /// </summary>
    /// <param name="pageContentContainer">The <see cref="IWebPageContentQueryDataContainer"/> to load values from.</param>
    /// <param name="data">The data object based on <see cref="LuceneSearchModel"/>.</param>
    /// <param name="languageName">The language on the WebSite which is indexed.</param>
    private async Task AddBaseProperties(IndexedItemModel lucenePageItem, Document document)
    {
        document.AddStringField(nameof(IndexedItemModel.ClassName), lucenePageItem.ClassName, Field.Store.YES);
        document.AddStringField(nameof(IndexedItemModel.WebPageItemGuid), lucenePageItem.WebPageItemGuid.ToString(), Field.Store.YES);
        document.AddStringField(nameof(IndexedItemModel.LanguageCode), lucenePageItem.LanguageCode, Field.Store.YES);

        string url;
        try
        {
            url = (await urlRetriever.Retrieve(lucenePageItem.WebPageItemGuid, lucenePageItem.LanguageCode)).RelativePath;
        }
        catch (Exception)
        {
            // Retrieve can throw an exception when processing a page update LuceneQueueItem
            // and the page was deleted before the update task has processed. In this case, upsert an
            // empty URL
            url = string.Empty;
        }

        document.AddStringField(URL_NAME, url, Field.Store.YES);
    }
}
