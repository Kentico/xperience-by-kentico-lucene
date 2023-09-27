using CMS.Websites;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Index;

namespace Kentico.Xperience.Lucene.Services;

public interface ILuceneIndexingStrategy
{
    /// <summary>
    /// Called when indexing a search model property. Does not trigger when indexing the
    /// properties specified by <see cref="LuceneSearchModel"/> base class.
    /// </summary>
    /// <param name="webPageItem">The <see cref="IWebPageFieldsSource"/> currently being indexed.</param>
    /// <param name="propertyName">The search model property that is being indexed.</param>
    /// <param name="usedColumn">The column that the value was retrieved from when the
    /// property uses the <see cref="SourceAttribute"/>. If not used, the parameter will
    /// be null.</param>
    /// <param name="foundValue">The value of the property that was found in the <paramref name="webPageItem"/>,
    /// or null if no value was found.</param>
    /// <returns>The value that will be indexed in Lucene.</returns>
    Task<object?> OnIndexingProperty(IWebPageContentQueryDataContainer webPageItem, string propertyName, string usedColumn, object? foundValue, string language);



    /// <summary>
    /// Called when indexing a search model. Enables overriding of multiple fields with custom data.
    /// </summary>
    /// <param name="webPageItem">The <see cref="IWebPageFieldsSource"/> currently being indexed.</param>
    /// <param name="model">The resulting search data <see cref="LuceneSearchModel"/> to be modified. The model could be changed during the process.</param>
    /// <returns>Modified Lucene document.</returns>
    Task<LuceneSearchModel> OnIndexingNode(IWebPageContentQueryDataContainer webPageItem, LuceneSearchModel model);

    /// <summary>
    /// Called when indexing a search model. Could be used to disable indexing of documents that match the scope, but should not be indexed e.g. error pages.
    /// </summary>
    /// <param name="webPageItem">The <see cref="IWebPageFieldsSource"/> currently being indexed.</param>
    /// <returns>bool</returns>
    bool ShouldIndexNode(IWebPageContentQueryDataContainer webPageItem);

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
