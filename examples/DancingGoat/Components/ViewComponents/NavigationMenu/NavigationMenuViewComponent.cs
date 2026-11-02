using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Services;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Renders the navigation menu for the current request.
    /// </summary>
    public class NavigationMenuViewComponent : ViewComponent
    {
        private readonly NavigationService navigationService;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;
        private readonly WebPageUrlProvider webPageUrlProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationMenuViewComponent"/> class.
        /// </summary>
        public NavigationMenuViewComponent(NavigationService navigationService, IPreferredLanguageRetriever currentLanguageRetriever, WebPageUrlProvider webPageUrlProvider)
        {
            this.navigationService = navigationService;
            this.currentLanguageRetriever = currentLanguageRetriever;
            this.webPageUrlProvider = webPageUrlProvider;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var languageName = currentLanguageRetriever.Get();

            var navigationViewModels = (await navigationService.GetSiteNavigationItemViewModels(languageName, HttpContext.RequestAborted)).ToList();

            var rootPath = NavigationActivePathResolver.Normalize(await webPageUrlProvider.HomePageUrl(languageName, HttpContext.RequestAborted));
            var currentPath = NavigationActivePathResolver.Normalize(HttpContext.Request.Path.Value);

            var activePath = NavigationActivePathResolver.GetActivePath(navigationViewModels.Select(navigationItem => navigationItem.ResolvedPath), currentPath, rootPath);

            return View($"~/Components/ViewComponents/NavigationMenu/Default.cshtml", new NavigationMenuViewModel(navigationViewModels, activePath));
        }
    }
}
