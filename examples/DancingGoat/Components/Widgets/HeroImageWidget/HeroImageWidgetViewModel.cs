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
        /// Supporting text displayed under the heading.
        /// </summary>
        public string Subtext { get; set; }


        /// <summary>
        /// Button text.
        /// </summary>
        public string ButtonText { get; set; }


        /// <summary>
        /// Target of button link.
        /// </summary>
        public string ButtonTarget { get; set; }


        /// <summary>
        /// Secondary (ghost) button text.
        /// </summary>
        public string SecondaryButtonText { get; set; }


        /// <summary>
        /// Target of the secondary button link.
        /// </summary>
        public string SecondaryButtonTarget { get; set; }
    }
}
