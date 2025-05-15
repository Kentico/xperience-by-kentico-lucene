using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;
internal class DeleteIndexWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    public string? CreatorName { get; set; }
    public LuceneIndex? LuceneIndex { get; set; }

    public DeleteIndexWebFarmTask()
        => eventLog = Service.Resolve<IEventLogService>();

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Lucene Delete Index task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Delete Task", "Execute", message);

        LuceneIndex!.StorageContext.DeleteIndex().GetAwaiter().GetResult();
    }
}
