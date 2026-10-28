using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat;
using DancingGoat.Commerce;
using DancingGoat.Controllers;
using DancingGoat.Models;
using DancingGoat.Services;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ProductPage.CONTENT_TYPE_NAME, typeof(DancingGoatProductDetailController), WebsiteChannelNames = [DancingGoatConstants.WEBSITE_CHANNEL_NAME])]

namespace DancingGoat.Controllers
{
    public class DancingGoatProductDetailController : Controller
    {
        private readonly IContentRetriever contentRetriever;
        private readonly ProductParametersExtractor productParametersExtractor;
        private readonly ProductVariantsExtractor productVariantsExtractor;
        private readonly TagRetriever tagRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly CalculationService calculationService;
        private readonly FreeShippingEligibilityService freeShippingEligibilityService;
        private readonly CartItemDiscountEligibilityService cartItemDiscountEligibilityService;


        public DancingGoatProductDetailController(
            IContentRetriever contentRetriever,
            ProductParametersExtractor productParametersExtractor,
            ProductVariantsExtractor productVariantsExtractor,
            TagRetriever tagRetriever,
            IPreferredLanguageRetriever currentLanguageRetriever,
            CalculationService calculationService,
            FreeShippingEligibilityService freeShippingEligibilityService,
            CartItemDiscountEligibilityService cartItemDiscountEligibilityService)
        {
            this.contentRetriever = contentRetriever;
            this.productParametersExtractor = productParametersExtractor;
            this.productVariantsExtractor = productVariantsExtractor;
            this.tagRetriever = tagRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.calculationService = calculationService;
            this.freeShippingEligibilityService = freeShippingEligibilityService;
            this.cartItemDiscountEligibilityService = cartItemDiscountEligibilityService;
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var languageName = currentLanguageRetriever.Get();
            var productPage = await contentRetriever.RetrieveCurrentPage<ProductPage>(
                new RetrieveCurrentPageParameters
                {
                    LinkedItemsMaxLevel = 2,
                    IncludeSecuredItems = User.Identity.IsAuthenticated
                },
                cancellationToken
            );

            if (productPage == null || !productPage.ProductPageProduct.Any())
            {
                return NotFound();
            }

            var productItem = productPage.ProductPageProduct.FirstOrDefault() as IProductFields;

            var productTag = productItem.ProductFieldTags.Any() ? await tagRetriever.GetTag(productItem.ProductFieldTags.First().Identifier, languageName, cancellationToken) : null;
            var tag = ProductListItemTagViewModel.GetViewModel(productTag?.Title);

            var parameters = await productParametersExtractor.ExtractParameters(productItem, languageName, cancellationToken);

            var variantValues = productVariantsExtractor.ExtractVariantsValue(productItem);

            var variantSkuCodes = productVariantsExtractor.ExtractVariantsSKUCode(productItem);

            var categoryTags = await tagRetriever.GetTags(productItem.ProductFieldCategory.Select(category => category.Identifier), languageName, cancellationToken);
            bool isMerchandise = categoryTags.Any(categoryTag => string.Equals(categoryTag.Name, DancingGoatTaxonomyConstants.MERCHANDISE_CATEGORY_TAG_NAME, StringComparison.Ordinal));

            int contentItemId = (productItem as IContentItemFieldsSource).SystemFields.ContentItemID;

            var calculationResultItem = (await calculationService.CalculateCatalogPrices([productItem], cancellationToken)).First();

            var appliedCandidate = calculationResultItem.PromotionData.CatalogPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionCandidate as DancingGoatCatalogPromotionCandidate;

            var freeShippingPromotions = await freeShippingEligibilityService.GetFreeShippingPromotions(calculationResultItem, cancellationToken);

            var cartItemDiscounts = await cartItemDiscountEligibilityService.GetCartItemDiscounts(calculationResultItem, cancellationToken);

            ViewBag.Title = productItem.ProductFieldName;

            return View(new ProductViewModel(productItem.ProductFieldName, productItem.ProductFieldDescription, productItem.ProductFieldImage.FirstOrDefault()?.ImageFile.Url, calculationResultItem.LineSubtotalAfterLineDiscount, productItem.ProductFieldPrice, appliedCandidate, tag, contentItemId, parameters, variantValues, (productItem as IProductSKU)?.ProductSKUCode, variantSkuCodes, isMerchandise, freeShippingPromotions, cartItemDiscounts)
            {
                WebPage = productPage
            });
        }
    }
}
