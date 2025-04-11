namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Contains methods for logging <see cref="LuceneQueueItem"/>s and <see cref="LuceneQueueItem"/>s
/// for processing by <see cref="LuceneQueueWorker"/> and <see cref="LuceneQueueWorker"/>.
/// </summary>
public interface ILuceneTaskLogger
{
    /// <summary>
    /// Logs an <see cref="LuceneQueueItem"/> for each registered crawler. Then, loops
    /// through all registered Lucene indexes and logs a task if the passed <paramref name="webpageItem"/> is indexed.
    /// </summary>
    /// <param name="webpageItem">The <see cref="IndexEventWebPageItemModel"/> that triggered the event.</param>
    /// <param name="eventName">The name of the Xperience event that was triggered.</param>
    Task HandleEvent(IndexEventWebPageItemModel webpageItem, string eventName);

    Task HandleReusableItemEvent(IndexEventReusableItemModel reusableItem, string eventName);

    /// <summary>
    /// Logs a single <see cref="LuceneQueueItem"/>.
    /// </summary>
    /// <param name="task">The task to log.</param>
    void LogIndexTask(LuceneQueueItem task);
}
