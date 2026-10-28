using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.Localization;

namespace DancingGoat.Models;

/// <summary>
/// View model for the product list item grid partial view.
/// </summary>
public record ProductListItemGridViewModel(IEnumerable<ProductListItemViewModel> Items, LocalizedHtmlString EmptyMessage = null);
