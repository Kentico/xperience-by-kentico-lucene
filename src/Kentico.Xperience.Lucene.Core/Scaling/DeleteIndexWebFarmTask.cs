using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class DeleteIndexWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneIndexManager luceneIndexManager;
    public string? CreatorName { get; set; }
    public string? IndexName { get; set; }

    public DeleteIndexWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneIndexManager = Service.Resolve<ILuceneIndexManager>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Lucene Delete Index task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Delete Task", "Execute", message);

        var luceneIndex = luceneIndexManager.GetRequiredIndex(IndexName!);
        luceneIndex!.StorageContext.DeleteIndex().GetAwaiter().GetResult();
    }
}
