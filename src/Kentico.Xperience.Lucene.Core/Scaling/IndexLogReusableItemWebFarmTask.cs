using CMS.Core;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;
internal class IndexLogReusableItemWebFarmTask : WebFarmTaskBase
{
    private readonly ILuceneTaskLogger luceneTaskLogger;
    public IndexEventReusableItemModel? Data { get; set; }
    public LuceneTaskType TaskType { get; set; }
    public string? IndexName { get; set; }

    public IndexLogReusableItemWebFarmTask() =>
        luceneTaskLogger = Service.Resolve<ILuceneTaskLogger>();

    public override void ExecuteTask() =>
        luceneTaskLogger.LogIndexTask(new LuceneQueueItem(Data!, TaskType, IndexName!));
}
