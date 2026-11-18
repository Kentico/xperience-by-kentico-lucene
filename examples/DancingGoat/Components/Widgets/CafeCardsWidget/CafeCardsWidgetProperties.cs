using CMS.ContentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Cafe cards widget properties.
    /// </summary>
    public class CafeCardsWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Section heading. Falls back to a localized default when empty.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.cafecardswidget.heading.label$}", ExplanationText = "{$dancinggoat.widget.heading.explanation$}", Order = 0)]
        public string Heading { get; set; }


        /// <summary>
        /// Smart folder with displayed cafes.
        /// </summary>
        [SmartFolderSelectorComponent(Label = "{$dancinggoat.cafecardswidget.cafesfolder.label$}", Order = 1)]
        public SmartFolderReference CafesFolder { get; set; }


        /// <summary>
        /// Maximum number of displayed cafes.
        /// </summary>
        [NumberInputComponent(Label = "{$dancinggoat.cafecardswidget.cafescount.label$}", Order = 2)]
        [MinimumIntegerValueValidationRule(1)]
        public int CafesCount { get; set; } = 3;
    }
}
