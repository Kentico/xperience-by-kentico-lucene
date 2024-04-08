using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Sections
{
    /// <summary>
    /// Section properties to define the theme.
    /// </summary>
    public class ThemeSectionProperties : ISectionProperties
    {
        /// <summary>
        /// Theme of the section.
        /// </summary>
        [DropDownComponent(Label = "Color scheme", Order = 1, Options = ";None\nsection-white;Flat white\nsection-cappuccino;Cappuccino")]
        public string Theme { get; set; }
    }
}
