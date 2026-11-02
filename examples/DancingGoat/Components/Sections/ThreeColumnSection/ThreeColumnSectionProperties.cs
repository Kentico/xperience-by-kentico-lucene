using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Sections
{
    /// <summary>
    /// Section properties for the 'Three column section'.
    /// </summary>
    public class ThreeColumnSectionProperties : ThemeSectionProperties
    {
        /// <summary>
        /// Title of the section.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.threecolumnsection.title.label$}", Order = 1)]
        public string Title { get; set; }
    }
}
