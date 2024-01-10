using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Class providing <see cref="IndexedLanguageInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(IIndexedLanguageInfoProvider))]
    public partial class IndexedLanguageInfoProvider : AbstractInfoProvider<IndexedLanguageInfo, IndexedLanguageInfoProvider>, IIndexedLanguageInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedLanguageInfoProvider"/> class.
        /// </summary>
        public IndexedLanguageInfoProvider()
            : base(IndexedLanguageInfo.TYPEINFO)
        {
        }
    }
}
