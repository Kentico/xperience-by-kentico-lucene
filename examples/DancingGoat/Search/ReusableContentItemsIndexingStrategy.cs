using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;
using DancingGoat.Search.Services;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

public class ReusableContentItemsIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private readonly IWebPageQueryResultMapper webPageMapper;
    private readonly IContentQueryExecutor queryExecutor;
    private readonly IWebPageUrlRetriever urlRetriever;
    private readonly WebScraperHtmlSanitizer htmlSanitizer;
    private readonly WebCrawlerService webCrawler;

    public const string FACET_DIMENSION = "ContentType";
    public const string INDEXED_WEBSITECHANNEL_NAME = "DancingGoatPages";
    public const string CRAWLER_CONTENT_FIELD_NAME = "Content";

    public ReusableContentItemsIndexingStrategy(
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryExecutor queryExecutor,
        IWebPageUrlRetriever urlRetriever,
        WebScraperHtmlSanitizer htmlSanitizer,
        WebCrawlerService webCrawler
    )
    {
        this.urlRetriever = urlRetriever;
        this.webPageMapper = webPageMapper;
        this.queryExecutor = queryExecutor;
        this.htmlSanitizer = htmlSanitizer;
        this.webCrawler = webCrawler;
    }

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

        var associatedWebPageItem = (await queryExecutor.GetWebPageResult(query, webPageMapper.Map<HomePage>)).First();
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

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FACET_DIMENSION, true);

        return facetConfig;
    }
}
