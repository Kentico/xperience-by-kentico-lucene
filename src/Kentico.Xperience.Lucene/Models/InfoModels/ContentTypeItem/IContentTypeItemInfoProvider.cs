using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Declares members for <see cref="ContentTypeItemInfo"/> management.
    /// </summary>
    public partial interface IContenttypeitemInfoProvider : IInfoProvider<ContentTypeItemInfo>, IInfoByIdProvider<ContentTypeItemInfo>, IInfoByNameProvider<ContentTypeItemInfo>
    {
    }
}
