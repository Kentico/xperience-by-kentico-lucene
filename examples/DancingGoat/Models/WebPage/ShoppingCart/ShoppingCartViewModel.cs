using System.Collections.Generic;
using System.Linq;

using DancingGoat.Commerce;

namespace DancingGoat.Models;

/// <param name="Items">Lines of the cart.</param>
/// <param name="TotalPrice">Grand total including shipping and tax. The cart page shows <paramref name="TotalWithoutShippingAndTax"/> instead, because shipping and tax are only known at checkout.</param>
/// <param name="LinesSubtotal">Sum of the line totals after line-level (catalog) discounts and before the order-level discount, shown as the "Subtotal" summary row.</param>
/// <param name="TotalWithoutShippingAndTax">Sum of the line totals after all discounts, excluding shipping and tax.</param>
/// <param name="TotalTax">Tax for the whole cart.</param>
/// <param name="TotalDiscount">Line-level and order-level discounts combined.</param>
/// <param name="OrderDiscountText">Message about the applied or the next available order promotion, shown as a banner under the cart.</param>
/// <param name="CouponCodes">Promo/discount codes entered for this cart, with their status.</param>
/// <param name="OrderDiscount">Order-level discount amount, shown as its own summary row. Line-level (catalog) discounts are already reflected in each line total.</param>
/// <param name="OrderDiscountName">Display name of the applied order promotion, shown under the discount row. Null when no order promotion applies.</param>
/// <param name="CouponCodeAttempt">Code the customer last tried to apply, kept in the field so it can be corrected. Null unless the attempt failed.</param>
/// <param name="CouponCodeError">Why the last attempt failed, shown under the code field. Null unless the attempt failed.</param>
/// <param name="FreeShippingText">Message about the applied or the next available free shipping promotion, shown as a banner under the cart.</param>
public record ShoppingCartViewModel(IEnumerable<ShoppingCartItemViewModel> Items, decimal TotalPrice, decimal LinesSubtotal, decimal TotalWithoutShippingAndTax, decimal TotalTax, decimal TotalDiscount, string OrderDiscountText, IEnumerable<CouponCodeViewModel> CouponCodes, decimal OrderDiscount = 0, string OrderDiscountName = null, string CouponCodeAttempt = null, string CouponCodeError = null, string FreeShippingText = null)
{
    /// <summary>
    /// Gets a view model representing an empty shopping cart.
    /// </summary>
    public static ShoppingCartViewModel Empty { get; } = new([], 0, 0, 0, 0, 0, null, []);


    /// <summary>
    /// Gets the number of units across all lines, shown as the "Items" row of the cart summary.
    /// </summary>
    public int TotalQuantity => Items.Sum(item => item.Quantity);
}
