using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Class providing <see cref="LuceneReusableContentTypeItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(ILuceneReusableContentTypeItemInfoProvider))]
public partial class LuceneReusableContentTypeItemInfoProvider : AbstractInfoProvider<LuceneReusableContentTypeItemInfo, LuceneReusableContentTypeItemInfoProvider>, ILuceneReusableContentTypeItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneReusableContentTypeItemInfoProvider"/> class.
    /// </summary>
    public LuceneReusableContentTypeItemInfoProvider()
        : base(LuceneReusableContentTypeItemInfo.TYPEINFO)
    {
    }
}
