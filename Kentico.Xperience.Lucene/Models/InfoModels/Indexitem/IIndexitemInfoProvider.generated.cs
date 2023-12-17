using CMS.DataEngine;

namespace CMS
{
    /// <summary>
    /// Declares members for <see cref="IndexitemInfo"/> management.
    /// </summary>
    public partial interface IIndexitemInfoProvider : IInfoProvider<IndexitemInfo>, IInfoByIdProvider<IndexitemInfo>, IInfoByNameProvider<IndexitemInfo>
    {
    }
}