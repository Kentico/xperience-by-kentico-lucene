using System.Threading;
using System.Threading.Tasks;

using CMS.Websites.Routing;

using DancingGoat;
using DancingGoat.Controllers;
using DancingGoat.Models;
using DancingGoat.Services;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

[assembly: RegisterWebPageRoute(ProductsSection.CONTENT_TYPE_NAME, typeof(DancingGoatProductSectionController), WebsiteChannelNames = [DancingGoatConstants.WEBSITE_CHANNEL_NAME])]

namespace DancingGoat.Controllers
{
    public class DancingGoatProductSectionController : Controller
    {
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly WebPageUrlProvider webPageUrlProvider;
        private readonly IStringLocalizer<SharedResources> localizer;


        public DancingGoatProductSectionController(
            WebPageUrlProvider webPageUrlProvider,
            IWebsiteChannelContext websiteChannelContext,
            IStringLocalizer<SharedResources> localizer)
        {
            this.webPageUrlProvider = webPageUrlProvider;
            this.websiteChannelContext = websiteChannelContext;
            this.localizer = localizer;
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (websiteChannelContext.IsPreview)
            {
                return Content(localizer["Redirection to the Store page when on the live site."]);
            }

            var storePageUrl = await webPageUrlProvider.StorePageUrl(cancellationToken: cancellationToken);

            // Redirect to the store page
            return Redirect(storePageUrl);
        }
    }
}
