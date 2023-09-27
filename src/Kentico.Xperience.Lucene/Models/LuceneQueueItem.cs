using CMS.DocumentEngine;
using CMS.Websites;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// A queued item to be processed by <see cref="LuceneQueueWorker"/> which
/// represents a recent change made to an indexed <see cref="TreeNode"/>.
/// </summary>
public sealed class LuceneQueueItem
{
    /// <summary>
    /// The <see cref="TreeNode"/> that was changed.
    /// </summary>
    public IWebPageContentQueryDataContainer Container
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

    public string Language
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneQueueItem"/> class.
    /// </summary>
    /// <param name="container">The <see cref="TreeNode"/> that was changed.</param>
    /// <param name="taskType">The type of the Lucene task.</param>
    /// <param name="indexName">The code name of the Lucene index to be updated.</param>
    /// <exception cref="ArgumentNullException" />
    public LuceneQueueItem(IWebPageContentQueryDataContainer container, LuceneTaskType taskType, string indexName, string language)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        Container = container;
        if (taskType != LuceneTaskType.PUBLISH_INDEX && container == null)
        {
            throw new ArgumentNullException(nameof(container));
        }
        TaskType = taskType;
        IndexName = indexName;
        Language = language;
    }
}
