using CMS.Core;
using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Documents;
using Lucene.Net.Index;

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

    // pick this date as some meaningful start date, if range of dates is too broad (for example historic dates, change decay algorithm instead)
    private static readonly DateTime decayStartDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

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

    public override void OnDocumentAddField(Document document, IIndexableField field)
    {
        if (field.Name == nameof(DancingGoatSearchModel.PublishedDateTicks) && field.GetInt64Value() is { } unixTimestampMs)
        {
            var dt = new DateTime(DateTools.UnixTimeMillisecondsToTicks(unixTimestampMs), DateTimeKind.Unspecified);

            // difference from first meaningful date in searched data history
            var delta = dt.Subtract(new DateTime(decayStartDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

            // decay defined as years from decay start date
            float decay = (float)delta.TotalDays / 365f;

            // boosting by in particular year by term occurence 
            string value = string.Join(" ", Enumerable.Range(1, dt.Month).Select(x => 'q'));
            var decayField = new TextField("$decay", value, Field.Store.NO)
            {
                Boost = decay
            };

            // to avoid showing irrelevant search results, field boosting can be done on existing fields (in that way no additional field is need in index and query) 

            document.Add(decayField);
        }

        base.OnDocumentAddField(document, field);
    }
}
