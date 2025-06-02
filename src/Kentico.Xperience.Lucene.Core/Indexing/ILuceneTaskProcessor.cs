namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Processes tasks from <see cref="LuceneQueueWorker"/>.
/// </summary>
public interface ILuceneTaskProcessor
{
    /// <summary>
    /// Prerocesses multiple queue items from all Lucene indexes in batches.
    /// </summary>
    /// <param name="queueItems">The items to process.</param>
    /// <param name="maximumBatchSize">The maximum number of items which can be processed in a single batch.</param>
    /// <returns>The number of items processed.</returns>
    Task<Dictionary<string, LucenePreprocessResult>> PreprocessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, int maximumBatchSize = 100);

    /// <summary>
    /// Finalizes returns the total number of successfully processed items. Lucene
    /// automatically applies batching in multiples of 1,000 when using their API,
    /// so all preprocessed items are forwarded to the API.
    /// </summary>
    /// <param name="lucenePreprocessingResults">A dictionary containing the preprocessing results for Lucene indices, where the key is the index name and the
    /// value is the corresponding preprocessing result.</param>
    /// <param name="cancellationToken">The cancellation token for the task.</param>
    /// <returns>The number of items processed.</returns>
    Task<int> ProcessLuceneIndices(Dictionary<string, LucenePreprocessResult> lucenePreprocessingResults, CancellationToken cancellationToken);
}
