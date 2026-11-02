using CMS.Base;
using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat.Commerce;
using DancingGoat.Models;
using DancingGoat.Services;
using DancingGoat.ViewComponents;

using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Injects DG services into the IoC container.
        /// </summary>
        public static void AddDancingGoatServices(this IServiceCollection services)
        {
            AddViewComponentServices(services);
            AddCommerceServices(services);
            AddSeoServices(services);

            services.AddSingleton<CurrentWebsiteChannelPrimaryLanguageRetriever>();
            services.AddSingleton<TagRetriever>();
            services.AddSingleton<WebPageUrlProvider>();
        }


        private static void AddCommerceServices(IServiceCollection services)
        {
            services.AddEventHandler<BeforeCreateContentItemEvent, ProductSkuValidationEventHandler>();
            services.AddEventHandler<BeforeUpdateDraftEvent, ProductSkuValidationEventHandler>();

            services.AddSingleton<OrderService>();
            services.AddSingleton<CalculationService>();
            services.AddSingleton<CustomerDataRetriever>();
            services.AddSingleton<BuyerIdentifierRetriever>();
            services.AddSingleton<ProductNameProvider>();
            services.AddSingleton<OrderNumberGenerator>();
            services.AddSingleton<ProductSkuValidator>();
            services.AddSingleton<ProductParametersExtractor>();
            services.AddSingleton<ProductVariantsExtractor>();
            services.AddSingleton<CountryStateRepository>();
            services.AddSingleton<ProductRepository>();
            services.AddSingleton<PaymentRepository>();
            services.AddSingleton<PromotionCouponRepository>();
            services.AddSingleton<PromotionRepository>();
            services.AddSingleton<PromotionBreakdownProvider>();
            services.AddSingleton<PriceCalculationTotalsCalculator>();
            services.AddSingleton<ShippingRepository>();
            services.AddTransient<UpsellOrderDiscountService>();
            services.AddTransient<UpsellFreeShippingService>();
            services.AddTransient<FreeShippingEligibilityService>();
            services.AddTransient<CartItemDiscountEligibilityService>();
            services.AddTransient<ShippingMethodOptionsService>();
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


        private static void AddSeoServices(IServiceCollection services)
        {
            services.AddSingleton<SeoMetadataService>();

            services.AddSingleton<IJsonLdGenerator, ProductJsonLdGenerator>();
            services.AddSingleton<IJsonLdGenerator, ArticleJsonLdGenerator>();
            services.AddSingleton<IJsonLdGenerator, WebSiteJsonLdGenerator>();
            services.AddSingleton<IJsonLdGenerator, BreadcrumbJsonLdGenerator>();
        }
    }
}
