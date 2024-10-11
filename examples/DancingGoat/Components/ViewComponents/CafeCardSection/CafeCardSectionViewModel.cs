using System.Collections.Generic;

using DancingGoat.Models;

namespace DancingGoat.ViewComponents
{
    public record CafeCardSectionViewModel(IEnumerable<CafeViewModel> Cafes, string ContactsPagePath)
    {
    }
}
