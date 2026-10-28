using System;

using CMS.Commerce;
using CMS.DataEngine;

namespace DancingGoat.Commerce;

/// <summary>
/// Query extensions for promotions.
/// </summary>
internal static class PromotionQueryExtensions
{
    /// <summary>
    /// Restricts the query to promotions that are active at <paramref name="currentTime"/>.
    /// </summary>
    /// <param name="query">Promotion query.</param>
    /// <param name="currentTime">Point in time the promotions are evaluated at.</param>
    /// <returns>Query for the promotions within their active date range.</returns>
    /// <remarks>
    /// A deactivated promotion has no activation date, a scheduled one activates in the future and an expired one
    /// ended in the past. None of them is active.
    /// </remarks>
    public static ObjectQuery<PromotionInfo> WhereActive(this ObjectQuery<PromotionInfo> query, DateTime currentTime)
    {
        return query
            .WhereNotNull(nameof(PromotionInfo.PromotionActiveFromWhen))
            .WhereLessThan(nameof(PromotionInfo.PromotionActiveFromWhen), currentTime)
            .Where(new WhereCondition()
                .WhereNull(nameof(PromotionInfo.PromotionActiveToWhen))
                .Or()
                .WhereGreaterThan(nameof(PromotionInfo.PromotionActiveToWhen), currentTime)
            );
    }
}
