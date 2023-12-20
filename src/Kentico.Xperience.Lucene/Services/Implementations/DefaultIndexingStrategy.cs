using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services.Implementations;

/// <summary>
/// Default indexing startegy just implements the methods but does not change the data.
/// </summary>
public class DefaultLuceneIndexingStrategy : ILuceneIndexingStrategy
{
    /// <inheritdoc />
    public virtual Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel lucenePageItem) => Task.FromResult(new Document());

    /// <inheritdoc />
    public virtual FacetsConfig FacetsConfigFactory() => null;

    public virtual async Task<IEnumerable<IndexedItemModel>> FindItemsToReindex(IndexedItemModel changedItem) => await Task.FromResult(new List<IndexedItemModel>() { changedItem });

    public virtual async Task<IEnumerable<IndexedItemModel>> FindItemsToReindex(IndexedContentItemModel changedItem) => await Task.FromResult( new List<IndexedItemModel>());
}

