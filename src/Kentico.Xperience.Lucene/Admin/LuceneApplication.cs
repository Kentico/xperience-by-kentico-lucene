using CMS.Membership;
using Kentico.Xperience.Admin.Base;

namespace Kentico.Xperience.Lucene.Admin
{
    /// <summary>
    /// The root application page for the Lucene integration.
    /// </summary>
    [UIPermission(SystemPermissions.VIEW)]
    internal class LuceneApplication : ApplicationPage
    {
        public const string IDENTIFIER = "Kentico.Xperience.Integrations.Lucene";
    }
}
