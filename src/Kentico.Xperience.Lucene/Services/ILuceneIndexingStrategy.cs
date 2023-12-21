using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Xperience.Lucene.Services;

public interface ILuceneIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
    /// </summary>
    /// <param name="lucenePageItem">The <see cref="IndexedItemModel"/> currently being indexed.</param>
    /// <param name="model">The resulting search data <see cref="LuceneSearchModel"/> to be modified. The model could be changed during the process.</param>
    /// <returns>Modified Lucene document.</returns>
    Task<Document?> MapToLuceneDocumentOrNull(IndexedItemModel lucenePageItem);

    /// <summary>
    /// When overriden and configuration supplied, indexing will also create taxonomy index for facet search
    /// </summary>
    /// <returns></returns>
    FacetsConfig? FacetsConfigFactory();

    Task<IEnumerable<IndexedItemModel>> FindItemsToReindex(IndexedItemModel changedItem);

    Task<IEnumerable<IndexedItemModel>> FindItemsToReindex(IndexedContentItemModel changedItem);
}
