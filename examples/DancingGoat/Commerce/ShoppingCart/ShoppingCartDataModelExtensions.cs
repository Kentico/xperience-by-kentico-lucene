using System.Text.Json;

using CMS.Commerce;

using DancingGoat.Commerce;

#pragma warning disable KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace DancingGoat.Helpers;

internal static class ShoppingCartDataModelExtensions
{
    /// <summary>
    /// Deserializes the shopping cart data model from the shopping cart object.
    /// </summary>
    public static ShoppingCartDataModel GetShoppingCartDataModel(this ShoppingCartInfo shoppingCart)
    {
        return (string.IsNullOrEmpty(shoppingCart?.ShoppingCartData) ? null : JsonSerializer.Deserialize<ShoppingCartDataModel>(shoppingCart.ShoppingCartData))
            ?? new ShoppingCartDataModel();
    }


    /// <summary>
    /// Serializes the shopping cart data model in the shopping cart object.
    /// </summary>
    public static void StoreShoppingCartDataModel(this ShoppingCartInfo shoppingCart, ShoppingCartDataModel shoppingCartData)
    {
        shoppingCart.ShoppingCartData = JsonSerializer.Serialize(shoppingCartData);
    }
}
#pragma warning restore KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
