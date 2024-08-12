using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;
internal class IndexLogWebPageItemWebFarmTask : WebFarmTaskBase
{
    private readonly ILuceneTaskLogger luceneTaskLogger;
    public IndexEventWebPageItemModel? Data { get; set; }
    public LuceneTaskType TaskType { get; set; }
    public string? IndexName { get; set; }

    public IndexLogWebPageItemWebFarmTask() =>
        luceneTaskLogger = Service.Resolve<ILuceneTaskLogger>();

    public override void ExecuteTask() =>
        luceneTaskLogger.LogIndexTask(new LuceneQueueItem(Data!, TaskType, IndexName!));
}
