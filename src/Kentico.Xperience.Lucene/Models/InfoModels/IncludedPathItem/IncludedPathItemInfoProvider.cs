using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Class providing <see cref="IncludedPathItemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIncludedPathItemInfoProvider))]
    public partial class IncludedPathItemInfoProvider : AbstractInfoProvider<IncludedPathItemInfo, IncludedPathItemInfoProvider>, IIncludedPathItemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedPathItemInfoProvider"/> class.
        /// </summary>
        public IncludedPathItemInfoProvider()
            : base(IncludedPathItemInfo.TYPEINFO)
        {
        }
    }
}
