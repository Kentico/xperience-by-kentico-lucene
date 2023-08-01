using CMS.MediaLibrary;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Card widget.
    /// </summary>
    public class CardWidgetViewModel
    {
        /// <summary>
        /// Card background image path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Card text.
        /// </summary>
        public string Text { get; set; }
    }
}