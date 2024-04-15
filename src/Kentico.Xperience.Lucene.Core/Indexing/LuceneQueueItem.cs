namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// A queued item to be processed by <see cref="LuceneQueueWorker"/> which
/// represents a recent change made to an indexed <see cref="ItemToIndex"/> which is a representation of a <see cref="IIndexEventItemModel"/>.
/// </summary>
public sealed class LuceneQueueItem
{
    /// <summary>
    /// The <see cref="ItemToIndex"/> that was changed.
    /// </summary>
    public IIndexEventItemModel ItemToIndex { get; }

    /// <summary>
    /// The type of the Lucene task.
    /// </summary>
    public LuceneTaskType TaskType { get; }

    /// <summary>
    /// The code name of the Lucene index to be updated.
    /// </summary>
    public string IndexName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneQueueItem"/> class.
    /// </summary>
    /// <param name="itemToIndex">The <see cref="IIndexEventItemModel"/> that was changed.</param>
    /// <param name="taskType">The type of the Lucene task.</param>
    /// <param name="indexName">The code name of the Lucene index to be updated.</param>
    /// <exception cref="ArgumentNullException" />
    public LuceneQueueItem(IIndexEventItemModel itemToIndex, LuceneTaskType taskType, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }
        if (taskType != LuceneTaskType.PUBLISH_INDEX && itemToIndex == null)
        {
            throw new ArgumentNullException(nameof(itemToIndex));
        }

        ItemToIndex = itemToIndex;
        TaskType = taskType;
        IndexName = indexName;
    }
}
