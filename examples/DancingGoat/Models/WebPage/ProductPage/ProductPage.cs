using System.Collections.Generic;

using CMS.Websites;

namespace DancingGoat.Models
{
	/// <summary>
	/// Represents a common product page model.
	/// </summary>
	public partial class ProductPage : IWebPageFieldsSource
	{
		/// <summary>
		/// Represents system properties for a web page item.
		/// </summary>
		public WebPageFields SystemFields { get; set; }


		/// <summary>
		/// RelatedItem.
		/// </summary>
		public IEnumerable<Product> RelatedItem { get; set; }
	}
}