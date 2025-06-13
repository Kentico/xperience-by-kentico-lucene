using System.Globalization;

using CMS;
using CMS.Commerce;

using DancingGoat.Commerce;

#pragma warning disable KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[assembly: RegisterImplementation(typeof(IPriceFormatter), typeof(PriceFormatter))]

namespace DancingGoat.Commerce;

/// <summary>
/// Represents the Dancing goat price formatter.
/// </summary>
internal sealed class PriceFormatter : IPriceFormatter
{
    public string Format(decimal price, PriceFormatConfiguration configuration)
    {
        const string CULTURE_CODE_EN_US = "en-US";

        return price.ToString("C2", CultureInfo.CreateSpecificCulture(CULTURE_CODE_EN_US));
    }
}
#pragma warning restore KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
