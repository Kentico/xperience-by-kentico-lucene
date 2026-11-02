using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;

using Microsoft.Extensions.Options;

namespace DancingGoat.Commerce;

public sealed class DancingGoatTaxPriceCalculationStep<TRequest, TResult> : ITaxPriceCalculationStep<TRequest, TResult>
    where TRequest : DancingGoatPriceCalculationRequest
    where TResult : DancingGoatPriceCalculationResult
{
    private readonly IPriceCalculationRoundingService priceRoundingService;
    private readonly TaxOptions taxOptions;

    public DancingGoatTaxPriceCalculationStep(IPriceCalculationRoundingService priceRoundingService, IOptions<TaxOptions> taxOptions)
    {
        this.priceRoundingService = priceRoundingService;

        this.taxOptions = taxOptions.Value;
    }


    public Task Execute(IPriceCalculationData<TRequest, TResult> calculationData, CancellationToken cancellationToken)
    {
        if (calculationData.Result.Items.Any(item => item.ProductData == null))
        {
            throw new InvalidOperationException($"Some items are missing product data: {string.Join(", ", calculationData.Result.Items.Where(item => item.ProductData == null).Select(item => item.ProductIdentifier))}");
        }

        calculationData.Result.TotalTax = 0;

        foreach (var resultItem in calculationData.Result.Items)
        {
            resultItem.TaxRate = DancingGoatTaxRateConstants.TAX_RATE;
            resultItem.LineTaxAmount = GetTaxAmount(resultItem.LineSubtotalAfterAllDiscounts);
            calculationData.Result.TotalTax += resultItem.LineTaxAmount;
        }
        calculationData.Result.ShippingTax = GetTaxAmount(calculationData.Result.ShippingPrice);

        calculationData.Result.TotalTax += calculationData.Result.ShippingTax;

        return Task.CompletedTask;
    }


    /// <summary>
    /// Calculates tax amount based on the price and tax options.
    /// </summary>
    private decimal GetTaxAmount(decimal price)
    {
        if (taxOptions.PricesIncludeTax)
        {
            return priceRoundingService.Round(price * (DancingGoatTaxRateConstants.TAX_RATE / (100m + DancingGoatTaxRateConstants.TAX_RATE)), new PriceCalculationRoundingContext());
        }

        return priceRoundingService.Round(DancingGoatTaxRateConstants.TAX_RATE * price / 100m, new PriceCalculationRoundingContext());
    }
}
