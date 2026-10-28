namespace DancingGoat.Models;

/// <summary>
/// Data behind the order summary card on the checkout step.
/// </summary>
/// <remarks>
/// Deliberately narrower than <see cref="CheckoutViewModel"/>: the card is re-rendered on every
/// shipping, payment and address change, and building the whole checkout model for it would
/// repeat the member, customer, country, state, payment and shipping method lookups each time.
/// </remarks>
/// <param name="ShoppingCart">Shopping cart contents and totals.</param>
/// <param name="ShippingPrice">Shipping price for the currently selected shipping method, after any shipping promotion discount.</param>
/// <param name="OriginalShippingPrice">Shipping price for the currently selected shipping method, before any shipping promotion discount.</param>
/// <param name="HasFreeShippingPromotion">Indicates whether a free shipping promotion is applied to the order.</param>
public sealed record OrderSummaryViewModel(ShoppingCartViewModel ShoppingCart, decimal ShippingPrice, decimal OriginalShippingPrice, bool HasFreeShippingPromotion)
{
    /// <summary>
    /// Indicates whether a shipping promotion discounted the shipping price.
    /// </summary>
    public bool HasShippingDiscount => HasFreeShippingPromotion && OriginalShippingPrice > ShippingPrice;
}
