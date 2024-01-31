using Kentico.Xperience.Lucene.Admin;

namespace Kentico.Xperience.Lucene.Indexing;

/// <summary>
/// Represents a global singleton store of Lucene indexes
/// </summary>
public sealed class LuceneIndexStore
{
    private static readonly Lazy<LuceneIndexStore> mInstance = new();
    private readonly List<LuceneIndex> registeredIndexes = [];

    /// <summary>
    /// Gets singleton instance of the <see cref="LuceneIndexStore"/>
    /// </summary>
    public static LuceneIndexStore Instance => mInstance.Value;

    /// <summary>
    /// Gets all registered indexes.
    /// </summary>
    public IEnumerable<LuceneIndex> GetAllIndices() => registeredIndexes;

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="indexName"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public LuceneIndex? GetIndex(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            return null;
        }

        return registeredIndexes.SingleOrDefault(i => i.IndexName.Equals(indexName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="identifier"/>,
    /// or <c>null</c>.
    /// </summary>
    /// <param name="identifier">The identifier of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public LuceneIndex? GetIndex(int identifier) => registeredIndexes.Find(i => i.Identifier == identifier);

    /// <summary>
    /// Gets a registered <see cref="LuceneIndex"/> with the specified <paramref name="indexName"/>. If no index is found, a <see cref="InvalidOperationException" /> is thrown.
    /// </summary>
    /// <param name="indexName">The name of the index to retrieve.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public LuceneIndex GetRequiredIndex(string indexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentException("Value must not be null or empty");
        }

        return registeredIndexes.SingleOrDefault(i => i.IndexName.Equals(indexName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The index '{indexName}' is not registered.");
    }

    /// <summary>
    /// Adds an index to the store.
    /// </summary>
    /// <param name="index">The index to add.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    internal void AddIndex(LuceneIndex index)
    {
        if (index == null)
        {
            throw new ArgumentNullException(nameof(index));
        }

        if (registeredIndexes.Exists(i => i.IndexName.Equals(index.IndexName, StringComparison.OrdinalIgnoreCase) || index.Identifier == i.Identifier))
        {
            throw new InvalidOperationException($"Attempted to register Lucene index with identifer [{index.Identifier}] and name [{index.IndexName}] but it is already registered.");
        }

        registeredIndexes.Add(index);
    }

    /// <summary>
    /// Resets all indicies
    /// </summary>
    /// <param name="models"></param>
    internal void SetIndicies(IEnumerable<LuceneConfigurationModel> models)
    {
        registeredIndexes.Clear();
        foreach (var index in models)
        {
            Instance.AddIndex(new LuceneIndex(index, StrategyStorage.Strategies));
        }
    }

    /// <summary>
    /// Sets the current indicies to those provided by <paramref name="configurationService"/>
    /// </summary>
    /// <param name="configurationService"></param>
    internal static void SetIndicies(ILuceneConfigurationStorageService configurationService)
    {
        var indices = configurationService.GetAllIndexData();

        Instance.SetIndicies(indices);
    }
}
