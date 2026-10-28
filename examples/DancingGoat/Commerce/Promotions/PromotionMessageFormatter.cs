using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Localization;

namespace DancingGoat.Commerce;

/// <summary>
/// Formats the parts the storefront promotion messages share.
/// </summary>
internal static class PromotionMessageFormatter
{
    /// <summary>
    /// Joins the names into an enumeration: "A", "A or B", "A, B or C".
    /// </summary>
    /// <param name="localizer">Localizer of the storefront resources.</param>
    /// <param name="names">Names to join. A null or empty name is left out.</param>
    /// <returns>The enumeration, or <c>null</c> when no name is left to join.</returns>
    public static string JoinWithOr(IStringLocalizer<SharedResources> localizer, IEnumerable<string> names)
    {
        var nameList = names.Where(name => !string.IsNullOrEmpty(name)).ToList();
        if (nameList.Count == 0)
        {
            return null;
        }

        if (nameList.Count == 1)
        {
            return nameList[0];
        }

        return string.Format(localizer["{0} or {1}"], string.Join(", ", nameList.Take(nameList.Count - 1)), nameList[^1]);
    }
}
