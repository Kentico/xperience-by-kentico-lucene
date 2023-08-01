using System.Linq;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(CafeCardWidgetViewComponent.IDENTIFIER, typeof(CafeCardWidgetViewComponent), "Cafe", typeof(CafeCardProperties), Description = "Displays a cafe with its background image.", IconClass = "icon-cup")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for cafe card widget.
    /// </summary>
    public class CafeCardWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.HomePage.CafeCardWidget";


        private readonly CafeRepository repository;

        /// <summary>
        /// Initializes an instance of <see cref="CafeCardWidgetViewComponent"/> class.
        /// </summary>
        public CafeCardWidgetViewComponent(
            CafeRepository repository)
        {
            this.repository = repository;
        }


        public ViewViewComponentResult Invoke(CafeCardProperties properties)
        {
            var selectedPage = properties.SelectedCafes.FirstOrDefault();
            var cafe = (selectedPage != null) ? repository.GetCafeByNodeId(selectedPage.ItemId) : null;
            var cafeModel = CafeCardViewModel.GetViewModel(cafe);

            return View("~/Components/Widgets/CafeCardWidget/_CafeCardWidget.cshtml", cafeModel);
        }
    }
}