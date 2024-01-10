using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Declares members for <see cref="LuceneIndexItemInfo"/> management.
    /// </summary>
    public partial interface ILuceneIndexItemInfoProvider : IInfoProvider<LuceneIndexItemInfo>, IInfoByIdProvider<LuceneIndexItemInfo>, IInfoByNameProvider<LuceneIndexItemInfo>
    {
    }
}
