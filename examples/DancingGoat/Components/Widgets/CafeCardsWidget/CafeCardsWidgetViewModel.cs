using System.Collections.Generic;

using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Cafe cards widget.
    /// </summary>
    public class CafeCardsWidgetViewModel
    {
        /// <summary>
        /// Section heading, or <see langword="null"/> to use the localized default.
        /// </summary>
        public string Heading { get; set; }


        /// <summary>
        /// Collection of cafes.
        /// </summary>
        public IEnumerable<CafeViewModel> Cafes { get; set; }


        /// <summary>
        /// Relative path to the contacts page.
        /// </summary>
        public string ContactsPagePath { get; set; }
    }
}
