using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.PageTemplates
{
    public class LandingPageSingleColumnProperties : IPageTemplateProperties
    {
        /// <summary>
        /// Indicates if logo should be shown.
        /// </summary>
        [CheckBoxComponent(Label = "Display logo", Order = 1)]
        public bool ShowLogo { get; set; } = true;


        /// <summary>
        /// Background color CSS class of the header.
        /// </summary>
        [RequiredValidationRule]
        [DropDownComponent(Label = "Background color of header", Order = 2, Options = "first-color;Chocolate\r\nsecond-color;Gold\r\nthird-color;Espresso")]
        public string HeaderColorCssClass { get; set; } = "first-color";
    }
}
