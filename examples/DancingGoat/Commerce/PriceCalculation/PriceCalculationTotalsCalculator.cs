using System.Linq;

using CMS.Commerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Calculator for getting totals from <see cref="DancingGoatPriceCalculationResult"/>.
/// </summary>
public sealed class PriceCalculationTotalsCalculator
{
    private readonly IPromotionCoveredQuantityCalculator promotionCoveredQuantityCalculator;


    /// <summary>
    /// Initializes a new instance of the <see cref="PriceCalculationTotalsCalculator"/> class.
    /// </summary>
    /// <param name="promotionCoveredQuantityCalculator">Calculator of the units a promotion covers.</param>
    public PriceCalculationTotalsCalculator(IPromotionCoveredQuantityCalculator promotionCoveredQuantityCalculator)
    {
        this.promotionCoveredQuantityCalculator = promotionCoveredQuantityCalculator;
    }


    /// <summary>
    /// Gets the subtotal from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetSubtotal(DancingGoatPriceCalculationResult calculationResult)
    {
        return calculationResult.Items.Sum(x => x.Quantity * x.ProductData.UnitPrice);
    }


    /// <summary>
    /// Gets the subtotal after line discounts (catalog discounts) were applied.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetSubtotalAfterLineDiscount(DancingGoatPriceCalculationResult calculationResult)
    {
        return GetSubtotal(calculationResult) - GetTotalCatalogDiscountAmount(calculationResult) - GetTotalCartItemDiscountAmount(calculationResult);
    }


    /// <summary>
    /// Gets the sum of the line totals after line discounts, shown as the "Subtotal" summary row.
    /// </summary>
    /// <remarks>
    /// Sums the same rounded per-line figures the cart lines display, unlike
    /// <see cref="GetSubtotalAfterLineDiscount"/>, which subtracts an unrounded discount from the
    /// undiscounted subtotal and can therefore disagree with the lines by a cent.
    /// </remarks>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetLinesSubtotal(DancingGoatPriceCalculationResult calculationResult)
    {
        return calculationResult.Items.Sum(x => x.LineSubtotalAfterLineDiscount);
    }


    /// <summary>
    /// Gets the total discount amount from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetTotalDiscountAmount(DancingGoatPriceCalculationResult calculationResult)
    {
        // Calculate the total catalog-level discount across all items (per-item discounts)
        var totalCatalogDiscount = GetTotalCatalogDiscountAmount(calculationResult);

        // Add what cart item promotions took off
        var totalCartItemDiscount = GetTotalCartItemDiscountAmount(calculationResult);

        // Get the order-level discount
        var orderDiscount = GetOrderDiscountAmount(calculationResult);

        return totalCatalogDiscount + totalCartItemDiscount + orderDiscount;
    }


    /// <summary>
    /// Gets the total without shipping and tax from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetTotalWithoutShippingAndTax(DancingGoatPriceCalculationResult calculationResult)
    {
        return calculationResult.Items.Sum(x => x.LineSubtotalAfterAllDiscounts);
    }


    /// <summary>
    /// Gets the order-level discount amount from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    public decimal GetOrderDiscountAmount(DancingGoatPriceCalculationResult calculationResult)
    {
        // Get the order-level discount
        var orderDiscount = calculationResult.PromotionData.OrderPromotionCandidates
            .FirstOrDefault(candidate => candidate.Applied)
            ?.PromotionCandidate.OrderDiscountAmount ?? 0;

        return orderDiscount;
    }


    /// <summary>
    /// Gets the sum of all item catalog-level discounts from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    private decimal GetTotalCatalogDiscountAmount(DancingGoatPriceCalculationResult calculationResult)
    {
        return calculationResult.Items
            .SelectMany(item => item.PromotionData.CatalogPromotionCandidates
                .Select(candidate => candidate.PromotionCandidate.UnitPriceDiscountAmount * promotionCoveredQuantityCalculator.GetCoveredQuantity(item, candidate)))
            .Sum();
    }


    /// <summary>
    /// Gets the sum of all cart item discount promotion discounts from the calculation result.
    /// </summary>
    /// <param name="calculationResult">The calculation result.</param>
    private decimal GetTotalCartItemDiscountAmount(DancingGoatPriceCalculationResult calculationResult)
    {
        return calculationResult.Items
            .SelectMany(item => item.PromotionData.CartItemPromotionCandidates
                .Select(candidate => candidate.PromotionCandidate.UnitPriceDiscountAmount * promotionCoveredQuantityCalculator.GetCoveredQuantity(item, candidate)))
            .Sum();
    }
}
