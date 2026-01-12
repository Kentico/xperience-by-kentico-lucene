
using CMS.Commerce;

using DancingGoat.Commerce;
using DancingGoat.Models;
using DancingGoat.Services;
using DancingGoat.ViewComponents;

using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Injects DG services into the IoC container.
        /// </summary>
        public static void AddDancingGoatServices(this IServiceCollection services)
        {
            AddViewComponentServices(services);
            AddCommerceServices(services);

            services.AddSingleton<CurrentWebsiteChannelPrimaryLanguageRetriever>();
            services.AddSingleton<TagTitleRetriever>();
            services.AddSingleton<WebPageUrlProvider>();
        }


        private static void AddCommerceServices(IServiceCollection services)
        {
            services.AddSingleton<ProductSkuValidationEventHandler>();

            services.AddSingleton<OrderService>();
            services.AddSingleton<CalculationService>();
            services.AddSingleton<CustomerDataRetriever>();
            services.AddSingleton<ProductNameProvider>();
            services.AddSingleton<OrderNumberGenerator>();
            services.AddSingleton<ProductSkuValidator>();
            services.AddSingleton<ProductParametersExtractor>();
            services.AddSingleton<ProductVariantsExtractor>();
            services.AddSingleton<CountryStateRepository>();
            services.AddSingleton<ProductRepository>();
            services.AddSingleton<PaymentRepository>();
            services.AddSingleton<ShippingRepository>();
            services.AddTransient<IPriceFormatter, PriceFormatter>();

            // Register extractors for product types
            services.AddSingleton<IProductTypeParametersExtractor, ProductManufacturerExtractor>();
            services.AddSingleton<IProductTypeParametersExtractor, CoffeeParametersExtractor>();
            services.AddSingleton<IProductTypeParametersExtractor, GrinderParametersExtractor>();
            services.AddSingleton<IProductTypeParametersExtractor, ProductTemplateAlphaSizeParametersExtractor>();

            services.AddTransient(typeof(ITaxPriceCalculationStep<,>), typeof(DancingGoatTaxPriceCalculationStep<,>));
            services.AddTransient(typeof(IProductDataRetriever<,>), typeof(ProductDataRetriever<,>));

            // Register extractors for product type variants
            services.AddSingleton<IProductTypeVariantsExtractor, ProductTemplateAlphaSizeVariantsExtractor>();
        }


        private static void AddViewComponentServices(IServiceCollection services)
        {
            services.AddSingleton<NavigationService>();
        }
    }
}
