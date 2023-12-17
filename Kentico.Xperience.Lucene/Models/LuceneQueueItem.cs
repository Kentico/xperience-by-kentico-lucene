using CMS.Websites;
using System;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// A queued item to be processed by <see cref="LuceneQueueWorker"/> which
/// represents a recent change made to an indexed <see cref="IndexedItemModel"/> which is a representation of a WebPageItem.
/// </summary>
public sealed class LuceneQueueItem
{
    /// <summary>
    /// The <see cref="IndexedItemModel"/> that was changed.
    /// </summary>
    public IndexedItemModel IndexedItemModel
    {
        get;
    }


    /// <summary>
    /// The type of the Lucene task.
    /// </summary>
    public LuceneTaskType TaskType
    {
        get;
    }


    /// <summary>
    /// The code name of the Lucene index to be updated.
    /// </summary>
    public string IndexName
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneQueueItem"/> class.
    /// </summary>
    /// <param name="indexedItem">The <see cref="Models.IndexedItemModel"/> that was changed.</param>
    /// <param name="taskType">The type of the Lucene task.</param>
    /// <param name="indexName">The code name of the Lucene index to be updated.</param>
    /// <param name="language">The language where the Index is applied.</param>
    /// <exception cref="ArgumentNullException" />
    public LuceneQueueItem(IndexedItemModel indexedItem, LuceneTaskType taskType, string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        IndexedItemModel = indexedItem;
        if (taskType != LuceneTaskType.PUBLISH_INDEX && indexedItem == null)
        {
            throw new ArgumentNullException(nameof(indexedItem));
        }
        TaskType = taskType;
        IndexName = indexName;
    }
}
