using CMS.DataEngine;

namespace CMS
{
    /// <summary>
    /// Class providing <see cref="ContenttypeitemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IContenttypeitemInfoProvider))]
    public partial class ContenttypeitemInfoProvider : AbstractInfoProvider<ContenttypeitemInfo, ContenttypeitemInfoProvider>, IContenttypeitemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContenttypeitemInfoProvider"/> class.
        /// </summary>
        public ContenttypeitemInfoProvider()
            : base(ContenttypeitemInfo.TYPEINFO)
        {
        }
    }
}