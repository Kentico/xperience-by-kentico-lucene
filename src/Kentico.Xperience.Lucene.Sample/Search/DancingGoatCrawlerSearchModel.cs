using CMS.Core;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Documents;

namespace DancingGoat.Search;

[IncludedPath("/%", ContentTypes = new string[] {
    AboutUs.CLASS_NAME,
    Article.CLASS_NAME,
    CafeSection.CLASS_NAME,
    Coffee.CLASS_NAME,
    Contacts.CLASS_NAME,
    Home.CLASS_NAME,
})]
public class DancingGoatCrawlerSearchModel : LuceneSearchModel
{
    public const string IndexName = "DancingGoatCrawler";

    [TextField(true)]
    //[ Source(new string[] { nameof(NewsPage.Title), nameof(TreeNode.DocumentName) })]
    [Source(new string[] { nameof(TreeNode.DocumentName) })]
    public string Title { get; set; }

    [TextField(false)]
    public string CrawlerContent { get; set; }

}

public class DancingGoatCrawlerLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override async Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
    {
        object result = foundValue;
        if (propertyName == nameof(DancingGoatCrawlerSearchModel.CrawlerContent))
        {
            var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();
            var webCrawler = Service.Resolve<WebCrawlerService>();

            string content = await webCrawler.CrawlNode(node);
            result = htmlSanitizer.SanitizeHtmlDocument(content);
        }

        return result;
    }
}
