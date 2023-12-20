using CMS.DataEngine;

namespace CMS
{
    /// <summary>
    /// Declares members for <see cref="ContenttypeitemInfo"/> management.
    /// </summary>
    public partial interface IContenttypeitemInfoProvider : IInfoProvider<ContenttypeitemInfo>, IInfoByIdProvider<ContenttypeitemInfo>, IInfoByNameProvider<ContenttypeitemInfo>
    {
    }
}