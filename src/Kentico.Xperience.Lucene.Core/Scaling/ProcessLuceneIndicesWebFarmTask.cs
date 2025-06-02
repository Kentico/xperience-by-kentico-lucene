using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class ProcessLuceneIndicesWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneTaskProcessor luceneTaskProcessor;
    public string? CreatorName { get; set; }
    public Dictionary<string, LucenePreprocessResult>? LucenePreprocessResults { get; set; }

    public ProcessLuceneIndicesWebFarmTask()
    {
        luceneTaskProcessor = Service.Resolve<ILuceneTaskProcessor>();
        eventLog = Service.Resolve<IEventLogService>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Process Lucene Indices task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Process Lucene Indices Task", "Execute", message);
        luceneTaskProcessor.ProcessLuceneIndices(LucenePreprocessResults!, CancellationToken.None).GetAwaiter().GetResult();
    }
}
