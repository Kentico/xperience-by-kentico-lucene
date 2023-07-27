using System;
using System.Linq;

using CMS.Core;
using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Extensions;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILuceneTaskLogger"/>.
    /// </summary>
    internal class DefaultLuceneTaskLogger : ILuceneTaskLogger
    {
        private readonly IEventLogService eventLogService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultLuceneTaskLogger"/> class.
        /// </summary>
        public DefaultLuceneTaskLogger(IEventLogService eventLogService)
        {
            this.eventLogService = eventLogService;
        }


        /// <inheritdoc />
        public void HandleEvent(TreeNode node, string eventName)
        {
            var taskType = GetTaskType(node, eventName);

            // Check standard indexes
            if (!node.IsLuceneIndexed())
            {
                return;
            }

            foreach (var indexName in IndexStore.Instance.GetAllIndexes().Select(index => index.IndexName))
            {
                if (!node.IsIndexedByIndex(indexName))
                {
                    continue;
                }

                LogIndexTask(new LuceneQueueItem(node, taskType, indexName));
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


        private static LuceneTaskType GetTaskType(TreeNode node, string eventName)
        {
            if (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase) && node.WorkflowHistory.Count == 0)
            {
                return LuceneTaskType.CREATE;
            }

            if (eventName.Equals(WorkflowEvents.Publish.Name, StringComparison.OrdinalIgnoreCase) && node.WorkflowHistory.Count > 0)
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
}
