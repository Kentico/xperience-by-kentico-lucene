using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Testimonial widget.
    /// </summary>
    public class TestimonialWidgetViewModel
    {
        /// <summary>
        /// Section heading, or <see langword="null"/> to use the localized default.
        /// </summary>
        public string Heading { get; set; }


        /// <summary>
        /// Quotation text.
        /// </summary>
        public string Quote { get; set; }


        /// <summary>
        /// Author name.
        /// </summary>
        public string Author { get; set; }


        /// <summary>
        /// Author role.
        /// </summary>
        public string AuthorRole { get; set; }


        /// <summary>
        /// Optional author photo path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Gets ViewModel for <paramref name="testimonial"/>.
        /// </summary>
        public static TestimonialWidgetViewModel GetViewModel(Testimonial testimonial, string heading, string imagePath = null)
        {
            if (testimonial == null)
            {
                return null;
            }

            return new TestimonialWidgetViewModel
            {
                Heading = heading,
                Quote = testimonial.TestimonialQuote,
                Author = testimonial.TestimonialAuthor,
                AuthorRole = testimonial.TestimonialAuthorRole,
                ImagePath = imagePath
            };
        }
    }
}
