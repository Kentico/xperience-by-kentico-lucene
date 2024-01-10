using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Class providing <see cref="IndexItemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIndexItemInfoProvider))]
    public partial class IndexItemInfoProvider : AbstractInfoProvider<IndexItemInfo, IndexItemInfoProvider>, IIndexItemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexItemInfoProvider"/> class.
        /// </summary>
        public IndexItemInfoProvider()
            : base(IndexItemInfo.TYPEINFO)
        {
        }
    }
}
