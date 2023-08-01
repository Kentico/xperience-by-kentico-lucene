using System.Collections.Generic;

using CMS.DocumentEngine;

using Kentico.Forms.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Hero image widget properties.
    /// </summary>
    public class HeroImageWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Background image.
        /// </summary>
        [ContentItemSelectorComponent("DancingGoatCore.Media", Label = "Background image", Order = 1)]
        public IEnumerable<LinkedContentItem> Image { get; set; } = new List<LinkedContentItem>();


        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Button text.
        /// </summary>
        public string ButtonText { get; set; }


        /// <summary>
        /// Target of button link.
        /// </summary>
        [TextInputComponent(Label = "Button target", Order = 2)]
        public string ButtonTarget { get; set; }


        /// <summary>
        /// Theme of the widget.
        /// </summary>
        [DropDownComponent(Label = "Color scheme", Order = 3, Options = "light;Light\ndark;Dark")]
        public string Theme { get; set; } = "dark";
    }
}