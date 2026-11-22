namespace Kentico.Xperience.Lucene.Core.Indexing;


/// <summary>
/// The service that provides storage and retrieval of Lucene index configurations.
/// </summary>
public interface ILuceneConfigurationStorageService
{
    /// <summary>
    /// Tries to create a new index configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="LuceneIndexModel"/> to be created.</param>
    /// <returns><see cref="bool"/> indication whether the creation was successful.</returns>
    public bool TryCreateIndex(LuceneIndexModel configuration);


    /// <summary>
    /// Tries to edit an existing index configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="LuceneIndexModel"/> to be edited.</param>
    /// <returns><see cref="bool"/> indication whether the editation was successful.</returns>
    public Task<bool> TryEditIndexAsync(LuceneIndexModel configuration);


    /// <summary>
    /// Tries to delete an existing index configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="LuceneIndexModel"/> to be deleted.</param>
    /// <returns><see cref="bool"/> indication whether the deletion was successful.</returns>
    public bool TryDeleteIndex(LuceneIndexModel configuration);


    /// <summary>
    /// Tries to delete an existing index configuration.
    /// </summary>
    /// <param name="id">The id of the <see cref="LuceneIndex"/> to be deleted.</param>
    /// <returns><see cref="bool"/> indication whether the deletion was successful.</returns>
    public bool TryDeleteIndex(int id);


    /// <summary>
    /// Gets the index configuration data or null if it doesn't exist.
    /// </summary>
    /// <param name="indexId">The id of the index.</param>
    /// <returns>The <see cref="Nullable{LuceneIndexModel}"/>.</returns>
    public Task<LuceneIndexModel?> GetIndexDataOrNullAsync(int indexId);


    /// <summary>
    /// Gets the index configuration data or null if it doesn't exist.
    /// </summary>
    /// <param name="indexName">The name of the index.</param>
    /// <returns>The <see cref="Nullable{LuceneIndexModel}"/>.</returns>
    public Task<LuceneIndexModel?> GetIndexDataOrNullAsync(string indexName);


    /// <summary>
    /// Gets the names of the existing indexes.
    /// </summary>
    /// <returns>The names of the existing indexes.</returns>
    public List<string> GetExistingIndexNames();


    /// <summary>
    /// Gets the ids of the existing indexes.
    /// </summary>
    /// <returns>The ids of the existing indexes.</returns>
    public List<int> GetIndexIds();


    /// <summary>
    /// Gets all index configurations.
    /// </summary>
    /// <returns>All registered <see cref="IEnumerable{LuceneIndexModel}"/>.</returns>
    public Task<IEnumerable<LuceneIndexModel>> GetAllIndexDataAsync();
}
