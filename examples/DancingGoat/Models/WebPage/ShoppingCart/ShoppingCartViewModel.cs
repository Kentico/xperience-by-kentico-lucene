using System.Collections.Generic;

namespace DancingGoat.Models;

public record ShoppingCartViewModel(ICollection<ShoppingCartItemViewModel> Items, decimal TotalPrice);
