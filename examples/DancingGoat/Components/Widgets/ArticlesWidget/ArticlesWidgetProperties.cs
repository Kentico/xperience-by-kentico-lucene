using System.Collections.Generic;

using CMS.Websites;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Websites.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Articles widget properties.
    /// </summary>
    public class ArticlesWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Articles section page whose articles are displayed.
        /// </summary>
        [WebPageSelectorComponent(MaximumPages = 1, Label = "{$dancinggoat.articleswidget.articlessection.label$}", Order = 1)]
        public IEnumerable<WebPageRelatedItem> ArticlesSection { get; set; } = new List<WebPageRelatedItem>();
    }
}
