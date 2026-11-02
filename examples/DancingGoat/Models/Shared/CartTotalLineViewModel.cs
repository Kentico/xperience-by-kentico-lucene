using Microsoft.AspNetCore.Mvc.Localization;

namespace DancingGoat.Models;

/// <summary>
/// View model for a single line of the cart totals partial view.
/// </summary>
public record CartTotalLineViewModel(LocalizedHtmlString Label, decimal Price, string RowCssClass = null, string ValueCssClass = null);
