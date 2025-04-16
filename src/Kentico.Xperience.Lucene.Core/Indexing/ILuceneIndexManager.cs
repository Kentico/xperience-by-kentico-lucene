namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Manages adding and getting of Lucene indexes.
/// </summary>
public interface ILuceneIndexManager
{
    /// <summary>
    /// Gets all existing indexes.
    /// </summary>
    IEnumerable<LuceneIndex> GetAllIndices();

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="indexName"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    LuceneIndex? GetIndex(string indexName);

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="identifier"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="identifier">The identifier of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    LuceneIndex? GetIndex(int identifier);

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="indexName"/>. If no index is found, a <see cref="InvalidOperationException" /> is thrown.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    LuceneIndex GetRequiredIndex(string indexName);

    /// <summary>
    /// Adds an index to the store.
    /// </summary>
    /// <param name="indexConfiguration">The index to add.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    void AddIndex(LuceneIndexModel indexConfiguration);
}
