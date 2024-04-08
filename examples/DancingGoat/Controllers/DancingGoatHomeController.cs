using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(HomePage.CONTENT_TYPE_NAME, typeof(DancingGoatHomeController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatHomeController : Controller
    {
        private readonly HomePageRepository homePageRepository;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;

        public DancingGoatHomeController(HomePageRepository homePageRepository, IWebPageDataContextRetriever webPageDataContextRetriever)
        {
            this.homePageRepository = homePageRepository;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
        }


        public async Task<IActionResult> Index()
        {
            var webPage = webPageDataContextRetriever.Retrieve().WebPage;

            var homePage = await homePageRepository.GetHomePage(webPage.WebPageItemID, webPage.LanguageName, HttpContext.RequestAborted);

            return View(HomePageViewModel.GetViewModel(homePage));
        }
    }
}
