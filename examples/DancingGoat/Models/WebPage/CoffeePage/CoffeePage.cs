using System.Collections.Generic;

namespace DancingGoat.Models
{
    /// <summary>
    /// Custom code for page of type <see cref="CoffeePage"/>.
    /// </summary>
    public partial class CoffeePage : IProductPage
    {
        /// <inheritdoc />
        IEnumerable<IProductFields> IProductPage.RelatedItem { get => RelatedItem; }
    }
}
