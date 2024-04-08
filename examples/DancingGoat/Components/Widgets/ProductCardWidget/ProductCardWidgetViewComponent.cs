using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc.Routing;
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
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        /// <summary>
        /// Creates an instance of <see cref="ProductCardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="repository">Repository for retrieving products.</param>
        /// <param name="currentLanguageRetriever">Retrieves preferred language name for the current request. Takes language fallback into account.</param>
        public ProductCardWidgetViewComponent(CoffeeRepository repository, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.repository = repository;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(ProductCardProperties properties)
        {
            var languageName = currentLanguageRetriever.Get();
            var selectedProductGuids = properties.SelectedProducts.Select(i => i.Identifier).ToList();
            IEnumerable<Coffee> products = (await repository.GetCoffees(selectedProductGuids, languageName))
                                                     .OrderBy(p => selectedProductGuids.IndexOf(p.SystemFields.ContentItemGUID));
            var model = ProductCardListViewModel.GetViewModel(products);

            return View("~/Components/Widgets/ProductCardWidget/_ProductCardWidget.cshtml", model);
        }
    }
}
