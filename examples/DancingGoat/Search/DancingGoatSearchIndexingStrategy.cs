#nullable enable

using System.Text.RegularExpressions;

using AngleSharp.Html.Parser;

using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

public sealed partial class DancingGoatSearchIndexingStrategy(
    IContentRetriever contentRetriever) : DefaultLuceneIndexingStrategy
{
    public const string FACET_DIMENSION = "ContentType";
    public const string PRODUCT_CATEGORY_FACET_DIMENSION = "ProductCategory";
    public const string PRODUCT_TAG_FACET_DIMENSION = "ProductTag";
    public const string PRODUCT_MANUFACTURER_FACET_DIMENSION = "ProductManufacturer";
    public const string COFFEE_PROCESSING_FACET_DIMENSION = "CoffeeProcessing";
    public const string COFFEE_TASTE_FACET_DIMENSION = "CoffeeTaste";
    public const string CONTENT_FIELD_NAME = "Content";
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel indexedPage || item.IsSecured)
        {
            return null;
        }

        string title;
        string content;
        string facetValue;
        string url;
        IProductFields? product = null;

        if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await RetrievePage<ArticlePage>(indexedPage);
            if (page is null || page.SEOFieldsNoIndex)
            {
                return null;
            }

            title = page.ArticleTitle ?? string.Empty;
            content = ExtractText(page.ArticlePageSummary, page.ArticlePageText);
            facetValue = "Article";
            url = page.GetUrl().RelativePath;
        }
        else if (string.Equals(item.ContentTypeName, ProductPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await RetrievePage<ProductPage>(indexedPage, linkedItemsMaxLevel: 1);
            if (page is null || page.SEOFieldsNoIndex || page.ProductPageProduct?.FirstOrDefault() is not IProductFields productFields)
            {
                return null;
            }

            product = productFields;
            title = productFields.ProductFieldName ?? string.Empty;
            content = ExtractText(productFields.ProductFieldDescription);
            facetValue = "Product";
            url = page.GetUrl().RelativePath;
        }
        else
        {
            return null;
        }

        var document = new Document
        {
            new FacetField(FACET_DIMENSION, facetValue),
            new TextField(nameof(DancingGoatSearchResultModel.Title), title, Field.Store.YES),
            new StringField(SORTABLE_TITLE_FIELD_NAME, title, Field.Store.YES),
            new StringField(nameof(DancingGoatSearchResultModel.Url), url, Field.Store.YES),
            new StringField(nameof(DancingGoatSearchResultModel.ContentType), facetValue, Field.Store.YES),
            new TextField(CONTENT_FIELD_NAME, content, Field.Store.YES),
            new StoredField(nameof(DancingGoatSearchResultModel.Snippet), content[..Math.Min(content.Length, 240)])
        };

        if (product is not null)
        {
            AddProductTaxonomyFacets(document, product);
        }

        return document;
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)
    {
        var pages = await contentRetriever.RetrievePages<ProductPage>(
            new RetrievePagesParameters
            {
                ChannelName = DancingGoatConstants.WEBSITE_CHANNEL_NAME,
                LanguageName = changedItem.LanguageName,
                IsForPreview = false
            },
            query => query.Linking(nameof(ProductPage.ProductPageProduct), [changedItem.ItemID]),
            new RetrievalCacheSettings($"ProductPagesLinking|{changedItem.ItemID}"),
            cancellationToken: default);

        return pages.Select(page => new IndexEventWebPageItemModel(
            page.SystemFields.WebPageItemID,
            page.SystemFields.WebPageItemGUID,
            changedItem.LanguageName,
            ProductPage.CONTENT_TYPE_NAME,
            page.SystemFields.WebPageItemName,
            page.SystemFields.ContentItemIsSecured,
            page.SystemFields.ContentItemContentTypeID,
            page.SystemFields.ContentItemCommonDataContentLanguageID,
            DancingGoatConstants.WEBSITE_CHANNEL_NAME,
            page.SystemFields.WebPageItemTreePath,
            page.SystemFields.WebPageItemOrder,
            page.SystemFields.WebPageItemParentID));
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var facetsConfig = new FacetsConfig();
        facetsConfig.SetMultiValued(FACET_DIMENSION, false);
        facetsConfig.SetMultiValued(PRODUCT_CATEGORY_FACET_DIMENSION, true);
        facetsConfig.SetMultiValued(PRODUCT_TAG_FACET_DIMENSION, true);
        facetsConfig.SetMultiValued(PRODUCT_MANUFACTURER_FACET_DIMENSION, true);
        facetsConfig.SetMultiValued(COFFEE_PROCESSING_FACET_DIMENSION, true);
        facetsConfig.SetMultiValued(COFFEE_TASTE_FACET_DIMENSION, true);
        return facetsConfig;
    }

    private static void AddProductTaxonomyFacets(Document document, IProductFields product)
    {
        var productFacets = new Dictionary<string, IEnumerable<TagReference>>
        {
            [PRODUCT_CATEGORY_FACET_DIMENSION] = product.ProductFieldCategory ?? [],
            [PRODUCT_TAG_FACET_DIMENSION] = product.ProductFieldTags ?? []
        };

        if (product is IProductManufacturer manufacturer)
        {
            productFacets[PRODUCT_MANUFACTURER_FACET_DIMENSION] = manufacturer.ProductManufacturerTag ?? [];
        }

        if (product is ProductCoffee coffee)
        {
            productFacets[COFFEE_PROCESSING_FACET_DIMENSION] = coffee.CoffeeProcessing ?? [];
            productFacets[COFFEE_TASTE_FACET_DIMENSION] = coffee.CoffeeTastes ?? [];
        }

        foreach (var (dimension, facetTags) in productFacets)
        {
            foreach (var reference in facetTags.DistinctBy(tag => tag.Identifier))
            {
                document.Add(new FacetField(dimension, reference.Identifier.ToString("D")));
            }
        }
    }

    private async Task<T?> RetrievePage<T>(IndexEventWebPageItemModel indexedPage, int linkedItemsMaxLevel = 0)
        where T : IWebPageFieldsSource, new()
    {
        var pages = await contentRetriever.RetrievePagesByGuids<T>(
            [indexedPage.ItemGuid],
            new RetrievePagesParameters
            {
                ChannelName = indexedPage.WebsiteChannelName,
                LanguageName = indexedPage.LanguageName,
                IsForPreview = false,
                LinkedItemsMaxLevel = linkedItemsMaxLevel
            },
            cancellationToken: default);

        return pages.FirstOrDefault();
    }

    private const string BLOCK_ELEMENTS_SELECTOR = "address, article, aside, blockquote, br, dd, div, dl, dt, figcaption, figure, footer, h1, h2, h3, h4, h5, h6, header, hr, li, main, nav, ol, p, pre, section, table, td, th, tr, ul";

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    private static string ExtractText(params string?[] values)
    {
        var parser = new HtmlParser();
        var textValues = new List<string>();
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var body = parser.ParseDocument(value).Body;
            if (body is null)
            {
                continue;
            }

            foreach (var element in body.QuerySelectorAll("script, style, [data-ktc-search-exclude]"))
            {
                element.Remove();
            }

            foreach (var element in body.QuerySelectorAll(BLOCK_ELEMENTS_SELECTOR))
            {
                element.Parent?.InsertBefore(body.Owner!.CreateTextNode(" "), element.NextSibling);
            }

            if (!string.IsNullOrWhiteSpace(body.TextContent))
            {
                textValues.Add(body.TextContent);
            }
        }

        return WhitespaceRegex().Replace(string.Join(" ", textValues), " ").Trim();
    }
}
