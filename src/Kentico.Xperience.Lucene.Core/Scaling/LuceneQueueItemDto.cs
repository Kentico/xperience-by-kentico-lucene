using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Core.Scaling;

internal class LuceneQueueItemDto<TIndexEventItemModel> where TIndexEventItemModel : class, IIndexEventItemModel, new()
{
    public TIndexEventItemModel ItemToIndex { get; set; } = new();

    public LuceneTaskType TaskType { get; set; }

    public string IndexName { get; set; } = string.Empty;
}
