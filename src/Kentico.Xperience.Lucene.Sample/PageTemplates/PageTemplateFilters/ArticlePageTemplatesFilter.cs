using System;
using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

using Kentico.PageBuilder.Web.Mvc.PageTemplates;

namespace DancingGoat.PageTemplates
{
    /// <summary>
    /// Article page template filter.
    /// </summary>
    /// <seealso cref="IPageTemplateFilter" />
    public class ArticlePageTemplatesFilter : IPageTemplateFilter
    {
        /// <summary>
        /// Applies filtering on the given <paramref name="pageTemplates" /> collection based on the given <paramref name="context" />.
        /// </summary>
        /// <returns>
        /// Returns only those page templates that are allowed for article pages if the given context matches the article page type.
        /// </returns>
        public IEnumerable<PageTemplateDefinition> Filter(IEnumerable<PageTemplateDefinition> pageTemplates, PageTemplateFilterContext context)
        {
            if (context.PageType.Equals(Article.CLASS_NAME, StringComparison.InvariantCultureIgnoreCase))
            {
                return pageTemplates.Where(t => GetArticlePageTemplates().Contains(t.Identifier));
            }

            // Exclude all article page templates from the final collection if the context does not match this filter
            return pageTemplates.Where(t => !GetArticlePageTemplates().Contains(t.Identifier));
        }


        /// <summary>
        /// Gets all the page templates that are allowed for the article page type.
        /// </summary>
        public IEnumerable<string> GetArticlePageTemplates() => new string[] { ComponentIdentifiers.ARTICLE_TEMPLATE, ComponentIdentifiers.ARTICLE_WITH_SIDEBAR_TEMPLATE };
    }
}