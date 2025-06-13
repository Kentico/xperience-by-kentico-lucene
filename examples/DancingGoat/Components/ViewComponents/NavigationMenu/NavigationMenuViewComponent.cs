using System.Threading.Tasks;

using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    public class NavigationMenuViewComponent : ViewComponent
    {
        private readonly NavigationService navigationService;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;

        public NavigationMenuViewComponent(NavigationService navigationService, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.navigationService = navigationService;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var languageName = currentLanguageRetriever.Get();

            var navigationViewModels = await navigationService.GetSiteNavigationItemViewModels(languageName, HttpContext.RequestAborted);

            return View($"~/Components/ViewComponents/NavigationMenu/Default.cshtml", navigationViewModels);
        }
    }
}
