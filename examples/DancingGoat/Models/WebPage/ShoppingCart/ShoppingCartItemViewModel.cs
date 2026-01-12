using DancingGoat.Commerce;

namespace DancingGoat.Models;

public record ShoppingCartItemViewModel(int ContentItemId, string Name, string ImageUrl, string DetailUrl, int Quantity, decimal UnitPrice, decimal TotalPrice, decimal ListPrice, DancingGoatCatalogPromotionCandidate AppliedPromotion, int? VariantId);
