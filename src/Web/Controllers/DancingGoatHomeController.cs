using System.Threading.Tasks;

using DancingGoat.Controllers;
using DancingGoat.Helpers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(HomePage.CONTENT_TYPE_NAME, typeof(DancingGoatHomeController), WebsiteChannelNames = new[] { WebsiteConstants.WEBSITE_CHANNEL_NAME })]

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
            var languageName = webPageDataContextRetriever.Retrieve().WebPage.LanguageName;

            var homePage = await homePageRepository.GetHomePage(languageName, HttpContext.RequestAborted);

            return View(HomePageViewModel.GetViewModel(homePage));
        }
    }
}