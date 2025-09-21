using DancingGoat.Commerce;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat;

public static class IApplicationBuilderExtensions
{
    /// <summary>
    /// Initializes Dancing Goat features that are related to the administration UI.
    /// </summary>
    public static void InitializeDancingGoat(this IApplicationBuilder builder)
    {
        InitializeCommerce(builder);
    }


    private static void InitializeCommerce(IApplicationBuilder builder)
    {
        // Register event handlers for product SKU uniqueness validation
        var productSkuValidationEventHandlers = builder.ApplicationServices.GetRequiredService<ProductSkuValidationEventHandler>();
        productSkuValidationEventHandlers.Initialize();
    }
}
