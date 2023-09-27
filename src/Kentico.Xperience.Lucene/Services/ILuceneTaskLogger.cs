using CMS.Websites;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services;

/// <summary>
/// Contains methods for logging <see cref="LuceneQueueItem"/>s and <see cref="LuceneQueueItem"/>s
/// for processing by <see cref="LuceneQueueWorker"/> and <see cref="LuceneQueueWorker"/>.
/// </summary>
public interface ILuceneTaskLogger
{
    /// <summary>
    /// Logs an <see cref="LuceneQueueItem"/> for each registered crawler. Then, loops
    /// through all registered Lucene indexes and logs a task if the passed <paramref name="pageContentContainer"/> is indexed.
    /// </summary>
    /// <param name="pageContentContainer">The <see cref="IWebPageContentQueryDataContainer"/> that triggered the event.</param>
    /// <param name="eventName">The name of the Xperience event that was triggered.</param>
    void HandleEvent(IWebPageContentQueryDataContainer pageContentContainer, string eventName);
}
