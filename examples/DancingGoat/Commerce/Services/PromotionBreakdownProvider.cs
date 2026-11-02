using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

/// <summary>
/// Breaks down cart lines that are priced by more than one promotion.
/// </summary>
/// <remarks>
/// A cart line can have multiple promotions applied. In that case, the breakdown shows how each promotion contributes to the line price.
/// A line with a single promotion shows only its original and discounted price instead and gets no breakdown.
/// </remarks>
public sealed class PromotionBreakdownProvider
{
    private readonly PromotionRepository promotionRepository;
    private readonly IPriceCalculationRoundingService priceCalculationRoundingService;
    private readonly IPromotionCoveredQuantityCalculator promotionCoveredQuantityCalculator;


    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionBreakdownProvider"/> class.
    /// </summary>
    public PromotionBreakdownProvider(PromotionRepository promotionRepository, IPriceCalculationRoundingService priceCalculationRoundingService,
        IPromotionCoveredQuantityCalculator promotionCoveredQuantityCalculator)
    {
        this.promotionRepository = promotionRepository;
        this.priceCalculationRoundingService = priceCalculationRoundingService;
        this.promotionCoveredQuantityCalculator = promotionCoveredQuantityCalculator;
    }


    /// <summary>
    /// Gets the breakdown of every cart line with multiple applied promotions.
    /// </summary>
    /// <param name="calculationResult">The calculation result of the cart.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IReadOnlyDictionary<ProductVariantIdentifier, IReadOnlyList<PromotionBreakdown>>> GetPromotionBreakdowns(DancingGoatPriceCalculationResult calculationResult, CancellationToken cancellationToken)
    {
        var linesWithMultiplePromotions = calculationResult.Items
            .Select(item => new LineWithMultiplePromotions(
                item,
                item.PromotionData.CatalogPromotionCandidates.FirstOrDefault(candidate => candidate.Applied),
                item.PromotionData.CartItemPromotionCandidates.FirstOrDefault(candidate => candidate.Applied)))
            .Where(line => line.CatalogCandidate != null && line.CartItemCandidate != null)
            .ToList();

        if (linesWithMultiplePromotions.Count == 0)
        {
            return new Dictionary<ProductVariantIdentifier, IReadOnlyList<PromotionBreakdown>>();
        }

        var promotionIds = linesWithMultiplePromotions.SelectMany(line => new[] { line.CatalogCandidate.PromotionID, line.CartItemCandidate.PromotionID });
        var promotionNames = await promotionRepository.GetPromotionDisplayNames(promotionIds, cancellationToken);

        return linesWithMultiplePromotions.ToDictionary(line => line.Item.ProductIdentifier, line => GetLineBreakdowns(line, promotionNames));
    }


    private IReadOnlyList<PromotionBreakdown> GetLineBreakdowns(LineWithMultiplePromotions line, IReadOnlyDictionary<int, string> promotionNames)
    {
        var item = line.Item;
        var unitPrice = item.ProductData.UnitPrice;

        var cartItemQuantity = promotionCoveredQuantityCalculator.GetCoveredQuantity(item, line.CartItemCandidate);
        var cartItemDiscountedPrice = Round(cartItemQuantity * (unitPrice - line.CartItemCandidate.PromotionCandidate.UnitPriceDiscountAmount));

        var catalogQuantity = promotionCoveredQuantityCalculator.GetCoveredQuantity(item, line.CatalogCandidate);
        var catalogDiscountedPrice = Round(catalogQuantity * (unitPrice - line.CatalogCandidate.PromotionCandidate.UnitPriceDiscountAmount));

        return
        [
            new PromotionBreakdown(promotionNames.GetValueOrDefault(line.CatalogCandidate.PromotionID), catalogQuantity, Round(catalogQuantity * unitPrice), catalogDiscountedPrice),
            new PromotionBreakdown(promotionNames.GetValueOrDefault(line.CartItemCandidate.PromotionID), cartItemQuantity, Round(cartItemQuantity * unitPrice), cartItemDiscountedPrice),
        ];
    }


    private decimal Round(decimal price) => priceCalculationRoundingService.Round(price, new PriceCalculationRoundingContext());


    private sealed record LineWithMultiplePromotions(
        DancingGoatPriceCalculationResultItem Item,
        IPriceCalculationPromotionCandidate<CatalogPromotionCandidate> CatalogCandidate,
        IPriceCalculationPromotionCandidate<CartItemPromotionCandidate> CartItemCandidate);
}
