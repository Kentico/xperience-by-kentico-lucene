using Kentico.Xperience.Admin.Base.Forms;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents the client properties for configuring a Lucene index component.
/// </summary>
public class LuceneIndexConfigurationComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexChannelConfiguration>>
{
    /// <summary>
    /// Possible content type items associated with the Lucene index.
    /// </summary>
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }


    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<LuceneIndexChannel>? PossibleChannels { get; set; }
}
