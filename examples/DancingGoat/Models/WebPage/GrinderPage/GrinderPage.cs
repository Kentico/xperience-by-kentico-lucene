using System.Collections.Generic;

namespace DancingGoat.Models
{
    /// <summary>
    /// Custom code for page of type <see cref="GrinderPage"/>.
    /// </summary>
    public partial class GrinderPage : IProductPage
    {
        /// <inheritdoc />
        IEnumerable<IProductFields> IProductPage.RelatedItem { get => RelatedItem; }
    }
}
