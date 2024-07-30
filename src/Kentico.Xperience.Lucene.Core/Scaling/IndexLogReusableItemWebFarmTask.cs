using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;
internal class IndexLogReusableItemWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneTaskLogger luceneTaskLogger;
    public string? CreatorName { get; set; }
    public IndexEventReusableItemModel? Data { get; set; }
    public LuceneTaskType TaskType { get; set; }
    public string? IndexName { get; set; }

    public IndexLogReusableItemWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneTaskLogger = Service.Resolve<ILuceneTaskLogger>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Lucene indexing task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Indexing Task", "Execute", message);

        luceneTaskLogger.LogIndexTask(new LuceneQueueItem(Data!, TaskType, IndexName!));
    }
}
