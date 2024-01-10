using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models;

/// <summary>
/// Class providing <see cref="LuceneContentTypeItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(ILuceneContentTypeItemInfoProvider))]
public partial class LuceneContentTypeItemInfoProvider : AbstractInfoProvider<LuceneContentTypeItemInfo, LuceneContentTypeItemInfoProvider>, ILuceneContentTypeItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneContentTypeItemInfoProvider"/> class.
    /// </summary>
    public LuceneContentTypeItemInfoProvider()
        : base(LuceneContentTypeItemInfo.TYPEINFO)
    {
    }
}
