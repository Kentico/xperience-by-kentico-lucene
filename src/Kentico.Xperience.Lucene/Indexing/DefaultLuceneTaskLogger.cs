using CMS.Core;
using CMS.Websites;
using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Indexing;

/// <summary>
/// Default implementation of <see cref="ILuceneTaskLogger"/>.
/// </summary>
internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
{
    private readonly IEventLogService eventLogService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILuceneIndexManager indexManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
    /// </summary>
    public DefaultLuceneTaskLogger(IEventLogService eventLogService, IServiceProvider serviceProvider, ILuceneIndexManager indexManager)
    {
        this.eventLogService = eventLogService;
        this.serviceProvider = serviceProvider;
        this.indexManager = indexManager;
    }

    /// <inheritdoc />
    public async Task HandleEvent(IndexEventWebPageItemModel webpageItem, string eventName)
    {
        var taskType = GetTaskType(eventName);

        foreach (var luceneIndex in indexManager.GetAllIndices())
        {
            if (!webpageItem.IsIndexedByIndex(eventLogService, indexManager, luceneIndex.IndexName, eventName))
            {
                continue;
            }

            var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
            var toReindex = await strategy.FindItemsToReindex(webpageItem);

            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    if (item.ItemGuid == webpageItem.ItemGuid)
                    {
                        if (taskType == LuceneTaskType.DELETE)
                        {
                            LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.DELETE, luceneIndex.IndexName));
                        }
                        else
                        {
                            LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.UPDATE, luceneIndex.IndexName));
                        }
                    }
                }
            }
        }
    }

    public async Task HandleReusableItemEvent(IndexEventReusableItemModel reusableItem, string eventName)
    {
        foreach (var luceneIndex in indexManager.GetAllIndices())
        {
            if (!reusableItem.IsIndexedByIndex(eventLogService, indexManager, luceneIndex.IndexName, eventName))
            {
                continue;
            }

            var strategy = serviceProvider.GetRequiredStrategy(luceneIndex);
            var toReindex = await strategy.FindItemsToReindex(reusableItem);

            if (toReindex is not null)
            {
                foreach (var item in toReindex)
                {
                    LogIndexTask(new LuceneQueueItem(item, LuceneTaskType.UPDATE, luceneIndex.IndexName));
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
