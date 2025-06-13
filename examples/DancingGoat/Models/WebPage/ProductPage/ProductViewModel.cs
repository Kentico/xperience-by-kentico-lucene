using System.Collections.Generic;

namespace DancingGoat.Models
{
    public record ProductViewModel(string Name, string Description, string ImagePath, decimal Price, string Tag, int ContentItemId, IDictionary<string, string> Parameters, IDictionary<int, string> Variants)
    {
    }
}
