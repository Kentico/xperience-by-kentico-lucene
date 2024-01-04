using CMS.DataEngine;

namespace CMS
{
    /// <summary>
    /// Class providing <see cref="IncludedpathitemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIncludedpathitemInfoProvider))]
    public partial class IncludedpathitemInfoProvider : AbstractInfoProvider<IncludedpathitemInfo, IncludedpathitemInfoProvider>, IIncludedpathitemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedpathitemInfoProvider"/> class.
        /// </summary>
        public IncludedpathitemInfoProvider()
            : base(IncludedpathitemInfo.TYPEINFO)
        {
        }
    }
}