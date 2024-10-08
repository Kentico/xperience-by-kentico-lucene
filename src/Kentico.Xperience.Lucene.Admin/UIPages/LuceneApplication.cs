using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Lucene.Admin;

[assembly: UIApplication(
    identifier: LuceneApplicationPage.IDENTIFIER,
    type: typeof(LuceneApplicationPage),
    slug: "lucene",
    name: "Lucene Search",
    category: BaseApplicationCategories.DEVELOPMENT,
    icon: Icons.Magnifier,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// The root application page for the Lucene integration.
/// </summary>
[UIPermission(SystemPermissions.VIEW)]
[UIPermission(SystemPermissions.CREATE)]
[UIPermission(SystemPermissions.UPDATE)]
[UIPermission(SystemPermissions.DELETE)]
[UIPermission(LuceneIndexPermissions.REBUILD, "Rebuild")]
internal class LuceneApplicationPage : ApplicationPage
{
    public const string IDENTIFIER = "Kentico.Xperience.Integrations.Lucene.Admin";
}
