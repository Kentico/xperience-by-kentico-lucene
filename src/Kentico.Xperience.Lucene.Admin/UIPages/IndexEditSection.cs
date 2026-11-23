using CMS.DataEngine;
using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: PageParameterConstants.PARAMETERIZED_SLUG,
   uiPageType: typeof(IndexEditSection),
   name: "Index",
   templateName: TemplateNames.SECTION_LAYOUT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

/// <summary>
/// Section holding the tabs of the index inspector.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class IndexEditSection : EditSectionPage<LuceneIndexItemInfo>
{
    protected override Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is LuceneIndexItemInfo index)
        {
            return Task.FromResult(string.IsNullOrWhiteSpace(index.LuceneIndexItemDisplayName)
                ? index.LuceneIndexItemIndexName
                : index.LuceneIndexItemDisplayName);
        }

        return base.GetObjectDisplayName(infoObject);
    }
}
