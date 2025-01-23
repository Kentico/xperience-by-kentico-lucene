using CMS.Core;
using CMS.Websites;

using Kentico.Xperience.Lucene.Core.Scaling;

using Microsoft.Extensions.DependencyInjection;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Default implementation of <see cref="ILuceneTaskLogger"/>.
/// </summary>
internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
{
    private readonly IEventLogService eventLogService;
    private readonly IServiceProvider serviceProvider;
    private readonly ILuceneIndexManager indexManager;
    private readonly IWebFarmService webFarmService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
    /// </summary>
    public DefaultLuceneTaskLogger(IEventLogService eventLogService,
        IServiceProvider serviceProvider,
        ILuceneIndexManager indexManager,
        IWebFarmService webFarmService)
    {
        this.eventLogService = eventLogService;
        this.serviceProvider = serviceProvider;
        this.indexManager = indexManager;
        this.webFarmService = webFarmService;
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
                            LogIndexTaskInternal(item, LuceneTaskType.DELETE, luceneIndex.IndexName);
                        }
                        else
                        {
                            LogIndexTaskInternal(item, LuceneTaskType.UPDATE, luceneIndex.IndexName);
                        }
                    }
                }
            }

            if (taskType == LuceneTaskType.DELETE)
            {
                LogIndexTaskInternal(webpageItem, LuceneTaskType.DELETE, luceneIndex.IndexName);
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
                    LogIndexTaskInternal(item, LuceneTaskType.UPDATE, luceneIndex.IndexName);
                }
            }
        }
    }

    /// <inheritdoc />
    public void LogIndexTask(LuceneQueueItem task)
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

    private void LogIndexTaskInternal(IIndexEventItemModel item, LuceneTaskType taskType, string indexName)
    {
        if (item is IndexEventReusableItemModel reusableItemModel)
        {
            webFarmService.CreateTask(new IndexLogReusableItemWebFarmTask()
            {
                Data = reusableItemModel,
                TaskType = taskType,
                IndexName = indexName
            });
        }
        else if (item is IndexEventWebPageItemModel webPageItemModel)
        {
            webFarmService.CreateTask(new IndexLogWebPageItemWebFarmTask()
            {
                Data = webPageItemModel,
                TaskType = taskType,
                IndexName = indexName
            });
        }

        LogIndexTask(new LuceneQueueItem(item, taskType, indexName));
    }

    private static LuceneTaskType GetTaskType(string eventName)
    {
        if (eventName.Equals(WebPageEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.UPDATE;
        }

        if (eventName.Equals(WebPageEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(WebPageEvents.Unpublish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.DELETE;
        }

        return LuceneTaskType.UNKNOWN;
    }
}
