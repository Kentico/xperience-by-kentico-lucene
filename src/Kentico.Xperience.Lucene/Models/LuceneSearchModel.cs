using Kentico.Xperience.Lucene.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Facet;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// The base class for all Lucene search models. Contains common Lucene
/// fields which should be present in all indexes.
/// </summary>
public class LuceneSearchModel
{
    /// <summary>
    /// The internal Lucene ID of this search record.
    /// </summary>
    [StringField(true)]
    public string? ObjectID
    {
        get;
        set;
    }


    /// <summary>
    /// The name of the Xperience class to which the indexed data belongs.
    /// </summary>
    [StringField(true)]
    public string? ClassName
    {
        get;
        set;
    }


    /// <summary>
    /// The relative live site URL of the indexed page.
    /// </summary>
    [StringField(true)]
    public string? Url
    {
        get;
        set;
    }

    public virtual IEnumerable<FacetField> OnTaxonomyFieldCreation()
    {
        yield break;
    }
}
