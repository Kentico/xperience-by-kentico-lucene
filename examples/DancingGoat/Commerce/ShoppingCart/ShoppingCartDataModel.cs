using System.Collections.Generic;

namespace DancingGoat.Commerce;

public sealed class ShoppingCartDataModel
{
    /// <summary>
    /// Items inside the shopping cart.
    /// </summary>
    public ICollection<ShoppingCartDataItem> Items { get; init; } = new List<ShoppingCartDataItem>();
}
