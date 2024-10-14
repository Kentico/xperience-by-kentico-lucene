using System.Collections.Generic;

using CMS.ContentEngine;

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
        [ContentItemSelectorComponent(typeof(ProductCardSchemaFilter), Label = "Selected products", Order = 1)]
        public IEnumerable<ContentItemReference> SelectedProducts { get; set; } = new List<ContentItemReference>();
    }
}
