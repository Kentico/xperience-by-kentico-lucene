using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class ProcessLuceneTasksWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneTaskProcessor luceneTaskProcessor;
    public string? CreatorName { get; set; }
    public List<LuceneQueueItemDto<IndexEventWebPageItemModel>>? LuceneWebPageQueueItems { get; set; }
    public List<LuceneQueueItemDto<IndexEventReusableItemModel>>? LuceneReusableQueueItems { get; set; }

    public ProcessLuceneTasksWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneTaskProcessor = Service.Resolve<ILuceneTaskProcessor>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Process Lucene Tasks task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Process Lucene Tasks Task", "Execute", message);

        var queueItems = LuceneWebPageQueueItems!.Select(x => new LuceneQueueItem(x.ItemToIndex, x.TaskType, x.IndexName)).ToList();
        queueItems.AddRange(LuceneReusableQueueItems!.Select(x => new LuceneQueueItem(x.ItemToIndex, x.TaskType, x.IndexName)));

        luceneTaskProcessor.ProcessLuceneTasks(queueItems, CancellationToken.None).GetAwaiter().GetResult();
    }
}
