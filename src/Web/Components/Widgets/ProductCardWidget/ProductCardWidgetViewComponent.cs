using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(ProductCardWidgetViewComponent.IDENTIFIER, typeof(ProductCardWidgetViewComponent), "Product cards", typeof(ProductCardProperties), Description = "Displays products.", IconClass = "icon-box")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Controller for product card widget.
    /// </summary>
    public class ProductCardWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.LandingPage.ProductCardWidget";


        private readonly CoffeeRepository repository;


        /// <summary>
        /// Creates an instance of <see cref="ProductCardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="repository">Repository for retrieving products.</param>
        public ProductCardWidgetViewComponent(CoffeeRepository repository)
        {
            this.repository = repository;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(ProductCardProperties properties)
        {
            var selectedProductGuids = properties.SelectedProducts.Select(i => i.Identifier).ToList();
            IEnumerable<Coffee> products = (await repository.GetCoffees(selectedProductGuids))
                                                     .OrderBy(p => selectedProductGuids.IndexOf(p.SystemFields.ContentItemGUID));
            var model = ProductCardListViewModel.GetViewModel(products);

            return View("~/Components/Widgets/ProductCardWidget/_ProductCardWidget.cshtml", model);
        }
    }
}