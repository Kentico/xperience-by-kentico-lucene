namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Hero image widget.
    /// </summary>
    public class HeroImageWidgetViewModel
    {
        /// <summary>
        /// Background image path.
        /// </summary>
        public string ImagePath { get; set; }


        /// <summary>
        /// Text.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Button text.
        /// </summary>
        public string ButtonText { get; set; }


        /// <summary>
        /// Target of button link.
        /// </summary>
        public string ButtonTarget { get; set; }


        /// <summary>
        /// Theme of the widget.
        /// </summary>
        public string Theme { get; set; }
    }
}
