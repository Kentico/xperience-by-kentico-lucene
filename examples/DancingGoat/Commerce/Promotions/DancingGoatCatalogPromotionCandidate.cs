using CMS.Commerce;

namespace DancingGoat.Commerce;

/// <summary>
/// Represents a catalog promotion candidate for DancingGoat demo site.
/// Extends base catalog promotion candidate with display label for the promotion.
/// </summary>
public class DancingGoatCatalogPromotionCandidate : CatalogPromotionCandidate
{
    /// <summary>
    /// Gets or sets the display label for the promotion (e.g., "25%" or "$10.00").
    /// </summary>
    public string PromotionDisplayLabel { get; set; }
}
