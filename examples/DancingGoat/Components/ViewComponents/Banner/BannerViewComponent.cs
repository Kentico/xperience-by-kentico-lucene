using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Banner view component.
    /// </summary>
    public class BannerViewComponent : ViewComponent
    {
        public ViewViewComponentResult Invoke(BannerViewModel banner)
        {
            return View("~/Components/ViewComponents/Banner/Default.cshtml", banner);
        }
    }
}
