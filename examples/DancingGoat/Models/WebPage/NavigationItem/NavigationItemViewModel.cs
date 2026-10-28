namespace DancingGoat.Models
{
    /// <param name="Caption">Text of the menu item.</param>
    /// <param name="RelativeUrl">URL the menu item links to.</param>
    /// <param name="ResolvedPath">Normalized <paramref name="RelativeUrl"/>, compared against the current
    /// request path to decide which item is active.</param>
    public record NavigationItemViewModel(string Caption, string RelativeUrl, string ResolvedPath)
    {
    }
}
