using CMS.Websites;

namespace DancingGoat.Models
{
    /// <summary>
    /// Interface for view model instances based on web page items.
    /// </summary>
    public interface IWebPageBasedViewModel
    {
        /// <summary>
        /// Web page which the view model is based on.
        /// </summary>
        IWebPageFieldsSource WebPage { get; }
    }
}
