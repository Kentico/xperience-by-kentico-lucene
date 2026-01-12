using System.Collections.Generic;

using DancingGoat.Commerce;

namespace DancingGoat.Models
{
    public record ProductViewModel(string Name, string Description, string ImagePath, decimal Price, decimal ListPrice, DancingGoatCatalogPromotionCandidate AppliedPromotion, string Tag, int ContentItemId, IDictionary<string, string> Parameters, IDictionary<int, string> Variants)
    {
    }
}
