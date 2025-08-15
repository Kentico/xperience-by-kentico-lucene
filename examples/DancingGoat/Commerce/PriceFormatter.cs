using System.Globalization;

using CMS;
using CMS.Commerce;

using DancingGoat.Commerce;

[assembly: RegisterImplementation(typeof(IPriceFormatter), typeof(PriceFormatter))]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents the Dancing goat price formatter.
/// </summary>
internal sealed class PriceFormatter : IPriceFormatter
{
    public string Format(decimal price, PriceFormatContext context)
    {
        const string CULTURE_CODE_EN_US = "en-US";

        return price.ToString("C2", CultureInfo.CreateSpecificCulture(CULTURE_CODE_EN_US));
    }
}
