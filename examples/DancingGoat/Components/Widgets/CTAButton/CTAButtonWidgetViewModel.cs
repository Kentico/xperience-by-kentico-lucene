namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for the CTA Button widget.
    /// </summary>
    public class CTAButtonWidgetViewModel
    {
        /// <summary>
        /// Text of the button.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// URL where the button points to.
        /// </summary>
        public string Url { get; set; }


        /// <summary>
        /// Indicates if link should be opened in a new tab.
        /// </summary>
        public bool OpenInNewTab { get; set; }
    }
}
