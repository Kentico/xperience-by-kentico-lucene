using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Class providing <see cref="LuceneIndexedLanguageInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(ILuceneIndexedLanguageInfoProvider))]
    public partial class LuceneIndexedLanguageInfoProvider : AbstractInfoProvider<LuceneIndexedLanguageInfo, LuceneIndexedLanguageInfoProvider>, ILuceneIndexedLanguageInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndexedLanguageInfoProvider"/> class.
        /// </summary>
        public LuceneIndexedLanguageInfoProvider()
            : base(LuceneIndexedLanguageInfo.TYPEINFO)
        {
        }
    }
}
