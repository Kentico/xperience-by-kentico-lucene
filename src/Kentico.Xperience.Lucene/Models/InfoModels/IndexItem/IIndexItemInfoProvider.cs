using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Declares members for <see cref="IndexItemInfo"/> management.
    /// </summary>
    public partial interface IIndexItemInfoProvider : IInfoProvider<IndexItemInfo>, IInfoByIdProvider<IndexItemInfo>, IInfoByNameProvider<IndexItemInfo>
    {
    }
}
