using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat;
using DancingGoat.Commerce;
using DancingGoat.Controllers;
using DancingGoat.Models;
using DancingGoat.ViewComponents;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

using static DancingGoat.ProductTagTaxonomyConstants;

[assembly: RegisterWebPageRoute(Store.CONTENT_TYPE_NAME, typeof(DancingGoatStoreController), WebsiteChannelNames = [DancingGoatConstants.WEBSITE_CHANNEL_NAME])]

namespace DancingGoat.Controllers
{
    public class DancingGoatStoreController : Controller
    {
        private readonly IContentRetriever contentRetriever;
        private readonly NavigationService navigationService;
        private readonly ITaxonomyRetriever taxonomyRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly ProductRepository productRepository;
        private readonly CalculationService calculationService;
        private readonly FreeShippingEligibilityService freeShippingEligibilityService;
        private readonly CartItemDiscountEligibilityService cartItemDiscountEligibilityService;
        private readonly ProductVariantsExtractor productVariantsExtractor;


        private const string PRODUCT_TAGS_FIELD_NAME = "ProductFieldTags";
        private readonly string[] PRODUCT_TAGS_TO_DISPLAY = [TAG_NAME_RECOMMENDED, TAG_NAME_BESTSELLER, TAG_NAME_HOT_TIPS];


        public DancingGoatStoreController(IContentRetriever contentRetriever, NavigationService navigationService,
            ITaxonomyRetriever taxonomyRetriever, IPreferredLanguageRetriever currentLanguageRetriever,
            ProductRepository productRepository, CalculationService calculationService,
            FreeShippingEligibilityService freeShippingEligibilityService,
            CartItemDiscountEligibilityService cartItemDiscountEligibilityService,
            ProductVariantsExtractor productVariantsExtractor)
        {
            this.contentRetriever = contentRetriever;
            this.navigationService = navigationService;
            this.taxonomyRetriever = taxonomyRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.productRepository = productRepository;
            this.calculationService = calculationService;
            this.freeShippingEligibilityService = freeShippingEligibilityService;
            this.cartItemDiscountEligibilityService = cartItemDiscountEligibilityService;
            this.productVariantsExtractor = productVariantsExtractor;
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var storePage = await contentRetriever.RetrieveCurrentPage<Store>(cancellationToken);
            var languageName = currentLanguageRetriever.Get();

            var tagCollection = await TagCollection.Create(PRODUCT_TAGS_TO_DISPLAY);
            var products = await productRepository.GetProductsByTags(PRODUCT_TAGS_FIELD_NAME, tagCollection, User.Identity.IsAuthenticated, cancellationToken);

            var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

            var categoryMenu = await navigationService.GetStoreNavigationItemViewModels(languageName, cancellationToken);

            var productTagsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(DancingGoatTaxonomyConstants.PRODUCT_TAGS_TAXONOMY_NAME, languageName, cancellationToken);

            var calculationResultItems = await calculationService.CalculateCatalogPrices(products, cancellationToken);

            var productIdsWithVariants = productVariantsExtractor.GetProductIdsWithVariants(products);

            var freeShippingProductIds = await freeShippingEligibilityService.GetFreeShippingEligibleProductIds(calculationResultItems, cancellationToken);

            var specialOfferProductIds = await cartItemDiscountEligibilityService.GetSpecialOfferProductIds(calculationResultItems, cancellationToken);

            return View(StoreViewModel.GetViewModel(storePage, products, calculationResultItems, productPageUrls, PRODUCT_TAGS_TO_DISPLAY, productTagsTaxonomy, languageName, categoryMenu, productIdsWithVariants, freeShippingProductIds, specialOfferProductIds));
        }
    }
}
