using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using System.Threading.Tasks;

namespace DancingGoat.Models;

[IncludedPath("/%", ContentTypes = new string[] { ArticlePage.CONTENT_TYPE_NAME })]
public class GlobalSearchModel : LuceneSearchModel
{
    public const string IndexName = "Global";

    [TextField(true)]
    [Source(new string[] { nameof(IWebPageFieldsSource.SystemFields.ContentItemName) })]
    public string Title { get; set; }

    [TextField(true)]
    public string CrawlerContent { get; set; }
}

public class GlobalSearchModelIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<object?> OnIndexingProperty(IWebPageContentQueryDataContainer webPageItem, string propertyName, string usedColumn, object? foundValue, string language)
    {
        object result = foundValue;

        if (propertyName == nameof(GlobalSearchModel.CrawlerContent))
        {
            var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
            var webCrawler = Service.Resolve<WebCrawlerService>();
            string content = await webCrawler.CrawlNode(webPageItem, language);
            return htmlSanitizer.SanitizeHtmlDocument(content);
        }

        return result;
    }

    public override bool ShouldIndexNode(IWebPageContentQueryDataContainer webPageItem)
    {
        return true;
    }
}
