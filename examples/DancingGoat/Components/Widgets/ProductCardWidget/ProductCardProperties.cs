using System.Collections.Generic;

using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Product card widget properties.
    /// </summary>
    public class ProductCardProperties : IWidgetProperties
    {
        /// <summary>
        /// Selected products.
        /// </summary>
        [ContentItemSelectorComponent(Coffee.CONTENT_TYPE_NAME, Label = "Selected products", Order = 1)]
        public IEnumerable<ContentItemReference> SelectedProducts { get; set; } = new List<ContentItemReference>();
    }
}
