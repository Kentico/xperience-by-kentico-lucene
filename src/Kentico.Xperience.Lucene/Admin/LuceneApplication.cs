using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.UIPages;
using Kentico.Xperience.Lucene.Admin;

[assembly: UIApplication(
    identifier: LuceneApplication.IDENTIFIER,
    type: typeof(LuceneApplication),
    slug: "lucene",
    name: "Search",
    category: BaseApplicationCategories.DEVELOPMENT,
    icon: Icons.Magnifier,
    templateName: TemplateNames.SECTION_LAYOUT)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// The root application page for the Lucene integration.
/// </summary>
[UIPermission(SystemPermissions.VIEW)]
internal class LuceneApplication : ApplicationPage
{
    public const string IDENTIFIER = "Kentico.Xperience.Integrations.Lucene";
}
