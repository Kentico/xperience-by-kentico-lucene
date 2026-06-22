using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Search;

namespace Kentico.Xperience.Lucene.Core.Scaling;

/// <summary>
/// Web farm task that drops a server's cached search reader for an index. Broadcast after a publish,
/// reset, or delete when the index lives on external (shared) storage, where the operation itself is not
/// replicated to other servers but their in-memory <see cref="LuceneIndexSearcherProvider"/> caches still
/// point at the now-replaced generation.
/// </summary>
internal class InvalidateSearchIndexWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly LuceneIndexSearcherProvider searcherProvider;
    public string? IndexName { get; set; }
    public string? CreatorName { get; set; }

    public InvalidateSearchIndexWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        searcherProvider = Service.Resolve<LuceneIndexSearcherProvider>();
    }

    public override void ExecuteTask()
    {
        if (string.IsNullOrEmpty(IndexName))
        {
            return;
        }

        string message = $"Server {SystemContext.ServerName} is processing an Invalidate Search Index task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Invalidate Search Index Task", "Execute", message);

        searcherProvider.Invalidate(IndexName);
    }
}
