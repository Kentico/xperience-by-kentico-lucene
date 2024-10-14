using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;

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


        private readonly ProductRepository repository;
        private readonly IPreferredLanguageRetriever currentLanguageRetriever;


        /// <summary>
        /// Creates an instance of <see cref="ProductCardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="repository">Repository for retrieving products.</param>
        /// <param name="currentLanguageRetriever">Retrieves preferred language name for the current request. Takes language fallback into account.</param>
        public ProductCardWidgetViewComponent(ProductRepository repository, IPreferredLanguageRetriever currentLanguageRetriever)
        {
            this.repository = repository;
            this.currentLanguageRetriever = currentLanguageRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(ProductCardProperties properties)
        {
            var languageName = currentLanguageRetriever.Get();
            var selectedProductGuids = properties.SelectedProducts.Select(i => i.Identifier).ToList();
            var products = (await repository.GetProducts(selectedProductGuids, languageName))
                                            .OrderBy(p => selectedProductGuids.IndexOf(((IContentItemFieldsSource)p).SystemFields.ContentItemGUID));
            var model = ProductCardListViewModel.GetViewModel(products);

            return View("~/Components/Widgets/ProductCardWidget/_ProductCardWidget.cshtml", model);
        }
    }
}
