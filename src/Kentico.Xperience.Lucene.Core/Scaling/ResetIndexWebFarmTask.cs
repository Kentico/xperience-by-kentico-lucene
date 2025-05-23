using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class ResetIndexWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneIndexService luceneIndexService;
    public LuceneIndex? LuceneIndex { get; set; }
    public string? CreatorName { get; set; }

    public ResetIndexWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneIndexService = Service.Resolve<ILuceneIndexService>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Reset Index task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Reset Index Task", "Execute", message);

        luceneIndexService.ResetIndex(LuceneIndex!);
    }
}
