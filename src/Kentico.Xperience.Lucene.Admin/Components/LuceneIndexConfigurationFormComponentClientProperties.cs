using Kentico.Xperience.Admin.Base.Forms;

using Kentico.Xperience.Lucene.Core.Indexing;

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Represents the client properties for configuring a Lucene index component.
/// </summary>
internal sealed class LuceneIndexConfigurationFormComponentClientProperties : FormComponentClientProperties<IEnumerable<LuceneIndexChannelConfiguration>>
{
    /// <summary>
    /// Possible content type items associated with the Lucene index.
    /// </summary>
    public IEnumerable<LuceneIndexContentType>? PossibleContentTypeItems { get; set; }


    /// <summary>
    /// The possible website channels in the system that can be indexed.
    /// </summary>
    public IEnumerable<LuceneIndexChannel>? PossibleChannels { get; set; }
}
