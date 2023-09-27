using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using CMS.Websites;

namespace Kentico.Xperience.Lucene.Services.Implementations;

/// <summary>
/// Default indexing startegy just implements the methods but does not change the data.
/// </summary>
public class DefaultLuceneIndexingStrategy : ILuceneIndexingStrategy
{
    /// <inheritdoc />
    public virtual Task<object?> OnIndexingProperty(IWebPageContentQueryDataContainer webPageItem, string propertyName, string usedColumn, object? foundValue, string language) => Task.FromResult(foundValue);

    /// <inheritdoc />
    public virtual Task<LuceneSearchModel> OnIndexingNode(IWebPageContentQueryDataContainer webPageItem, LuceneSearchModel model) => Task.FromResult(model);

    /// <inheritdoc />
    public virtual bool ShouldIndexNode(IWebPageContentQueryDataContainer webPageItem) => true;

    /// <inheritdoc />
    public virtual FacetsConfig? FacetsConfigFactory() => null;

    public virtual void OnDocumentAddField(Document document, IIndexableField field) => document.Add(field);
}
