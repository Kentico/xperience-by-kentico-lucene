using CMS.MediaLibrary;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Banner widget.
    /// </summary>
    public class BannerWidgetModel
    {
        /// <summary>
        /// Banner background image path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Banner text.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Gets or sets a title for a link defined by <see cref="LinkUrl"/>.
        /// </summary>
        public string LinkTitle { get; set; }


        /// <summary>
        /// Gets or sets URL to which the visitor is redirected after clicking on the <see cref="Text"/>.
        /// </summary>
        public string LinkUrl { get; set; }
    }
}