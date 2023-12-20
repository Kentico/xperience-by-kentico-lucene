using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        if (!indexedItem.IsLuceneIndexed(eventName))
        {
            return;
        }

        foreach (string? indexName in IndexStore.Instance.GetAllIndices().Select(index => index.IndexName))
        {
            if (!indexedItem.IsIndexedByIndex(indexName, eventName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);

            var toReindex = await luceneIndex.LuceneIndexingStrategy.FindItemsToReindex(indexedItem);
            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    if (item.WebPageItemGuid == indexedItem.WebPageItemGuid)
                    {
                        if (taskType == LuceneTaskType.DELETE)
                        {
                            LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.DELETE, indexName));
                        }
                        else
                        {
                            LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.UPDATE, indexName));
                        }
                    }
                }
            }
        }
    }

    public async Task HandleContentItemEvent(IndexedContentItemModel indexedItem, string eventName)
    {
        if (!indexedItem.IsLuceneIndexed(eventName))
        {
            return;
        }

        foreach (string? indexName in IndexStore.Instance.GetAllIndices().Select(index => index.IndexName))
        {
            if (!indexedItem.IsIndexedByIndex(indexName, eventName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);

            var toReindex = await luceneIndex.LuceneIndexingStrategy.FindItemsToReindex(indexedItem);
            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.UPDATE, indexName));
                }
            }
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
