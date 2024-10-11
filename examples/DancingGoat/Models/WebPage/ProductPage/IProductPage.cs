using System.Collections.Generic;

using CMS.Websites;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a common product page model.
    /// </summary>
    public interface IProductPage : IWebPageFieldsSource
    {
        /// <summary>
        /// Get product related item.
        /// </summary>
        public IEnumerable<IProductFields> RelatedItem { get; }
    }
}
