using System;
using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.PageBuilder.Web.Mvc.PageTemplates;

namespace DancingGoat.PageTemplates
{
    /// <summary>
    /// Landing page template filter.
    /// </summary>
    /// <seealso cref="IPageTemplateFilter" />
    public class LandingPageTemplatesFilter : IPageTemplateFilter
    {
        /// <summary>
        /// Applies filtering on the given <paramref name="pageTemplates" /> collection based on the given <paramref name="context" />.
        /// </summary>
        /// <returns>
        /// Returns only those page templates that are allowed for landing pages if the given context matches the landing page type.
        /// </returns>
        public IEnumerable<PageTemplateDefinition> Filter(IEnumerable<PageTemplateDefinition> pageTemplates, PageTemplateFilterContext context)
        {
            if (context.PageType.Equals(LandingPage.CLASS_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                return pageTemplates.Where(t => GetLandingPageTemplates().Contains(t.Identifier));
            }

            // Exclude all landing page templates from the final collection if the context does not match this filter
            return pageTemplates.Where(t => !GetLandingPageTemplates().Contains(t.Identifier));
        }


        /// <summary>
        /// Gets all the page templates that are allowed for the landing page type.
        /// </summary>
        public IEnumerable<string> GetLandingPageTemplates() => new string[] { ComponentIdentifiers.LANDING_PAGE_SINGLE_COLUMN_TEMPLATE };
    }
}