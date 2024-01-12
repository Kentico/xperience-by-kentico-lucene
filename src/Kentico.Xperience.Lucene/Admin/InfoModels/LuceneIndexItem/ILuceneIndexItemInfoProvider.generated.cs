using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Declares members for <see cref="LuceneIndexItemInfo"/> management.
    /// </summary>
    public partial interface ILuceneIndexItemInfoProvider : IInfoProvider<LuceneIndexItemInfo>, IInfoByIdProvider<LuceneIndexItemInfo>, IInfoByNameProvider<LuceneIndexItemInfo>
    {
    }
}
