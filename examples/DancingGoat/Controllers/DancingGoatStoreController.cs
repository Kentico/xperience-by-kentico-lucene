using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat;
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


        private const string PRODUCT_TAGS_FIELD_NAME = "ProductFieldTags";
        private readonly string[] PRODUCT_TAGS_TO_DISPLAY = [TAG_NAME_BESTSELLER, TAG_NAME_HOT_TIPS];


        public DancingGoatStoreController(IContentRetriever contentRetriever, NavigationService navigationService,
            ITaxonomyRetriever taxonomyRetriever, IPreferredLanguageRetriever currentLanguageRetriever,
            ProductRepository productRepository)
        {
            this.contentRetriever = contentRetriever;
            this.navigationService = navigationService;
            this.taxonomyRetriever = taxonomyRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.productRepository = productRepository;
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var storePage = await contentRetriever.RetrieveCurrentPage<Store>(cancellationToken);
            var languageName = currentLanguageRetriever.Get();

            var tagCollection = await TagCollection.Create(PRODUCT_TAGS_TO_DISPLAY);
            var products = await GetProductsByTags(tagCollection, cancellationToken);

            var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

            var categoryMenu = await navigationService.GetStoreNavigationItemViewModels(languageName, cancellationToken);

            var productTagsTaxonomy = await taxonomyRetriever.RetrieveTaxonomy(DancingGoatTaxonomyConstants.PRODUCT_TAGS_TAXONOMY_NAME, languageName, cancellationToken);

            return View(StoreViewModel.GetViewModel(storePage, products, productPageUrls, PRODUCT_TAGS_TO_DISPLAY, productTagsTaxonomy, languageName, categoryMenu));
        }


        private async Task<IEnumerable<IProductFields>> GetProductsByTags(TagCollection tagCollection, CancellationToken cancellationToken = default)
        {
            var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
                [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
                new RetrieveContentOfReusableSchemasParameters
                {
                    LinkedItemsMaxLevel = 1,
                    WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME]
                },
                query => query.Where(where => where.WhereContainsTags(PRODUCT_TAGS_FIELD_NAME, tagCollection)),
                new RetrievalCacheSettings($"WhereContainsTags_{PRODUCT_TAGS_FIELD_NAME}_{string.Join("_", PRODUCT_TAGS_TO_DISPLAY)}"),
                cancellationToken
            );

            return products;
        }
    }
}
