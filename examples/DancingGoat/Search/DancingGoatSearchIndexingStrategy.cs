#nullable enable

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using AngleSharp.Html.Parser;

using CMS.Websites;

using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Util;

namespace DancingGoat.Search;

public sealed partial class DancingGoatSearchIndexingStrategy(
    IContentRetriever contentRetriever) : DefaultLuceneIndexingStrategy
{
    public const string INDEX_NAME = "DancingGoatGlobal";
    public const string STRATEGY_NAME = "DancingGoatGlobalSearch";
    public const string CONTENT_FIELD_NAME = "Content";
    public const string SORTABLE_TITLE_FIELD_NAME = "SortableTitle";

    /// <summary>
    /// Holds <see cref="SECURED_VALUE"/> for pages that require authentication, <see cref="NOT_SECURED_VALUE"/> otherwise.
    /// Secured pages are indexed only when <c>CMSLuceneSearch:IncludeSecuredItems</c> is enabled, and the search service
    /// filters them out for anonymous visitors.
    /// </summary>
    public const string IS_SECURED_FIELD_NAME = "IsSecured";
    public const string SECURED_VALUE = "true";
    public const string NOT_SECURED_VALUE = "false";

    private const string BLOCK_ELEMENTS_SELECTOR = "address, article, aside, blockquote, br, dd, div, dl, dt, figcaption, figure, footer, h1, h2, h3, h4, h5, h6, header, hr, li, main, nav, ol, p, pre, section, table, td, th, tr, ul";

    private readonly IContentRetriever contentRetriever = contentRetriever;

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }

        string title;
        string content;
        string contentType;
        string url;
        object facetSource;

        if (string.Equals(item.ContentTypeName, ArticlePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await RetrievePage<ArticlePage>(indexedPage);
            if (page is null || page.SEOFieldsNoIndex)
            {
                return null;
            }

            facetSource = page;
            title = page.ArticleTitle ?? string.Empty;
            content = ExtractText(page.ArticlePageSummary, page.ArticlePageText);
            contentType = DancingGoatSearchFacets.ARTICLE_CONTENT_TYPE;
            url = page.GetUrl().RelativePath;
        }
        else if (string.Equals(item.ContentTypeName, ProductPage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            var page = await RetrievePage<ProductPage>(indexedPage, linkedItemsMaxLevel: 1);
            if (page is null || page.SEOFieldsNoIndex || page.ProductPageProduct?.FirstOrDefault() is not IProductFields productFields)
            {
                return null;
            }

            facetSource = productFields;
            title = productFields.ProductFieldName ?? string.Empty;
            content = ExtractText(productFields.ProductFieldDescription);
            contentType = DancingGoatSearchFacets.PRODUCT_CONTENT_TYPE;
            url = page.GetUrl().RelativePath;
        }
        else
        {
            return null;
        }

        var document = new Document
        {
            new FacetField(DancingGoatSearchFacets.CONTENT_TYPE_DIMENSION, contentType),
            new TextField(nameof(DancingGoatSearchResultModel.Title), title, Field.Store.YES),
            new SortedDocValuesField(SORTABLE_TITLE_FIELD_NAME, new BytesRef(GetSortableTitle(title))),
            new StringField(nameof(DancingGoatSearchResultModel.Url), url, Field.Store.YES),
            new StringField(nameof(DancingGoatSearchResultModel.ContentType), contentType, Field.Store.YES),
            new TextField(CONTENT_FIELD_NAME, content, Field.Store.YES),
            new StringField(IS_SECURED_FIELD_NAME, item.IsSecured ? SECURED_VALUE : NOT_SECURED_VALUE, Field.Store.YES)
        };

        AddTaxonomyFacets(document, facetSource);

        return document;
    }

    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventReusableItemModel changedItem)
    {
        var pages = await contentRetriever.RetrievePages<ProductPage>(
            new RetrievePagesParameters
            {
                ChannelName = DancingGoatConstants.WEBSITE_CHANNEL_NAME,
                LanguageName = changedItem.LanguageName,
                IsForPreview = false,
                UseLanguageFallbacks = false,
                IncludeSecuredItems = true
            },
            query => query.Linking(nameof(ProductPage.ProductPageProduct), [changedItem.ItemID]),
            RetrievalCacheSettings.CacheDisabled,
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
        facetsConfig.SetMultiValued(DancingGoatSearchFacets.CONTENT_TYPE_DIMENSION, false);
        foreach (var definition in DancingGoatSearchFacets.TAXONOMY_FACETS)
        {
            facetsConfig.SetMultiValued(definition.Dimension, true);
        }

        return facetsConfig;
    }

    private static void AddTaxonomyFacets(Document document, object facetSource)
    {
        foreach (var definition in DancingGoatSearchFacets.TAXONOMY_FACETS)
        {
            var tags = definition.GetTags(facetSource) ?? [];
            foreach (var tag in tags.DistinctBy(tag => tag.Identifier))
            {
                document.Add(new FacetField(definition.Dimension, tag.Identifier.ToString("D")));
            }
        }
    }

    /// <summary>
    /// Returns a case-insensitive, accent-folded key so that titles such as "Él" and "el" sort next to each other.
    /// </summary>
    private static string GetSortableTitle(string title)
    {
        var decomposedTitle = title.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposedTitle.Length);
        foreach (char character in decomposedTitle)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
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
                UseLanguageFallbacks = false,
                IncludeSecuredItems = true,
                LinkedItemsMaxLevel = linkedItemsMaxLevel
            },
            null,
            RetrievalCacheSettings.CacheDisabled,
            cancellationToken: default);

        return pages.FirstOrDefault();
    }

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
