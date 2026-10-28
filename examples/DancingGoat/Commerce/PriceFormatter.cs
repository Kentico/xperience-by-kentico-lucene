using System.Globalization;

using CMS;
using CMS.Commerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Represents the Dancing goat price formatter.
/// </summary>
internal sealed class PriceFormatter : IPriceFormatter
{
    internal const string CULTURE_CODE = "en-US";


    internal static readonly string CurrencyCode = new RegionInfo(CULTURE_CODE).ISOCurrencySymbol;


    public string Format(decimal price, PriceFormatContext context)
    {
        return price.ToString("C2", CultureInfo.CreateSpecificCulture(CULTURE_CODE));
    }
}
