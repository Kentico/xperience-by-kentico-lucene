using System.Collections.Generic;

using DancingGoat.Models;

namespace DancingGoat.Commerce;

public sealed class ProductNameProvider
{
    private readonly ProductVariantsExtractor productVariantsExtractor;


    public ProductNameProvider(ProductVariantsExtractor productVariantsExtractor)
    {
        this.productVariantsExtractor = productVariantsExtractor;
    }


    public string GetProductName(IProductFields product, int? variantId = null)
    {
        var variantValues = product == null ? null : productVariantsExtractor.ExtractVariantsValue(product);
        return FormatProductName(product?.ProductFieldName, variantValues, variantId);
    }


    private static string FormatProductName(string productName, IDictionary<int, string> variants, int? variantId)
    {
        return variants != null && variantId != null && variants.TryGetValue(variantId.Value, out var variantValue)
            ? $"{productName} - {variantValue}"
            : productName;
    }
}
