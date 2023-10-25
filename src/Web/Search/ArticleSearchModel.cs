using DancingGoat.Models;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

[IncludedPath("/%", ContentTypes = new string[] { ArticlePage.CONTENT_TYPE_NAME })]
public class ArticleSearchModel : LuceneSearchModel
{
    public const string IndexName = "CafeIndex";
    
    [TextField(true)]
    [Source(new string[] { nameof(TreeNode.DocumentName) })]
    public string Title { get; set; }

    [TextField(true)]
    public string CafeCountry { get; set; }
    
    [TextField(true)]
    public string CafeCity { get; set; }
    
    [TextField(true)]
    public string CafeZipCode { get; set; }
}


public class ArticleLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();
        facetConfig.SetHierarchical("Country", true);
        return facetConfig;
    }
}
