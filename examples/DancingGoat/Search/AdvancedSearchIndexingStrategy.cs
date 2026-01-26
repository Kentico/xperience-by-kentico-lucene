using CMS.Websites;

using DancingGoat.Models;
using DancingGoat.Search.Services;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

public class AdvancedSearchIndexingStrategy(
    IContentRetriever contentRetriever,
    WebScraperHtmlSanitizer htmlSanitizer,
    WebCrawlerService webCrawler
    ) : DefaultLuceneIndexingStrategy
{
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private readonly IContentRetriever contentRetriever = contentRetriever;
    private readonly WebScraperHtmlSanitizer htmlSanitizer = htmlSanitizer;
    private readonly WebCrawlerService webCrawler = webCrawler;

    public const string FACET_DIMENSION = "ContentType";
    public const string INDEXED_WEBSITECHANNEL_NAME = "DancingGoatPages";
    public const string CRAWLER_CONTENT_FIELD_NAME = "Content";

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        var document = new Document();

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }

        string sortableTitle;
        string title;
        string content;
        string facetValue;

        if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            // The implementation of GetPage<T>() is below
            var page = await GetPage<ArticlePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName);

            if (page is null)
            {
                return null;
            }

            sortableTitle = title = page.ArticleTitle ?? string.Empty;

            string rawContent = await webCrawler.CrawlWebPage(page!);
            content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
            facetValue = "Article";
        }
        else if (string.Equals(item.ContentTypeName, HomePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await GetPage<HomePage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName);

            if (page is null)
            {
                return null;
            }

            if (page.HomePageBanner?.Any() != true)
            {
                return null;
            }

            sortableTitle = title = page!.HomePageBanner.First().BannerText;

            string rawContent = await webCrawler.CrawlWebPage(page!);
            content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
            facetValue = "Home";
        }
        else if (string.Equals(item.ContentTypeName, ProductPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await GetPage<ProductPage>(
                indexedPage.ItemGuid,
                indexedPage.WebsiteChannelName,
                indexedPage.LanguageName);

            if (page is null)
            {
                return null;
            }

            if (page.ProductPageProduct.FirstOrDefault() is not IProductFields contentItem)
            {
                return null;
            }

            sortableTitle = title = contentItem.ProductFieldName;
            string rawContent = await webCrawler.CrawlWebPage(page);
            content = htmlSanitizer.SanitizeHtmlDocument(rawContent);
            facetValue = "Product";
        }
        else
        {
            return null;
        }

        document.Add(new FacetField(FACET_DIMENSION, facetValue));
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
            var result = await contentRetriever.RetrievePages<ArticlePage>(
                new RetrievePagesParameters() { LanguageName = changedItem.LanguageName, LinkedItemsMaxLevel = 4, ChannelName = changedItem.WebsiteChannelName, IsForPreview = false },
                q => q.Linking(nameof(ArticlePage.ArticlePageTeaser), [changedItem.ItemID]),
                new RetrievalCacheSettings($"ArticlesLinking|{changedItem.ItemID}"),
                cancellationToken: default);

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
                    changedItem.WebsiteChannelName,
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
            // Note: For deletion events, the changedItem.RelatedItems property is automatically populated
            // with information about items that reference the deleted item. You can use this instead of
            // querying, which would return empty results for deleted items.
            // Example:
            // if (changedItem.RelatedItems.Any())
            // {
            //     foreach (var relatedItem in changedItem.RelatedItems)
            //     {
            //         reindexedItems.Add(new IndexEventWebPageItemModel(...));
            //     }
            //     return reindexedItems;
            // }

            var result = await contentRetriever.RetrievePages<HomePage>(
                new RetrievePagesParameters() { LanguageName = changedItem.LanguageName, LinkedItemsMaxLevel = 4, ChannelName = INDEXED_WEBSITECHANNEL_NAME, IsForPreview = false },
                q => q.Linking(nameof(HomePage.HomePageBanner), [changedItem.ItemID]),
                new RetrievalCacheSettings($"HomePageLinking|{changedItem.ItemID}"),
                cancellationToken: default);

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

        facetConfig.SetMultiValued(FACET_DIMENSION, false);

        return facetConfig;
    }

    private async Task<T?> GetPage<T>(Guid id, string channelName, string languageName)
        where T : IWebPageFieldsSource, new()
    {
        var result = await contentRetriever.RetrievePagesByGuids<T>([id],
                new RetrievePagesParameters() { LanguageName = languageName, LinkedItemsMaxLevel = 4, ChannelName = channelName, IsForPreview = false },
                RetrievePagesQueryParameters.Default,
                new RetrievalCacheSettings($"Page|{id}"),
                cancellationToken: default);

        return result.FirstOrDefault();
    }
}
