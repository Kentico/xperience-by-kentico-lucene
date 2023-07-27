using CMS.DocumentEngine;

using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Contains methods for logging <see cref="LuceneQueueItem"/>s and <see cref="LuceneCrawlerQueueItem"/>s
    /// for processing by <see cref="LuceneQueueWorker"/> and <see cref="LuceneCrawlerQueueWorker"/>.
    /// </summary>
    public interface ILuceneTaskLogger
    {
        /// <summary>
        /// Logs an <see cref="LuceneCrawlerQueueItem"/> for each registered crawler. Then, loops
        /// through all registered Lucene indexes and logs a task if the passed <paramref name="node"/> is indexed.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> that triggered the event.</param>
        /// <param name="eventName">The name of the Xperience event that was triggered.</param>
        void HandleEvent(TreeNode node, string eventName);
    }
}
