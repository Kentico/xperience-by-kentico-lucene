using Kentico.PageBuilder.Web.Mvc;

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
        public string ColorCssClass { get; set; } = "first-color";
    }
}
