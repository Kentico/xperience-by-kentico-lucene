namespace DancingGoat.Models;

public record ShoppingCartItemViewModel(int ContentItemId, string Name, string ImageUrl, string DetailUrl, int Quantity, decimal UnitPrice, decimal TotalPrice, int? VariantId);
