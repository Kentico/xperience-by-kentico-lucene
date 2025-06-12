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
        private readonly IContentRetriever contentRetriever;

        public DancingGoatLandingPageController(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }

        public async Task<IActionResult> Index()
        {
            var landingPage = await contentRetriever.RetrieveCurrentPage<LandingPage>(HttpContext.RequestAborted);
            return new TemplateResult(landingPage);
        }
    }
}
