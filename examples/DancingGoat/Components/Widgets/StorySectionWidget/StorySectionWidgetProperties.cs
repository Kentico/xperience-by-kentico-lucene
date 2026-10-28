using System.Collections.Generic;

using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Story section widget properties.
    /// </summary>
    public class StorySectionWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Displayed story section.
        /// </summary>
        [ContentItemSelectorComponent(StorySection.CONTENT_TYPE_NAME, Label = "{$dancinggoat.storysectionwidget.storysection.label$}", MaximumItems = 1, Order = 1)]
        public IEnumerable<ContentItemReference> SelectedStorySection { get; set; } = new List<ContentItemReference>();
    }
}
