using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

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


        public ViewViewComponentResult Invoke(ProductCardProperties properties)
        {
            var selectedProductIds = properties.SelectedProducts.Select(i => i.ItemId).ToList();
            IEnumerable<Coffee> products = repository.Get(selectedProductIds)
                                                     .OrderBy(p => selectedProductIds.IndexOf(p.CoffeeID));
            var model = ProductCardListViewModel.GetViewModel(products);
            return View("~/Components/Widgets/ProductCardWidget/_ProductCardWidget.cshtml", model);
        }
    }
}