using System.Collections.Generic;
using System.Linq;

using DancingGoat.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Cafe card section view component.
    /// </summary>
    public class CafeCardSectionViewComponent : ViewComponent
    {
        public ViewViewComponentResult Invoke(IEnumerable<CafeViewModel> cafes)
        {
            return View("~/Components/ViewComponents/CafeCardSection/Default.cshtml", cafes.Take(3));
        }
    }
}
