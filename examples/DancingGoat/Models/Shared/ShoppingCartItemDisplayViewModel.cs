namespace DancingGoat.Models;

/// <summary>
/// View model for the editable shopping cart item partial view. The read-only variant shown
/// on the order review page has its own partial and does not need the language for its links.
/// </summary>
public record ShoppingCartItemDisplayViewModel(ShoppingCartItemViewModel Item, string LanguageName = null);
