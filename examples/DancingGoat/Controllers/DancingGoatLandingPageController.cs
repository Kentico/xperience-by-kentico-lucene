using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Kentico.PageBuilder.Web.Mvc.PageTemplates;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(LandingPage.CONTENT_TYPE_NAME, typeof(DancingGoatLandingPageController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatLandingPageController : Controller
    {
        private readonly LandingPageRepository landingPageRepository;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        public DancingGoatLandingPageController(LandingPageRepository landingPageRepository, IWebPageDataContextRetriever webPageDataContextRetriever, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.landingPageRepository = landingPageRepository;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<IActionResult> Index()
        {
            var webPageItemId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;
            var languageName = currentLanguageRetriever.Get();

            var landingPage = await landingPageRepository.GetLandingPage(webPageItemId, languageName, cancellationToken: HttpContext.RequestAborted);

            return new TemplateResult(landingPage);
        }
    }
}
