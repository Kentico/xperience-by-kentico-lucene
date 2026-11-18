using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.DataEngine;
using CMS.DataEngine.Query;
using CMS.Helpers;

namespace DancingGoat.Commerce;

/// <summary>
/// Repository for promotions the storefront can offer to the customer.
/// </summary>
public sealed class PromotionRepository
{
    private readonly IInfoProvider<PromotionInfo> promotionInfoProvider;
    private readonly IInfoProvider<PromotionCouponInfo> promotionCouponInfoProvider;


    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionRepository"/> class.
    /// </summary>
    public PromotionRepository(IInfoProvider<PromotionInfo> promotionInfoProvider, IInfoProvider<PromotionCouponInfo> promotionCouponInfoProvider)
    {
        this.promotionInfoProvider = promotionInfoProvider;
        this.promotionCouponInfoProvider = promotionCouponInfoProvider;
    }


    /// <summary>
    /// Retrieves the active promotions of the given type that the customer can use now.
    /// </summary>
    /// <param name="promotionType">Type of the promotions to retrieve.</param>
    /// <param name="appliedCouponCodes">Coupon codes applied in the shopping cart. A coupon-activated promotion is included only when its code is applied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Promotions within their active date range that have no coupon, or whose coupon code is in <paramref name="appliedCouponCodes"/>.
    /// </returns>
    public async Task<IEnumerable<PromotionInfo>> GetActivePromotions(PromotionType promotionType, IEnumerable<string> appliedCouponCodes, CancellationToken cancellationToken)
    {
        var couponCodeList = appliedCouponCodes?.ToList() ?? [];

        var couponSubquery = promotionCouponInfoProvider.Get()
            .Column(nameof(PromotionCouponInfo.PromotionCouponID))
            .WhereEquals(nameof(PromotionCouponInfo.PromotionCouponPromotionID), nameof(PromotionInfo.PromotionID).AsColumn());

        var query = GetActivePromotionsQuery(promotionType);

        if (couponCodeList.Count > 0)
        {
            var appliedCouponSubquery = promotionCouponInfoProvider.Get()
                .Column(nameof(PromotionCouponInfo.PromotionCouponPromotionID))
                .WhereIn(nameof(PromotionCouponInfo.PromotionCouponCode), couponCodeList)
                .WhereEquals(nameof(PromotionCouponInfo.PromotionCouponPromotionID), nameof(PromotionInfo.PromotionID).AsColumn());

            query = query.Where(
                new WhereCondition()
                    .WhereNotExists(couponSubquery)
                    .Or()
                    .WhereExists(appliedCouponSubquery)
            );
        }
        else
        {
            query = query.WhereNotExists(couponSubquery);
        }

        return await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Gets the available promotions based on the specified criteria.
    /// </summary>
    /// <param name="promotionType">Type of the promotion to filter by.</param>
    /// <param name="promotionRuleIdentifier">Identifier of the promotion rule to filter by.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of available promotions matching the specified criteria.</returns>
    public async Task<IEnumerable<PromotionInfo>> GetAvailablePromotions(PromotionType promotionType, string promotionRuleIdentifier, CancellationToken cancellationToken)
    {
        var query = GetActivePromotionsQuery(promotionType);

        if (promotionRuleIdentifier != null)
        {
            query.WhereEquals(nameof(PromotionInfo.PromotionRuleIdentifier), promotionRuleIdentifier);
        }

        return await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Retrieves the coupon code that activates each of the promotions.
    /// </summary>
    /// <param name="promotionIds">Identifiers of the promotions to examine.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// Promotion identifier to coupon code. A promotion that needs no coupon code is not in the result. When
    /// several coupon codes activate the promotion, the oldest one is taken, so that the product detail keeps
    /// naming the same code.
    /// </returns>
    public async Task<IDictionary<int, string>> GetActivatingCouponCodes(IEnumerable<int> promotionIds, CancellationToken cancellationToken)
    {
        var coupons = await promotionCouponInfoProvider.Get()
            .Columns(nameof(PromotionCouponInfo.PromotionCouponID), nameof(PromotionCouponInfo.PromotionCouponPromotionID), nameof(PromotionCouponInfo.PromotionCouponCode))
            .WhereIn(nameof(PromotionCouponInfo.PromotionCouponPromotionID), promotionIds.ToList())
            .OrderBy(nameof(PromotionCouponInfo.PromotionCouponID))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return coupons
            .GroupBy(coupon => coupon.PromotionCouponPromotionID)
            .ToDictionary(group => group.Key, group => group.First().PromotionCouponCode);
    }


    /// <summary>
    /// Gets the display name of the promotion identified by <paramref name="promotionId"/>.
    /// </summary>
    /// <param name="promotionId">Identifier of the promotion.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The promotion display name, or <c>null</c> when no promotion is given or it cannot be found.</returns>
    /// <example>
    /// Example return value: "Order over $200 - 5% off"
    /// </example>
    public async Task<string> GetPromotionDisplayName(int? promotionId, CancellationToken cancellationToken)
    {
        if (promotionId is null or 0)
        {
            return null;
        }

        var promotion = await promotionInfoProvider.GetAsync(promotionId.Value, cancellationToken);

        return string.IsNullOrEmpty(promotion?.PromotionDisplayName) ? null : promotion.PromotionDisplayName;
    }


    /// <summary>
    /// Gets the display names of multiple promotions in one query.
    /// </summary>
    /// <param name="promotionIds">Identifiers of the promotions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary of promotion identifier to display name. A promotion that cannot be found is not in the result.</returns>
    public async Task<IReadOnlyDictionary<int, string>> GetPromotionDisplayNames(IEnumerable<int> promotionIds, CancellationToken cancellationToken)
    {
        var promotionIdList = promotionIds.Distinct().ToList();
        if (promotionIdList.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var promotions = await promotionInfoProvider.Get()
            .Columns(nameof(PromotionInfo.PromotionID), nameof(PromotionInfo.PromotionDisplayName))
            .WhereIn(nameof(PromotionInfo.PromotionID), promotionIdList)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return promotions.ToDictionary(promotion => promotion.PromotionID, promotion => promotion.PromotionDisplayName);
    }


    /// <summary>
    /// Creates a query for promotions of the given type that are within their active date range.
    /// </summary>
    /// <param name="promotionType">Type of the promotions to retrieve.</param>
    /// <returns>Query for the active promotions of the given type.</returns>
    private ObjectQuery<PromotionInfo> GetActivePromotionsQuery(PromotionType promotionType)
    {
        return promotionInfoProvider.Get()
            .WhereEquals(nameof(PromotionInfo.PromotionType), promotionType.ToStringRepresentation())
            .WhereActive(DateTime.Now);
    }
}
