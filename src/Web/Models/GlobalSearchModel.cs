using CMS.Websites;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;

namespace DancingGoat.Models;

[IncludedPath("/%", ContentTypes = new string[] {  nameof( ArticlePage.CONTENT_TYPE_NAME ) })]
public class GlobalSearchModel : LuceneSearchModel
{
    public const string IndexName = "Global";

    [TextField(true)]
    [Source(new string[] { nameof(IWebPageFieldsSource.SystemFields.ContentItemName) })]
    public string ArticleTitle { get; set; }
}

public class GlobalSearchModelIndexingStrategy : DefaultLuceneIndexingStrategy
{ 
    
}
