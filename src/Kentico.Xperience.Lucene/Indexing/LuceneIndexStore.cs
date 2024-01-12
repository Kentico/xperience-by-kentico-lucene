using Kentico.Xperience.Lucene.Admin;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Kentico.Xperience.Lucene.Indexing;

/// <summary>
/// Represents a store of Lucene indexes and crawlers.
/// </summary>
public sealed class LuceneIndexStore
{
    private static readonly Lazy<LuceneIndexStore> mInstance = new();
    private readonly List<LuceneIndex> registeredIndexes = new();

    /// <summary>
    /// Gets current instance of the <see cref="LuceneIndexStore"/> class.
    /// </summary>
    public static LuceneIndexStore Instance => mInstance.Value;

    /// <summary>
    /// Adds an index to the store.
    /// </summary>
    /// <param name="index">The index to add.</param>
    /// <exception cref="ArgumentNullException" />
    /// <exception cref="InvalidOperationException" />
    public void AddIndex(LuceneIndex index)
    {
        if (index == null)
        {
            throw new ArgumentNullException(nameof(index));
        }

        if (registeredIndexes.Exists(i => i.IndexName.Equals(index.IndexName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Attempted to register Lucene index with name '{index.IndexName},' but it is already registered.");
        }

        registeredIndexes.Add(index);
    }

    public void AddIndices(IEnumerable<LuceneConfigurationModel> models)
    {
        registeredIndexes.Clear();
        foreach (var index in models)
        {
            index.StrategyName ??= "";

            Instance.AddIndex(new LuceneIndex(
                new StandardAnalyzer(LuceneVersion.LUCENE_48),
                index.IndexName ?? "",
                index.ChannelName ?? "",
                index.LanguageNames?.ToList() ?? new(),
                index.Id,
                index.Paths ?? new(),
                indexPath: null,
                luceneIndexingStrategyType: StrategyStorage.Strategies[index.StrategyName] ?? typeof(DefaultLuceneIndexingStrategy)
            ));
        }
    }

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
    /// Gets all registered indexes.
    /// </summary>
    public IEnumerable<LuceneIndex> GetAllIndices() => registeredIndexes;


    internal void ClearIndexes() => registeredIndexes.Clear();

    internal LuceneIndex? GetIndex(int id) => registeredIndexes.Find(i => i.Identifier == id);
}
