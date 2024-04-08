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
        private readonly ConfirmationPageRepository confirmationPageRepository;
        private readonly IWebPageDataContextRetriever webPageDataContextRetriever;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;

        public DancingGoatConfirmationController(ConfirmationPageRepository confirmationPageRepository, IWebPageDataContextRetriever webPageDataContextRetriever, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.confirmationPageRepository = confirmationPageRepository;
            this.webPageDataContextRetriever = webPageDataContextRetriever;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<IActionResult> Index()
        {
            var webPageItemId = webPageDataContextRetriever.Retrieve().WebPage.WebPageItemID;
            var languageName = currentLanguageRetriever.Get();

            var confirmationPage = await confirmationPageRepository.GetConfirmationPage(webPageItemId, languageName, cancellationToken: HttpContext.RequestAborted);

            return View(ConfirmationPageViewModel.GetViewModel(confirmationPage));
        }
    }
}
