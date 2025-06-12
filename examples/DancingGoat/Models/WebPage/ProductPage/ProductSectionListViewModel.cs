using System.Collections.Generic;

namespace DancingGoat.Models
{
    public record ProductSectionListViewModel(string Title, IEnumerable<ProductListItemViewModel> Items)
    {
    }
}
