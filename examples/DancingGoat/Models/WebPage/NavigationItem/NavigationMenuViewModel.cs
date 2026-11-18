using System.Collections.Generic;

namespace DancingGoat.Models
{
    /// <param name="Items">Menu items to render.</param>
    /// <param name="ActivePath">Resolved path of the item to highlight, or null when no item matches.</param>
    public record NavigationMenuViewModel(IEnumerable<NavigationItemViewModel> Items, string ActivePath)
    {
    }
}
