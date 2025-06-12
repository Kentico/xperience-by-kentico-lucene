using System.Collections.Generic;
using System.Linq;

using CMS.ContentEngine;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

internal sealed class CalculationService
{
    /// <summary>
    /// Calculate total price of all items within shopping cart with specified products.
    /// </summary>
    public static decimal CalculateTotalPrice(ShoppingCartDataModel shoppingCartDataModel, IEnumerable<IProductFields> products)
    {
        return shoppingCartDataModel.Items
            .Sum(item => CalculateItemPrice(
                item.Quantity,
                products.First(product =>
                    (product as IContentItemFieldsSource).SystemFields.ContentItemID == item.ContentItemId
                ).ProductFieldPrice
            ));
    }


    /// <summary>
    /// Calculate price of all units of a product.
    /// </summary>
    public static decimal CalculateItemPrice(int quantity, decimal unitPrice)
    {
        return quantity * unitPrice;
    }
}
