using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Websites;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ProductsSection.CONTENT_TYPE_NAME, typeof(DancingGoatProductController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatProductController : Controller
    {
        private readonly ProductSectionRepository productSectionRepository;
        private readonly ProductPageRepository productPageRepository;
        private readonly ProductRepository productRepository;
        private readonly ITaxonomyRetriever taxonomyRetriever;
        private readonly IWebPageUrlRetriever urlRetriever;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        public DancingGoatProductController(
            ProductSectionRepository productSectionRepository,
            ProductPageRepository productPageRepository,
            ProductRepository productRepository,
            IWebPageUrlRetriever urlRetriever,
            IPreferredLanguageRetriever currentLanguageRetriever,
            IWebPageDataContextRetriever webPageDataContextRetriever,
            ITaxonomyRetriever taxonomyRetriever)
        {
            this.productSectionRepository = productSectionRepository;
            this.productPageRepository = productPageRepository;
            this.productRepository = productRepository;
            this.urlRetriever = urlRetriever;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.taxonomyRetriever = taxonomyRetriever;
        }


        public async Task<IActionResult> Index()
        {
            var languageName = currentLanguageRetriever.Get();
            var webPage = webPageDataContextRetriever.Retrieve().WebPage;
            var productsSection = await productSectionRepository.GetProductsSection(webPage.WebPageItemID, languageName, HttpContext.RequestAborted);

            var products = await GetProducts(languageName, productsSection);

            var taxonomies = new Dictionary<string, TaxonomyViewModel>();
            var taxonomyNames = new List<string> { "CoffeeProcessing", "CoffeeTastes", "GrinderManufacturer", "GrinderType" };
            foreach (var taxonomyName in taxonomyNames)
            {
                var taxonomy = await taxonomyRetriever.RetrieveTaxonomy(taxonomyName, languageName);
                if (taxonomy.Tags.Any())
                {
                    taxonomies.Add(taxonomyName, TaxonomyViewModel.GetViewModel(taxonomy));
                }
            }

            var listModel = new ProductListViewModel(products, taxonomies);

            return View(listModel);
        }


        [HttpPost($"{{{WebPageRoutingOptions.LANGUAGE_ROUTE_VALUE_KEY}}}/{{controller}}/{{action}}")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Filter(IDictionary<string, TaxonomyViewModel> filter)
        {
            var languageName = currentLanguageRetriever.Get();
            var webPage = webPageDataContextRetriever.Retrieve().WebPage;
            var productsSection = await productSectionRepository.GetProductsSection(webPage.WebPageItemID, languageName, HttpContext.RequestAborted);

            var products = await GetProducts(languageName, productsSection, filter);
            return PartialView("ProductsList", products);
        }


        private async Task<IEnumerable<ProductListItemViewModel>> GetProducts(string languageName, ProductsSection productsSection, IDictionary<string, TaxonomyViewModel> filter = null)
        {
            var products = await productRepository.GetProducts(languageName, filter ?? new Dictionary<string, TaxonomyViewModel>(), cancellationToken: HttpContext.RequestAborted);
            var productPages = await productPageRepository.GetProducts(productsSection.SystemFields.WebPageItemTreePath, languageName, products, cancellationToken: HttpContext.RequestAborted);

            return productPages.Select(productPage => ProductListItemViewModel.GetViewModel(productPage, urlRetriever, languageName).Result);
        }
    }
}
