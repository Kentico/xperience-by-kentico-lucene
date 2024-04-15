using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Xperience.Lucene.Core.Indexing;

/// <summary>
/// Default indexing strategy that provides simple indexing.
/// </summary>
public class DefaultLuceneIndexingStrategy : ILuceneIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
    /// By default, no custom content item fields or secured items are indexed, only the contents of <see cref="IIndexEventItemModel"/> and fields defined in <see cref="BaseDocumentProperties"/>
    /// </summary>
    /// <param name="item">The <see cref="IIndexEventItemModel"/> currently being indexed.</param>
    /// <returns>Modified Lucene document.</returns>
    public virtual Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item.IsSecured)
        {
            return Task.FromResult<Document?>(null);
        }

        var indexDocument = new Document()
        {
            new TextField(nameof(item.Name), item.Name, Field.Store.YES),
        };

        return Task.FromResult<Document?>(indexDocument);
    }

    /// <inheritdoc />
    public virtual FacetsConfig? FacetsConfigFactory() => null;

    /// <inheritdoc />
    public virtual async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem) => await Task.FromResult(new List<IIndexEventItemModel>() { changedItem });

    /// <inheritdoc />
    public virtual async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem) => await Task.FromResult(new List<IIndexEventItemModel>());
}

