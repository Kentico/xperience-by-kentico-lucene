using Kentico.PageBuilder.Web.Mvc;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Properties for Articles widget.
    /// </summary>
    public class ArticlesWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Number of articles to show.
        /// </summary>
        public int Count { get; set; } = 5;
    }
}