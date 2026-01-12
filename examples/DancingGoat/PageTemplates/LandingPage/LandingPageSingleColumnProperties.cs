using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.PageTemplates
{
    public class LandingPageSingleColumnProperties : IPageTemplateProperties
    {
        /// <summary>
        /// Indicates if logo should be shown.
        /// </summary>
        [CheckBoxComponent(Label = "{$dancinggoat.landingpagesinglecolumn.showlogo.label$}", Order = 1)]
        public bool ShowLogo { get; set; } = true;


        /// <summary>
        /// Background color CSS class of the header.
        /// </summary>
        [RequiredValidationRule]
        [DropDownComponent(Label = "{$dancinggoat.landingpagesinglecolumn.headercolor.label$}", Order = 2,
            Options = "first-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.chocolate$}\nsecond-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.gold$}\nthird-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.espresso$}")]
        [ExcludeFromAiraTranslation]
        public string HeaderColorCssClass { get; set; } = "first-color";
    }
}
