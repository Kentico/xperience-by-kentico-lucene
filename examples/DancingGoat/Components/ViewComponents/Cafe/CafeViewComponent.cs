using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Cafe view component.
    /// </summary>
    public class CafeViewComponent : ViewComponent
    {
        public ViewViewComponentResult Invoke(CafeViewModel cafe)
        {
            return View("~/Components/ViewComponents/Cafe/Default.cshtml", cafe);
        }
    }
}
