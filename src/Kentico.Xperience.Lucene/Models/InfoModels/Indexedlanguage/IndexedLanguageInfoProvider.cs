using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Class providing <see cref="IndexedlanguageInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIndexedlanguageInfoProvider))]
    public partial class IndexedlanguageInfoProvider : AbstractInfoProvider<IndexedlanguageInfo, IndexedlanguageInfoProvider>, IIndexedlanguageInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedlanguageInfoProvider"/> class.
        /// </summary>
        public IndexedlanguageInfoProvider()
            : base(IndexedlanguageInfo.TYPEINFO)
        {
        }
    }
}
