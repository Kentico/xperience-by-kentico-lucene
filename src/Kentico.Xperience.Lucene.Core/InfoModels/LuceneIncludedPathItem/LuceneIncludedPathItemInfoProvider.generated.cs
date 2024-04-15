using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Class providing <see cref="LuceneIncludedPathItemInfo"/> management.
/// </summary>
[ProviderInterface(typeof(ILuceneIncludedPathItemInfoProvider))]
public partial class LuceneIncludedPathItemInfoProvider : AbstractInfoProvider<LuceneIncludedPathItemInfo, LuceneIncludedPathItemInfoProvider>, ILuceneIncludedPathItemInfoProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneIncludedPathItemInfoProvider"/> class.
    /// </summary>
    public LuceneIncludedPathItemInfoProvider()
        : base(LuceneIncludedPathItemInfo.TYPEINFO)
    {
    }
}
