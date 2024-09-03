using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat.Models;

using Kentico.Xperience.Lucene.Core.Indexing;

using Lucene.Net.Documents;

using Microsoft.IdentityModel.Tokens;

namespace DancingGoat.Search;

public class SimpleSearchIndexingStrategy : DefaultLuceneIndexingStrategy
{
    private readonly IWebPageQueryResultMapper webPageMapper;
    private readonly IContentQueryExecutor queryExecutor;

    public SimpleSearchIndexingStrategy(
        IWebPageQueryResultMapper webPageMapper,
        IContentQueryExecutor queryExecutor
    )
    {
        this.webPageMapper = webPageMapper;
        this.queryExecutor = queryExecutor;
    }

    public override async Task<Document?> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        var document = new Document();

        string title = string.Empty;

        // IIndexEventItemModel could be a reusable content item or a web page item, so we use
        // pattern matching to get access to the web page item specific type and fields
        if (item is not IndexEventWebPageItemModel indexedPage)
        {
            return null;
        }
        if (string.Equals(item.ContentTypeName, HomePage.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
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

            title = page!.HomePageBanner.First().BannerHeaderText;
        }
        else
        {
            return null;
        }

        document.Add(new TextField(nameof(DancingGoatSearchResultModel.Title), title, Field.Store.YES));

        return document;
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
