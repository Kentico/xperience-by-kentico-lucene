using System.Collections.Generic;

using CMS.DocumentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Cafe card widget properties.
    /// </summary>
    public class CafeCardProperties : IWidgetProperties
    {
        /// <summary>
        /// Selected cafes.
        /// </summary>
        [ContentItemSelectorComponent("DancingGoatCore.Cafe", Order = 1, Label = "Cafe")]
        [RequiredValidationRule]
        public IEnumerable<LinkedContentItem> SelectedCafes { get; set; } = new List<LinkedContentItem>();
    }
}