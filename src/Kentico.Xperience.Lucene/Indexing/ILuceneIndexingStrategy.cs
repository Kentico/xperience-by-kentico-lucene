using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Xperience.Lucene.Indexing;

public interface ILuceneIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
    /// </summary>
    /// <param name="item">The <see cref="IIndexEventItemModel"/> currently being indexed.</param>
    /// <returns>Modified Lucene document.</returns>
    Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item);

    /// <summary>
    /// When overriden and configuration supplied, indexing will also create taxonomy index for facet search
    /// </summary>
    /// <returns></returns>
    FacetsConfig? FacetsConfigFactory();

    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem);

    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem);
}
