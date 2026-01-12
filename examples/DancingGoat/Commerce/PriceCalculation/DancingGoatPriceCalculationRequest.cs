using CMS.Commerce;

namespace DancingGoat.Commerce;

public record DancingGoatPriceCalculationRequest : PriceCalculationRequestBase<DancingGoatPriceCalculationRequestItem, AddressDto>;
