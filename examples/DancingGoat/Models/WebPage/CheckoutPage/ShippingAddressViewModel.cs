using System.ComponentModel.DataAnnotations;

namespace DancingGoat.Models;

public sealed record ShippingAddressViewModel : CustomerAddressViewModel
{
    [Display(Name = "Same as billing")]
    public bool IsSameAsBilling { get; set; }
}
