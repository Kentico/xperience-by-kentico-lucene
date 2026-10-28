using System;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.DataEngine;
using CMS.DataEngine.Query;

namespace DancingGoat.Commerce;

/// <summary>
/// Repository for promotion coupon.
/// </summary>
public class PromotionCouponRepository
{
    private readonly IInfoProvider<PromotionCouponInfo> promotionCouponInfoProvider;
    private readonly IInfoProvider<PromotionInfo> promotionInfoProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionCouponRepository"/> class.
    /// </summary>
    public PromotionCouponRepository(IInfoProvider<PromotionCouponInfo> promotionCouponInfoProvider, IInfoProvider<PromotionInfo> promotionInfoProvider)
    {
        this.promotionCouponInfoProvider = promotionCouponInfoProvider;
        this.promotionInfoProvider = promotionInfoProvider;
    }


    /// <summary>
    /// Checks if a promotion coupon of a currently active promotion exists.
    /// </summary>
    /// <param name="couponCode">The coupon code to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the promotion coupon exists and its promotion is active, false otherwise.</returns>
    /// <remarks>
    /// A coupon of an inactive promotion is treated as non-existent, so the storefront rejects it the same way as
    /// an unknown code.
    /// </remarks>
    public async Task<bool> PromotionCouponExists(string couponCode, CancellationToken cancellationToken)
    {
        var activePromotionSubquery = promotionInfoProvider.Get()
            .Column(nameof(PromotionInfo.PromotionID))
            .WhereEquals(nameof(PromotionInfo.PromotionID), nameof(PromotionCouponInfo.PromotionCouponPromotionID).AsColumn())
            .WhereActive(DateTime.Now);

        var promotionCouponId = await promotionCouponInfoProvider.Get()
            .WhereEquals(nameof(PromotionCouponInfo.PromotionCouponCode), couponCode)
            .WhereExists(activePromotionSubquery)
            .Column(nameof(PromotionCouponInfo.PromotionCouponID))
            .TopN(1)
            .GetScalarResultAsync<int>(cancellationToken: cancellationToken);

        return promotionCouponId > 0;
    }
}
