using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;
internal class RebuildWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneClient luceneClient;
    public string? IndexName { get; set; }
    public string? CreatorName { get; set; }

    public RebuildWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneClient = Service.Resolve<ILuceneClient>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Lucene Rebuild task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Rebuild Task", "Execute", message);

        luceneClient.Rebuild(IndexName!, default).GetAwaiter().GetResult();
    }
}
