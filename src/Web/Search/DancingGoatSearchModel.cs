using CMS.Core;
using CMS.Websites;
using DancingGoat.Models;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;

namespace DancingGoat.Search;

[IncludedPath("/%", ContentTypes = new string[] { ArticlePage.CONTENT_TYPE_NAME })]
public class DancingGoatSearchModel : LuceneSearchModel
{
    public const string IndexName = "DancingGoatSearch";

    [TextField(true)]
    [Source(new string[] { nameof(IWebPageFieldsSource.SystemFields.ContentItemName) })]
    public string Title { get; set; }

    [TextField(true)]
    public string CrawlerContent { get; set; }
}

public class GlobalSearchModelIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<object?> OnIndexingProperty(IWebPageContentQueryDataContainer pageContentContainer, string propertyName, string usedColumn, object? foundValue, string language)
    {
        object result = foundValue;

        if (propertyName == nameof(DancingGoatSearchModel.CrawlerContent))
        {
            var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
            var webCrawler = Service.Resolve<WebCrawlerService>();
            string content = await webCrawler.CrawlNode(pageContentContainer, language);
            return htmlSanitizer.SanitizeHtmlDocument(content);
        }

        return result;
    }

    public override bool ShouldIndexNode(IWebPageContentQueryDataContainer webPageItem)
    {
        return true;
    }
}
