using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;
using DancingGoat.Search.Services;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

/// <summary>
/// Provides a custom indexing strategy for reusable content items, enabling their integration into Lucene Search.
/// </summary>
public class ReusableContentItemsIndexingStrategy : DefaultLuceneIndexingStrategy
{
    /// <summary>
    /// Represents the name of the field used for storing a sortable version of a title.
    /// </summary>
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";


    /// <summary>
    /// Represents the facet dimension used to categorize content by type.
    /// </summary>
    public const string FACET_DIMENSION = "ContentType";


    /// <summary>
    /// Represents the name of the indexed <see cref="WebsiteChannelInfo"/> used for content categorization.
    /// </summary>
    public const string INDEXED_WEBSITECHANNEL_NAME = "DancingGoatPages";


    /// <summary>
    /// Represents the name of the field used for storing content crawled from web pages.
    /// </summary>
    public const string CRAWLER_CONTENT_FIELD_NAME = "Content";


    private readonly IContentQueryResultMapper contentQueryResultMapper;


    private readonly IContentQueryExecutor queryExecutor;


    private readonly IWebPageUrlRetriever urlRetriever;


    private readonly WebScraperHtmlSanitizer htmlSanitizer;


    private readonly WebCrawlerService webCrawler;


    /// <summary>
    /// Initializes a new instance of the <see cref="ReusableContentItemsIndexingStrategy"/> class.
    /// </summary>
    /// <param name="contentQueryResultMapper">The <see cref="IContentQueryResultMapper"/></param>
    /// <param name="queryExecutor">The <see cref="IContentQueryExecutor"/></param>
    /// <param name="urlRetriever">The <see cref="IWebPageUrlRetriever"/></param>
    /// <param name="htmlSanitizer">The <see cref="WebScraperHtmlSanitizer"/></param>
    /// <param name="webCrawler">The <see cref="WebCrawlerService"/></param>
    public ReusableContentItemsIndexingStrategy(
        IContentQueryResultMapper contentQueryResultMapper,
        IContentQueryExecutor queryExecutor,
        IWebPageUrlRetriever urlRetriever,
        WebScraperHtmlSanitizer htmlSanitizer,
        WebCrawlerService webCrawler
    )
    {
        this.urlRetriever = urlRetriever;
        this.queryExecutor = queryExecutor;
        this.htmlSanitizer = htmlSanitizer;
        this.webCrawler = webCrawler;
        this.contentQueryResultMapper = contentQueryResultMapper;
    }


    /// <inheritdoc/>
    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        var document = new Document();

        string sortableTitle = string.Empty;
        string title = string.Empty;
        string content = string.Empty;

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is not IndexEventReusableItemModel indexedItem)
        {
            return null;
        }

        if (!string.Equals(item.ContentTypeName, Banner.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var query = new ContentItemQueryBuilder()
        .ForContentType(HomePage.CONTENT_TYPE_NAME,
            config =>
                config
                    .WithLinkedItems(4)

                    // Because the changedItem is a reusable content item, we don't have a website channel name to use here
                    // so we use a hardcoded channel name.
                    .ForWebsite(INDEXED_WEBSITECHANNEL_NAME)

                    // Retrieves all HomePages that link to the Banner through the HomePage.HomePageBanner field
                    .Linking(nameof(HomePage.HomePageBanner), new[] { indexedItem.ItemID }))
        .InLanguage(indexedItem.LanguageName);

        var associatedWebPageItem = (await queryExecutor.GetWebPageResult(query, contentQueryResultMapper.Map<HomePage>)).First();
        string url = string.Empty;
        try
        {
            url = (await urlRetriever.Retrieve(associatedWebPageItem.SystemFields.WebPageItemTreePath,
                INDEXED_WEBSITECHANNEL_NAME, indexedItem.LanguageName)).RelativePath;
        }
        catch (Exception)
        {
            // Retrieve can throw an exception when processing a page update LuceneQueueItem
            // and the page was deleted before the update task has processed. In this case, return no item.
            return null;
        }

        sortableTitle = title = associatedWebPageItem!.HomePageBanner.First().BannerText;
        string rawContent = await webCrawler.CrawlWebPage(associatedWebPageItem!);
        content = htmlSanitizer.SanitizeHtmlDocument(rawContent);

        //If the indexed item is a reusable content item, we need to set the url manually.
        document.Add(new StringField(BaseDocumentProperties.URL, url, Field.Store.YES));
        document.Add(new TextField(nameof(DancingGoatSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));
        document.Add(new TextField(CRAWLER_CONTENT_FIELD_NAME, content, Field.Store.NO));

        return document;
    }


    /// <inheritdoc/>
    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FACET_DIMENSION, true);

        return facetConfig;
    }
}
