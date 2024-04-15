using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Core;

/// <summary>
/// Declares members for <see cref="LuceneContentTypeItemInfo"/> management.
/// </summary>
public partial interface ILuceneContentTypeItemInfoProvider : IInfoProvider<LuceneContentTypeItemInfo>, IInfoByIdProvider<LuceneContentTypeItemInfo>, IInfoByNameProvider<LuceneContentTypeItemInfo>
{
}
