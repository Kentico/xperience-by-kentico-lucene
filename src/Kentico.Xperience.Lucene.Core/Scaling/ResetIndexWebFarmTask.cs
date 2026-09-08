using CMS.Base;
using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class ResetIndexWebFarmTask : WebFarmTaskBase
{
    private readonly IEventLogService eventLog;
    private readonly ILuceneIndexService luceneIndexService;
    private readonly ILuceneIndexManager luceneIndexManager;
    private readonly LuceneIndexSearcherProvider searcherProvider;
    public string? IndexName { get; set; }
    public string? CreatorName { get; set; }

    public ResetIndexWebFarmTask()
    {
        eventLog = Service.Resolve<IEventLogService>();
        luceneIndexService = Service.Resolve<ILuceneIndexService>();
        luceneIndexManager = Service.Resolve<ILuceneIndexManager>();
        searcherProvider = Service.Resolve<LuceneIndexSearcherProvider>();
    }

    public override void ExecuteTask()
    {
        string message = $"Server {SystemContext.ServerName} is processing a Reset Index task from creator {CreatorName}";
        eventLog.LogInformation("Lucene Reset Index Task", "Execute", message);

        var luceneIndex = luceneIndexManager.GetRequiredIndex(IndexName!);

        // Drop this server's cached searcher before the reset and wait for in-flight searches to release it -
        // retention renames the previous generation, which fails while a cached reader holds handles inside
        // its folder. Invalidated locally only: every server runs this task itself.
        searcherProvider.InvalidateAndWait(luceneIndex.IndexName, LuceneIndexSearcherProvider.ReaderReleaseTimeout);

        luceneIndexService.ResetIndex(luceneIndex);
    }
}
