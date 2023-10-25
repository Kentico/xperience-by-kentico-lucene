using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Index;

namespace Kentico.Xperience.Lucene.Services;

public interface ILuceneIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
    /// </summary>
    /// <param name="indexedItem">The <see cref="IndexedItemModel"/> currently being indexed.</param>
    /// <param name="model">The resulting search data <see cref="LuceneSearchModel"/> to be modified. The model could be changed during the process.</param>
    /// <returns>Modified Lucene document.</returns>
    Task<LuceneSearchModel> OnIndexingNode(IndexedItemModel indexedItem, LuceneSearchModel model);

    /// <summary>
    /// Called when indexing a search model. Could be used to disable indexing of documents that match the scope, but should not be indexed e.g. error pages.
    /// </summary>
    /// <param name="indexedItem">The <see cref="IndexedItemModel"/> currently being indexed.</param>
    /// <returns>bool</returns>
    Task<bool> ShouldIndexNode(IndexedItemModel indexedItem);

    /// <summary>
    /// When overriden and configuration supplied, indexing will also create taxonomy index for facet search
    /// </summary>
    /// <returns></returns>
    FacetsConfig? FacetsConfigFactory();

    /// <summary>
    /// Called when field is added to document 
    /// </summary>
    /// <param name="document">indexed document</param>
    /// <param name="field">indexed field</param>
    void OnDocumentAddField(Document document, IIndexableField field);
}
