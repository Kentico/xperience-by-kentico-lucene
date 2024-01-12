using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Class providing <see cref="LuceneIndexLanguageItemInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(ILuceneIndexLanguageItemInfoProvider))]
    public partial class LuceneIndexedLanguageInfoProvider : AbstractInfoProvider<LuceneIndexLanguageItemInfo, LuceneIndexedLanguageInfoProvider>, ILuceneIndexLanguageItemInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndexedLanguageInfoProvider"/> class.
        /// </summary>
        public LuceneIndexedLanguageInfoProvider()
            : base(LuceneIndexLanguageItemInfo.TYPEINFO)
        {
        }
    }
}
