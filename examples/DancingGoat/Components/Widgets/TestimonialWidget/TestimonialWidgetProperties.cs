using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Properties for Testimonial widget.
    /// </summary>
    public class TestimonialWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Quotation text.
        /// </summary>
        public string QuotationText { get; set; }


        /// <summary>
        /// Author text.
        /// </summary>
        public string AuthorText { get; set; }


        /// <summary>
        /// Background color CSS class.
        /// </summary>
        [ExcludeFromAiraTranslation]
        public string ColorCssClass { get; set; } = "first-color";
    }
}
