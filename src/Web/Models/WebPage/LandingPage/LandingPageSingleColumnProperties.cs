using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Models
{
    public class LandingPageSingleColumnProperties : IWebPageViewModel
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


        // KX-7003 - Will be removed after content modernization"
        public IRoutedWebPage WebPage => throw new System.NotImplementedException();
    }
}
