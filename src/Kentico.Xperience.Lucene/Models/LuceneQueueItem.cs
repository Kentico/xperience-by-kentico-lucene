using CMS.Websites;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// A queued item to be processed by <see cref="LuceneQueueWorker"/> which
/// represents a recent change made to an indexed <see cref="IWebPageContentQueryDataContainer"/> which is a representation of a WebPageItem.
/// </summary>
public sealed class LuceneQueueItem
{
    /// <summary>
    /// The <see cref="IWebPageContentQueryDataContainer"/> that was changed.
    /// </summary>
    public IWebPageContentQueryDataContainer PageContentContainer
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
    /// The language where the index is applied.
    /// </summary>
    public string Language
    {
        get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneQueueItem"/> class.
    /// </summary>
    /// <param name="pageContentContainer">The <see cref="IWebPageContentQueryDataContainer"/> that was changed.</param>
    /// <param name="taskType">The type of the Lucene task.</param>
    /// <param name="indexName">The code name of the Lucene index to be updated.</param>
    /// <param name="language">The language where the Index is applied.</param>
    /// <exception cref="ArgumentNullException" />
    public LuceneQueueItem(IWebPageContentQueryDataContainer pageContentContainer, LuceneTaskType taskType, string indexName, string language)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        PageContentContainer = pageContentContainer;
        if (taskType != LuceneTaskType.PUBLISH_INDEX && pageContentContainer == null)
        {
            throw new ArgumentNullException(nameof(pageContentContainer));
        }
        TaskType = taskType;
        IndexName = indexName;
        Language = language;
    }
}
