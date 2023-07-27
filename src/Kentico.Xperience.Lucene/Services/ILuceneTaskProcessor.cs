using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Processes tasks from <see cref="LuceneQueueWorker"/>.
    /// </summary>
    public interface ILuceneTaskProcessor
    {
        /// <summary>
        /// Processes multiple queue items from all Lucene indexes in batches. Lucene
        /// automatically applies batching in multiples of 1,000 when using their API,
        /// so all queue items are forwarded to the API.
        /// </summary>
        /// <param name="queueItems">The items to process.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <returns>The number of items processed.</returns>
        Task<int> ProcessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken);

    }
}
