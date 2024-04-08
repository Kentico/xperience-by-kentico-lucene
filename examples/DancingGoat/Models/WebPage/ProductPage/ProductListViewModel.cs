using System.Collections.Generic;

namespace DancingGoat.Models
{
    public record ProductListViewModel(IEnumerable<ProductListItemViewModel> Items, Dictionary<string, TaxonomyViewModel> Filter)
    {
    }
}
