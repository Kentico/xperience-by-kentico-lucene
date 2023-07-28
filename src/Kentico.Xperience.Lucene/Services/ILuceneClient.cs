using Kentico.Xperience.Lucene.Models;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Contains methods to interface with the Lucene API.
    /// </summary>
    public interface ILuceneClient
    {
        /// <summary>
        /// Removes records from the Lucene index.
        /// </summary>
        /// <param name="objectIds">The Lucene internal IDs of the records to delete.</param>
        /// <param name="indexName">The index containing the objects to delete.</param>
        /// 
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="OverflowException" />
        /// <returns>The number of records deleted.</returns>
        Task<int> DeleteRecords(IEnumerable<string> objectIds, string indexName);

        /// <summary>
        /// Gets the indices of the Lucene application with basic statistics.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        Task<ICollection<LuceneIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken);



        /// <summary>
        /// Updates the Lucene index with the dynamic data in each object of the passed <paramref name="dataObjects"/>.
        /// </summary>
        /// <remarks>Logs an error if there are issues loading the node data.</remarks>
        /// <param name="dataObjects">The objects to upsert into Lucene.</param>
        /// <param name="indexName">The index to upsert the data to.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="OverflowException" />
        /// <returns>The number of objects processed.</returns>
        Task<int> UpsertRecords(IEnumerable<LuceneSearchModel> dataObjects, string indexName, CancellationToken cancellationToken);


        /// <summary>
        /// Rebuilds the Lucene index by removing existing data from Lucene and indexing all
        /// pages in the content tree included in the index.
        /// </summary>
        /// <param name="indexName">The index to rebuild.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="OperationCanceledException" />
        /// <exception cref="ObjectDisposedException" />
        Task Rebuild(string indexName, CancellationToken cancellationToken);
    }
}
