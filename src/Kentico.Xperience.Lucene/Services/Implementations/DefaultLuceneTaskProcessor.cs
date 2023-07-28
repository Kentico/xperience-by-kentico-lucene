using CMS.Core;
using CMS.DocumentEngine;
using CMS.WorkflowEngine;

using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    internal class DefaultLuceneTaskProcessor : ILuceneTaskProcessor
    {
        private readonly ILuceneClient luceneClient;
        private readonly ILuceneModelGenerator luceneObjectGenerator;
        private readonly IEventLogService eventLogService;
        private readonly IWorkflowStepInfoProvider workflowStepInfoProvider;
        private readonly IVersionHistoryInfoProvider versionHistoryInfoProvider;


        public DefaultLuceneTaskProcessor(ILuceneClient luceneClient,
            IEventLogService eventLogService,
            IWorkflowStepInfoProvider workflowStepInfoProvider,
            IVersionHistoryInfoProvider versionHistoryInfoProvider,
            ILuceneModelGenerator luceneObjectGenerator)
        {
            this.luceneClient = luceneClient;
            this.eventLogService = eventLogService;
            this.workflowStepInfoProvider = workflowStepInfoProvider;
            this.versionHistoryInfoProvider = versionHistoryInfoProvider;
            this.luceneObjectGenerator = luceneObjectGenerator;
        }


        /// <inheritdoc />
        public async Task<int> ProcessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken)
        {
            int successfulOperations = 0;

            // Group queue items based on index name
            var groups = queueItems.GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    var deleteIds = new List<string>();
                    var deleteTasks = group.Where(queueItem => queueItem.TaskType == LuceneTaskType.DELETE);
                    deleteIds.AddRange(GetIdsToDelete(deleteTasks));

                    var updateTasks = group.Where(queueItem => queueItem.TaskType is LuceneTaskType.UPDATE or LuceneTaskType.CREATE);
                    var upsertData = new List<LuceneSearchModel>();
                    foreach (var queueItem in updateTasks)
                    {
                        var data = await luceneObjectGenerator.GetTreeNodeData(queueItem);
                        upsertData.Add(data);
                    }

                    successfulOperations += await luceneClient.DeleteRecords(deleteIds, group.Key);
                    successfulOperations += await luceneClient.UpsertRecords(upsertData, group.Key, cancellationToken);
                }
                catch (Exception ex)
                {
                    eventLogService.LogError(nameof(DefaultLuceneTaskProcessor), nameof(ProcessLuceneTasks), ex.Message);
                }
            }

            return successfulOperations;
        }

        private IEnumerable<string> GetIdsToDelete(IEnumerable<LuceneQueueItem> deleteTasks) => deleteTasks.Select(queueItem => queueItem.Node.DocumentID.ToString());
    }
}
