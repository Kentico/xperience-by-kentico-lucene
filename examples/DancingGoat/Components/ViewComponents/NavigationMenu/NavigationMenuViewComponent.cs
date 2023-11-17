using System.Threading.Tasks;

using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.Controllers
{
    public class NavigationMenuViewComponent : ViewComponent
    {
        private readonly NavigationService navigationService;


        public NavigationMenuViewComponent(NavigationService navigationService)
        {
            this.navigationService = navigationService;
        }


        public async Task<IViewComponentResult> InvokeAsync()
        {
            var navigationItems = await navigationService.GetNavigationItems();

            return View($"~/Components/ViewComponents/NavigationMenu/NavigationMenu.cshtml", navigationItems);
        }
    }
}