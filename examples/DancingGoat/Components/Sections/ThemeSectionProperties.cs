using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base;
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
        [DropDownComponent(Label = "{$dancinggoat.themesection.theme.label$}", Order = 1,
            Options = ";{$dancinggoat.themesection.theme.option.none$}\nsection-white;{$dancinggoat.themesection.theme.option.white$}\nsection-cappuccino;{$dancinggoat.themesection.theme.option.cappuccino$}")]
        [ExcludeFromAiraTranslation]
        public string Theme { get; set; }
    }
}
