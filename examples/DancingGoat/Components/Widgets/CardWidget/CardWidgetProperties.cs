using System.Collections.Generic;

using CMS.ContentEngine;

using Kentico.Forms.Web.Mvc;
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
        [ContentItemSelectorComponent(Models.Image.CONTENT_TYPE_NAME, Label = "{$dancinggoat.cardwidget.image.label$}", Order = 1)]
        public IEnumerable<ContentItemReference> Image { get; set; } = new List<ContentItemReference>();

        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string Text { get; set; }
    }
}
