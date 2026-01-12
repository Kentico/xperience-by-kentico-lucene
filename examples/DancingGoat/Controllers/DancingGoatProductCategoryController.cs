using System;
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

[assembly: RegisterWebPageRoute(ProductCategory.CONTENT_TYPE_NAME, typeof(DancingGoatProductCategoryController), WebsiteChannelNames = [DancingGoatConstants.WEBSITE_CHANNEL_NAME])]

namespace DancingGoat.Controllers
{
    public class DancingGoatProductCategoryController : Controller
    {
        private readonly IContentRetriever contentRetriever;
        private readonly NavigationService navigationService;
        private readonly ITaxonomyRetriever taxonomyRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly ProductRepository productRepository;
        private readonly CalculationService calculationService;

        private const string PRODUCT_CATEGORY_FIELD_NAME = "ProductFieldCategory";


        public DancingGoatProductCategoryController(
            IContentRetriever contentRetriever,
            NavigationService navigationService,
            ITaxonomyRetriever taxonomyRetriever,
            IPreferredLanguageRetriever currentLanguageRetriever,
            ProductRepository productRepository,
            CalculationService calculationService)
        {
            this.contentRetriever = contentRetriever;
            this.navigationService = navigationService;
            this.taxonomyRetriever = taxonomyRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.productRepository = productRepository;
            this.calculationService = calculationService;
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var languageName = currentLanguageRetriever.Get();

            var productCategoryPage = await contentRetriever.RetrieveCurrentPage<ProductCategory>(
                new RetrieveCurrentPageParameters { LinkedItemsMaxLevel = 1 },
                cancellationToken
            );

            var tagCollection = await TagCollection.Create(productCategoryPage.ProductCategoryTag.Select(t => t.Identifier));

            var products = await GetProductsByTags(tagCollection, cancellationToken);

            var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

            var productTagsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(DancingGoatTaxonomyConstants.PRODUCT_TAGS_TAXONOMY_NAME, languageName, cancellationToken);

            var categoryMenu = await navigationService.GetStoreNavigationItemViewModels(languageName, cancellationToken);

            var calculationResultItems = await calculationService.CalculateCatalogPrices(products, cancellationToken);

            return View(ProductListingViewModel.GetViewModel(productCategoryPage, products, calculationResultItems, productPageUrls, productTagsTaxonomy, categoryMenu, languageName));
        }


        public async Task<IEnumerable<IProductFields>> GetProductsByTags(TagCollection tagCollection, CancellationToken cancellationToken = default)
        {
            var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
                    [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
                    new RetrieveContentOfReusableSchemasParameters
                    {
                        LinkedItemsMaxLevel = 1,
                        WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME],
                        IncludeSecuredItems = User.Identity.IsAuthenticated
                    },
                    query => query.Where(where => where.WhereContainsTags(PRODUCT_CATEGORY_FIELD_NAME, tagCollection)),
                    new RetrievalCacheSettings($"WhereContainsTags_{PRODUCT_CATEGORY_FIELD_NAME}_{string.Join("_", tagCollection.TagIdentifiers)}"),
                    cancellationToken
                );

            return products;
        }
    }
}
