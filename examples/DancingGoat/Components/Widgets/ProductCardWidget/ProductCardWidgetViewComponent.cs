using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Commerce;
using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(ProductCardWidgetViewComponent.IDENTIFIER, typeof(ProductCardWidgetViewComponent), "{$dancinggoat.productcardwidget.title$}", typeof(ProductCardProperties), Description = "{$dancinggoat.productcardwidget.description$}", IconClass = "icon-box")]

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


        private readonly IContentRetriever contentRetriever;
        private readonly ProductRepository productRepository;


        /// <summary>
        /// Creates an instance of <see cref="ProductCardWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        /// <param name="productRepository">Repository providing product page URLs.</param>
        public ProductCardWidgetViewComponent(IContentRetriever contentRetriever, ProductRepository productRepository)
        {
            this.contentRetriever = contentRetriever;
            this.productRepository = productRepository;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(ProductCardProperties properties, CancellationToken cancellationToken)
        {
            var selectedProductGuids = properties.SelectedProducts.Select(i => i.Identifier).ToList();

            var products = await contentRetriever.RetrieveContentOfReusableSchemas<IProductFields>(
                [IProductFields.REUSABLE_FIELD_SCHEMA_NAME],
                new RetrieveContentOfReusableSchemasParameters
                {
                    LinkedItemsMaxLevel = 1,
                    WorkspaceNames = [DancingGoatConstants.COMMERCE_WORKSPACE_NAME]
                },
                query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), selectedProductGuids)),
                new RetrievalCacheSettings($"WhereIn_{nameof(IContentQueryDataContainer.ContentItemGUID)}_{string.Join("_", selectedProductGuids)}"),
                cancellationToken
            );

            var orderedProducts = products.OrderBy(p => selectedProductGuids.IndexOf(((IContentItemFieldsSource)p).SystemFields.ContentItemGUID));
            var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);
            var model = ProductCardListViewModel.GetViewModel(orderedProducts, productPageUrls);

            return View("~/Components/Widgets/ProductCardWidget/_ProductCardWidget.cshtml", model);
        }
    }
}
