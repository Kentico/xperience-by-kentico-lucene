using System.Threading.Tasks;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

[assembly: RegisterWebPageRoute(ConfirmationPage.CONTENT_TYPE_NAME, typeof(DancingGoatConfirmationController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Controllers
{
    public class DancingGoatConfirmationController : Controller
    {
        private readonly IContentRetriever contentRetriever;

        public DancingGoatConfirmationController(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }

        public async Task<IActionResult> Index()
        {
            var confirmationPage = await contentRetriever.RetrieveCurrentPage<ConfirmationPage>(
                new RetrieveCurrentPageParameters { IncludeSecuredItems = true },
                HttpContext.RequestAborted
            );

            return View(ConfirmationPageViewModel.GetViewModel(confirmationPage));
        }
    }
}
