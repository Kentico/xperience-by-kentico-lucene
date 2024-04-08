using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using CMS.ContentEngine;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

[assembly: RegisterWebPageRoute(CoffeePage.CONTENT_TYPE_NAME, typeof(DancingGoatCoffeeController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME }, ActionName = nameof(DancingGoatCoffeeController.Detail))]

namespace DancingGoat.Controllers
{
    public class DancingGoatCoffeeController : Controller
    {
        private readonly ProductPageRepository productPageRepository;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly ITaxonomyRetriever taxonomyRetriever;


        public DancingGoatCoffeeController(ProductPageRepository productPageRepository, 
            IWebPageDataContextRetriever webPageDataContextRetriever, 
            IPreferredLanguageRetriever currentLanguageRetriever,
            ITaxonomyRetriever taxonomyRetriever)
        {
            this.productPageRepository = productPageRepository;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.taxonomyRetriever = taxonomyRetriever;
        }


        public async Task<IActionResult> Detail()
        {
            var languageName = currentLanguageRetriever.Get();
            var webPageItemId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;

            var coffee = await productPageRepository.GetProduct<CoffeePage>(CoffeePage.CONTENT_TYPE_NAME, webPageItemId, languageName, cancellationToken: HttpContext.RequestAborted);

            return View(await CoffeeDetailViewModel.GetViewModel(coffee, languageName, taxonomyRetriever));
        }
    }
}
