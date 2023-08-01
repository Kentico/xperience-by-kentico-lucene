using CMS.Core;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Documents;

namespace DancingGoat.Search;

[IncludedPath("/%", ContentTypes = new string[] { Article.CLASS_NAME })]
public class DancingGoatSearchModel : LuceneSearchModel
{
    public const string IndexName = "DancingGoat";

    [TextField(true)]
    //[ Source(new string[] { nameof(NewsPage.Title), nameof(TreeNode.DocumentName) })]
    [Source(new string[] { nameof(TreeNode.DocumentName) })]
    public string Title { get; set; }

    [TextField(true)]
    public string ArticleSummary { get; set; }

    [TextField(false)]
    public string ArticleText { get; set; }

    [TextField(false)]
    public string AllContent { get; set; }

    [StringField(true)]
    public string ImagePath { get; set; }

    [Int64Field(true)]
    public long PublishedDateTicks { get; set; }

    public DateTime PublishedDate =>
        new(DateTools.UnixTimeMillisecondsToTicks(PublishedDateTicks), DateTimeKind.Utc);
}

public class DancingGoatLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
{
    private static readonly string[] contentFields = new string[]
    {
        nameof(Article.DocumentName),
        nameof(Article.ArticleSummary),
        nameof(Article.ArticleText),
    };

    public override Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
    {
        object result = foundValue;
        if (propertyName == nameof(DancingGoatSearchModel.AllContent))
        {
            var htmlSanitizer = Service.Resolve<WebScraperHtmlSanitizer>();

            result = string.Join(" ", contentFields
                .Select(f => node.GetStringValue(f, ""))
                .Select(s => htmlSanitizer.SanitizeHtmlFragment(s)));
        }

        if (string.Equals(propertyName, nameof(DancingGoatSearchModel.ImagePath)) && node is Article article)
        {
            result = GetMediaURL(article);
        }

        if (string.Equals(propertyName, nameof(DancingGoatSearchModel.PublishedDateTicks)))
        {
            var date = node.PublishedVersionExists && node.DocumentLastPublished != DateTime.MinValue
                ? node.DocumentLastPublished
                : node.DocumentModifiedWhen != DateTime.MinValue
                ? node.DocumentModifiedWhen
                : node.DocumentCreatedWhen;
            result = DateTools.TicksToUnixTimeMilliseconds(date.Ticks);
        }

        return Task.FromResult(result);
    }

    private static string GetMediaURL(Article article)
    {
        var media = article.Fields.Teaser.OfType<Media>().FirstOrDefault();

        if (media is null)
        {
            return "";
        }

        var url = media.Fields.File.Url.AsSpan();

        return url.StartsWith("~")
            ? url[1..].ToString()
            : url.ToString();
    }
}
