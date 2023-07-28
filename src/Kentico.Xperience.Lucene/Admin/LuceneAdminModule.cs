using Kentico.Xperience.Admin.Base;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// An administration module which registers client scripts for Lucene.
    /// </summary>
    internal class LuceneAdminModule : AdminModule
    {
        /// <inheritdoc/>
        public LuceneAdminModule()
            : base(nameof(LuceneAdminModule))
        {
        }


        /// <inheritdoc/>
        protected override void OnInit()
        {
            base.OnInit();

            RegisterClientModule("kentico", "xperience-integrations-lucene");
        }
    }
}
