using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;
using DancingGoat.Search.Services;

using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;

using Microsoft.IdentityModel.Tokens;

namespace DancingGoat.Search;

public class AdvancedSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private readonly IWebPageQueryResultMapper webPageMapper;
    private readonly IContentQueryExecutor queryExecutor;
    private readonly WebScraperHtmlSanitizer htmlSanitizer;
    private readonly WebCrawlerService webCrawler;

    public const string FACET_DIMENSION = "ContentType";
    public const string INDEXED_WEBSITECHANNEL_NAME = "DancingGoatPages";
    public const string CRAWLER_CONTENT_FIELD_NAME = "Content";

    public AdvancedSearchIndexingStrategy(
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryExecutor queryExecutor,
        WebScraperHtmlSanitizer htmlSanitizer,
        WebCrawlerService webCrawler
    )
    {
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
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }

        if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            // The implementation of GetPage<T>() is below
            var page = await GetPage<ArticlePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName,
                ArticlePage.CONTENT_TYPE_NAME);

            if (page is null)
            {
                return null;
            }

            sortableTitle = title = page?.ArticleTitle ?? string.Empty;

            string rawContent = await webCrawler.CrawlWebPage(page!);
            content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
        }
        else if (string.Equals(item.ContentTypeName, HomePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await GetPage<HomePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName,
                HomePage.CONTENT_TYPE_NAME);

            if (page is null)
            {
                return null;
            }

            if (page.HomePageBanner.IsNullOrEmpty())
            {
                return null;
            }

            sortableTitle = title = page!.HomePageBanner.First().BannerText;

            string rawContent = await webCrawler.CrawlWebPage(page!);
            content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
        }
        else
        {
            return null;
        }

        document.Add(new TextField(nameof(DancingGoatSearchResultModel.Title), title, Field.Store.YES));
        document.Add(new StringField(SORTABLE_TITLE_FIELD_NAME, sortableTitle, Field.Store.YES));
        document.Add(new TextField(CRAWLER_CONTENT_FIELD_NAME, content, Field.Store.NO));

        return document;
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem)
    {
        var reindexedItems = new List<IIndexEventItemModel>();

        if (string.Equals(changedItem.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(ArticlePage.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .WithLinkedItems(4)

                            // Because the changedItem is a reusable content item, we don't have a website channel name to use here
                            // so we use a hardcoded channel name.
                            //
                            // This will be resolved with an upcoming Xperience by Kentico feature
                            // https://roadmap.kentico.com/c/193-new-api-cross-content-type-querying
                            .ForWebsite(INDEXED_WEBSITECHANNEL_NAME)

                            // Retrieves all ArticlePages that link to the Article through the ArticlePage.ArticlePageArticle field
                            .Linking(nameof(ArticlePage.ArticlePageTeaser), new[] { changedItem.ItemID }))
                .InLanguage(changedItem.LanguageName);

            var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<ArticlePage>);

            foreach (var articlePage in result)
            {
                // This will be a IIndexEventItemModel passed to our MapToLuceneDocumentOrNull method above
                reindexedItems.Add(new IndexEventWebPageItemModel(
                    articlePage.SystemFields.WebPageItemID,
                    articlePage.SystemFields.WebPageItemGUID,
                    changedItem.LanguageName,
                    ArticlePage.CONTENT_TYPE_NAME,
                    articlePage.SystemFields.WebPageItemName,
                    articlePage.SystemFields.ContentItemIsSecured,
                    articlePage.SystemFields.ContentItemContentTypeID,
                    articlePage.SystemFields.ContentItemCommonDataContentLanguageID,
                    INDEXED_WEBSITECHANNEL_NAME,
                    articlePage.SystemFields.WebPageItemTreePath,
                    articlePage.SystemFields.WebPageItemParentID,
                    articlePage.SystemFields.WebPageItemOrder));
            }
        }

        return reindexedItems;
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)
    {
        var reindexedItems = new List<IIndexEventItemModel>();

        if (string.Equals(changedItem.ContentTypeName, Banner.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(HomePage.CONTENT_TYPE_NAME,
                    config =>
                        config
                            .WithLinkedItems(4)

                            // Because the changedItem is a reusable content item, we don't have a website channel name to use here
                            // so we use a hardcoded channel name.
                            //
                            // This will be resolved with an upcoming Xperience by Kentico feature
                            // https://roadmap.kentico.com/c/193-new-api-cross-content-type-querying
                            .ForWebsite(INDEXED_WEBSITECHANNEL_NAME)

                            // Retrieves all ArticlePages that link to the Article through the ArticlePage.ArticlePageArticle field
                            .Linking(nameof(HomePage.HomePageBanner), new[] { changedItem.ItemID }))
                .InLanguage(changedItem.LanguageName);

            var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<HomePage>);

            foreach (var homePage in result)
            {
                // This will be a IIndexEventItemModel passed to our MapToLuceneDocumentOrNull method above
                reindexedItems.Add(new IndexEventWebPageItemModel(
                    homePage.SystemFields.WebPageItemID,
                    homePage.SystemFields.WebPageItemGUID,
                    changedItem.LanguageName,
                    HomePage.CONTENT_TYPE_NAME,
                    homePage.SystemFields.WebPageItemName,
                    homePage.SystemFields.ContentItemIsSecured,
                    homePage.SystemFields.ContentItemContentTypeID,
                    homePage.SystemFields.ContentItemCommonDataContentLanguageID,
                    INDEXED_WEBSITECHANNEL_NAME,
                    homePage.SystemFields.WebPageItemTreePath,
                    homePage.SystemFields.WebPageItemParentID,
                    homePage.SystemFields.WebPageItemOrder));
            }
        }

        return reindexedItems;
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();

        facetConfig.SetMultiValued(FACET_DIMENSION, true);

        return facetConfig;
    }

    private async Task<T?> GetPage<T>(Guid id, string channelName, string languageName, string contentTypeName)
        where T : IWebPageFieldsSource, new()
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(contentTypeName,
                config =>
                    config
                        .WithLinkedItems(4) // You could parameterize this if you want to optimize specific database queries
                        .ForWebsite(channelName)
                        .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemGUID), id))
                        .TopN(1))
            .InLanguage(languageName);

        var result = await queryExecutor.GetWebPageResult(query, webPageMapper.Map<T>);

        return result.FirstOrDefault();
    }
}
