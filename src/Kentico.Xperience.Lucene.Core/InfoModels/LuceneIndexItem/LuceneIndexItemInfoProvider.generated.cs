using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Class providing <see cref="LuceneIndexItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(ILuceneIndexItemInfoProvider))]
public partial class LuceneIndexItemInfoProvider : AbstractInfoProvider<LuceneIndexItemInfo, LuceneIndexItemInfoProvider>, ILuceneIndexItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneIndexItemInfoProvider"/> class.
    /// </summary>
    public LuceneIndexItemInfoProvider()
        : base(LuceneIndexItemInfo.TYPEINFO)
    {
    }
}
