using System.Collections.Generic;

using CMS.DocumentEngine;

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
        [ContentItemSelectorComponent("DancingGoatCore.Coffee", Label = "Selected products", Order = 1)]
        public IEnumerable<LinkedContentItem> SelectedProducts { get; set; } = new List<LinkedContentItem>();
    }
}