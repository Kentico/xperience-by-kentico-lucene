using System.Collections.Generic;

using CMS.ContentEngine;

namespace DancingGoat.Models
{
	/// <summary>
	/// Represents a common product model.
	/// </summary>
	public class Product
	{
		/// <summary>
		/// Represents system properties for a content item.
		/// </summary>
		public ContentItemFields SystemFields { get; set; }


		/// <summary>
		/// Product name.
		/// </summary>
		public string Name { get; set; }


		/// <summary>
		/// Product description.
		/// </summary>
		public string Description { get; set; }


		/// <summary>
		/// Product image.
		/// </summary>
		public IEnumerable<Image> Image { get; set; }
	}
}