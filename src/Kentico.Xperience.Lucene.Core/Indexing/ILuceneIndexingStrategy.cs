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
    public Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item);


    /// <summary>
    /// When overridden and configuration supplied, indexing will also create taxonomy index for facet search
    /// </summary>
    /// <returns>Facet configuration for the index that is used in indexing and querying</returns>
    public FacetsConfig? FacetsConfigFactory();


    /// <summary>
    /// Triggered by modifications to a web page item, which is provided to determine what other items should be included for indexing
    /// </summary>
    /// <param name="changedItem">The web page item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToLuceneDocumentOrNull"/> for indexing</returns>
    public Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem);


    /// <summary>
    /// Triggered by modifications to a reusable content item, which is provided to determine what other items should be included for indexing 
    /// </summary>
    /// <param name="changedItem">The reusable content item that was modified</param>
    /// <returns>Items that should be passed to <see cref="MapToLuceneDocumentOrNull"/> for indexing</returns>
    public Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem);


    /// <summary>
    /// Declares how the fields this strategy indexes can be searched, so the administration Search tab can offer typed filters.
    /// Fields from <see cref="BaseDocumentProperties"/> are declared by the library and do not need to be returned.
    /// Indexed fields without a descriptor are listed as untyped and can still be searched with a Lucene query.
    /// </summary>
    /// <returns>Descriptors of fields this strategy indexes. A descriptor overrides a base field with the same name.</returns>
    public IEnumerable<LuceneSearchFieldDescriptor> GetSearchFieldDescriptors() => [];
}
