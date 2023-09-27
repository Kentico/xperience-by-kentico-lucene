using CMS.Core;
using CMS.DocumentEngine;
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
    public void HandleEvent(IWebPageContentQueryDataContainer webPageItem, string eventName)
    {
        var taskType = GetTaskType(webPageItem, eventName);

        if (!webPageItem.IsLuceneIndexed())
        {
            return;
        }

        foreach (string? indexName in IndexStore.Instance.GetAllIndexes().Select(index => index.IndexName))
        {
            if (!webPageItem.IsIndexedByIndex(indexName))
            {
                continue;
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            LogIndexTask(new LuceneQueueItem(webPageItem, taskType, indexName, luceneIndex!.Language));
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


    private static LuceneTaskType GetTaskType(IWebPageContentQueryDataContainer node, string eventName)
    {
        //TODO if (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase) && node.WorkflowHistory.Count == 0)
        //This should be the create condition, but we do not have WorkFlowhistory

        if (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.CREATE;
        }

        if (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.UPDATE;
        }

        if (eventName.Equals(DocumentEvents.Delete.Name, StringComparison.OrdinalIgnoreCase) ||
            eventName.Equals(WorkflowEvents.Archive.Name, StringComparison.OrdinalIgnoreCase))
        {
            return LuceneTaskType.DELETE;
        }

        return LuceneTaskType.UNKNOWN;
    }
}
