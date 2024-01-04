using System.Collections.Generic;

using CMS.DocumentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Card widget properties.
    /// </summary>
    public class CardWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Image to be displayed.
        /// </summary>
        [ContentItemSelectorComponent("DancingGoatCore.Media", Label = "Image", Order = 1)]
        public IEnumerable<LinkedContentItem> Image { get; set; } = new List<LinkedContentItem>();

        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string Text { get; set; }
    }
}