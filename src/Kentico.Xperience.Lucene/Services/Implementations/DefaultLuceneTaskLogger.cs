using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Extensions;
using Kentico.Xperience.Lucene.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Default implementation of <see cref="ILuceneTaskLogger"/>.
/// </summary>
internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
{
    private readonly IEventLogService eventLogService;
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
    /// </summary>
    public DefaultLuceneTaskLogger(IEventLogService eventLogService, IServiceProvider serviceProvider)
    {
        this.eventLogService = eventLogService;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task HandleEvent(IndexedItemModel indexedModel, string eventName)
    {
        var taskType = GetTaskType(eventName);

        if (!indexedModel.IsLuceneIndexed(eventLogService, eventName))
        {
            return;
        }

        foreach (string? indexName in IndexStore.Instance.GetAllIndices().Select(index => index.IndexName))
        {
            if (!indexedModel.IsIndexedByIndex(eventLogService, indexName, eventName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);

            if (luceneIndex is not null)
            {
                var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                var toReindex = await strategy!.FindItemsToReindex(indexedModel);

                if (toReindex is not null)
                {
                    foreach (var item in toReindex)
                    {
                        if (item.WebPageItemGuid == indexedModel.WebPageItemGuid)
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
    }

    public async Task HandleContentItemEvent(IndexedContentItemModel indexedItem, string eventName)
    {
        foreach (string? indexName in IndexStore.Instance.GetAllIndices().Select(index => index.IndexName))
        {
            if (!indexedItem.IsIndexedByIndex(eventLogService, indexName, eventName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            if (luceneIndex is not null)
            {
                var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
                var toReindex = await strategy!.FindItemsToReindex(indexedItem);
                if (toReindex is not null)
                {
                    foreach (var item in toReindex)
                    {
                        LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.UPDATE, indexName));
                    }
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
