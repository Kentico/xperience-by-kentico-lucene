using CMS.DataEngine;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// Class providing <see cref="LuceneModuleVersionInfo"/> management.
    /// </summary>
    [ProviderInterface(typeof(ILuceneModuleVersionInfoProvider))]
    public partial class LuceneModuleVersionInfoProvider : AbstractInfoProvider<LuceneModuleVersionInfo, LuceneModuleVersionInfoProvider>, ILuceneModuleVersionInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneModuleVersionInfoProvider"/> class.
        /// </summary>
        public LuceneModuleVersionInfoProvider()
            : base(LuceneModuleVersionInfo.TYPEINFO)
        {
        }
    }
}
