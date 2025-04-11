using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Xperience.Lucene.Core.Indexing;

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
    /// <returns>Facet configuration for the index thta is used in indexing and querying</returns>
    FacetsConfig? FacetsConfigFactory();

    /// <summary>
    /// Triggered by modifications to a web page item, which is provided to determine what other items should be included for indexing
    /// </summary>
    /// <param name="changedItem">The web page item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToLuceneDocumentOrNull"/> for indexing</returns>
    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem);

    /// <summary>
    /// Triggered by modifications to a reusable content item, which is provided to determine what other items should be included for indexing 
    /// </summary>
    /// <param name="changedItem">The reusable content item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToLuceneDocumentOrNull"/> for indexing</returns>
    Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem);
}
