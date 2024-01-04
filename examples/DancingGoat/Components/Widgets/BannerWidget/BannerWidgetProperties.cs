using System;
using System.Collections.Generic;

using CMS.DocumentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Content.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Banner widget properties.
    /// </summary>
    public class BannerWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Image to be displayed.
        /// </summary>
        [ContentItemSelectorComponent("DancingGoatCore.Media", Label = "Background image", Order = 1)]
        public IEnumerable<LinkedContentItem> Image { get; set; } = new List<LinkedContentItem>();

        
        /// <summary>
        /// Text to be displayed.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Gets or sets URL to which a visitor is redirected after clicking on the <see cref="Text"/>.
        /// </summary>
        [UrlSelectorComponent(Label = "Link URL", Order = 2)]
        public string LinkUrl { get; set; }


        /// <summary>
        /// Gets or sets a title for a link defined by <see cref="LinkUrl"/>.
        /// </summary>
        /// <remarks>
        /// If URL targets a page in the site then URL is stored in a given format '~/en-us/article'.
        /// </remarks>
        [TextInputComponent(Label = "Link title", Order = 3)]
        [VisibleIfNotEmpty(nameof(LinkUrl))]
        public string LinkTitle { get; set; } = String.Empty;
    }
}