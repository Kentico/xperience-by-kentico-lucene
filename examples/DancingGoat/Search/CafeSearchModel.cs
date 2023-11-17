using CMS.DocumentEngine;
using CMS.DocumentEngine.Types.DancingGoatCore;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Facet;

namespace DancingGoat.Search;

[IncludedPath("/%", ContentTypes = new string[] { Cafe.CLASS_NAME })]
public class CafeSearchModel: LuceneSearchModel
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

    public override IEnumerable<FacetField> OnTaxonomyFieldCreation()
    {
        string[] countries = CafeCountry?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(country => country.Trim()).ToArray() ?? Array.Empty<string>();
        yield return countries switch
        {
            { Length: >= 2 } => new FacetField("Country", countries[0], countries[1]),
            { Length: 1 } => new FacetField("Country", countries[0], "no state"),
            _ => new FacetField("Country", "no country", "no state")
        };
    }
}


public class CafeLuceneIndexingStrategy : DefaultLuceneIndexingStrategy
{
    public override FacetsConfig FacetsConfigFactory()
    {
        var facetConfig = new FacetsConfig();
        facetConfig.SetHierarchical("Country", true);
        return facetConfig;
    }
}
