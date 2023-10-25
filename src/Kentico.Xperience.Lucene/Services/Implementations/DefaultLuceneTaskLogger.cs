using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Extensions;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Default implementation of <see cref="ILuceneTaskLogger"/>.
/// </summary>
internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
{
    private readonly IEventLogService eventLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
    /// </summary>
    public DefaultLuceneTaskLogger(IEventLogService eventLogService) => this.eventLogService = eventLogService;


    /// <inheritdoc />
    public async Task HandleEvent(IndexedItemModel indexedItem, string eventName)
    {
        var taskType = GetTaskType(eventName);

        if (!await indexedItem.IsLuceneIndexed(eventName))
        {
            return;
        }

        foreach (string? indexName in IndexStore.Instance.GetAllIndexes().Select(index => index.IndexName))
        {
            if (!await indexedItem.IsIndexedByIndex(indexName, eventName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            LogIndexTask(new LuceneQueueItem(indexedItem, taskType, indexName, luceneIndex!.Language));
        }
    }


    /// <summary>
    /// Logs a single <see cref="LuceneQueueItem"/>.
    /// </summary>
    /// <param name="task">The task to log.</param>
    private void LogIndexTask(LuceneQueueItem task)
    {
        try
        {
            LuceneQueueWorker.EnqueueLuceneQueueItem(task);
        }
        catch (InvalidOperationException ex)
        {
            eventLogService.LogException(nameof(DefaultLuceneTaskLogger), nameof(LogIndexTask), ex);
        }
    }


    private static LuceneTaskType GetTaskType(string eventName)
    {
        if (eventName.Equals(WebPageEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.UPDATE;
        }

        if (eventName.Equals(WebPageEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(WebPageEvents.Archive.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.DELETE;
        }

        return LuceneTaskType.UNKNOWN;
    }
}
