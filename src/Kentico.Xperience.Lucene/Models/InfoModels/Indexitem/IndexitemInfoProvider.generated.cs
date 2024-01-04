using CMS.DataEngine;

namespace CMS
{
    /// <summary>
    /// Class providing <see cref="IndexitemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIndexitemInfoProvider))]
    public partial class IndexitemInfoProvider : AbstractInfoProvider<IndexitemInfo, IndexitemInfoProvider>, IIndexitemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexitemInfoProvider"/> class.
        /// </summary>
        public IndexitemInfoProvider()
            : base(IndexitemInfo.TYPEINFO)
        {
        }
    }
}