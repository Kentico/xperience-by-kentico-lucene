using System;
using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.PageBuilder.Web.Mvc.PageTemplates;

namespace DancingGoat.PageTemplates
{
    /// <summary>
    /// Subscription page template filter.
    /// </summary>
    /// <seealso cref="IPageTemplateFilter" />
    public class SubscriptionPageTemplatesFilter : IPageTemplateFilter
    {
        /// <summary>
        /// Applies filtering on the given <paramref name="pageTemplates" /> collection based on the given <paramref name="context" />.
        /// </summary>
        /// <returns>
        /// Returns only those page templates that are allowed for subscription pages if the given context matches the subscription page type.
        /// </returns>
        public IEnumerable<PageTemplateDefinition> Filter(IEnumerable<PageTemplateDefinition> pageTemplates, PageTemplateFilterContext context)
        {
            if (context.PageType.Equals(Subscription.CLASS_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                return pageTemplates.Where(t => GetSubscriptionPageTemplates().Contains(t.Identifier));
            }

            // Exclude all subscription page templates from the final collection if the context does not match this filter
            return pageTemplates.Where(t => !GetSubscriptionPageTemplates().Contains(t.Identifier));
        }


        /// <summary>
        /// Gets all the page templates that are allowed for the subscription page type.
        /// </summary>
        public IEnumerable<string> GetSubscriptionPageTemplates() => new[] { ComponentIdentifiers.SUBSCRIPTION_TEMPLATE };
    }
}