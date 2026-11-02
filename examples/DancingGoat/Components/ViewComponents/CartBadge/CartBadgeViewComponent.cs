using System.Linq;
using System.Threading.Tasks;

using DancingGoat.Helpers;

using Kentico.Commerce.Web.Mvc;

using Microsoft.AspNetCore.Mvc;

namespace DancingGoat.ViewComponents
{
    /// <summary>
    /// Renders the item-count badge on the header cart icon.
    /// </summary>
    public class CartBadgeViewComponent : ViewComponent
    {
        private readonly ICurrentShoppingCartRetriever currentShoppingCartRetriever;


        /// <summary>
        /// Creates an instance of <see cref="CartBadgeViewComponent"/> class.
        /// </summary>
        /// <param name="currentShoppingCartRetriever">Current shopping cart retriever.</param>
        public CartBadgeViewComponent(ICurrentShoppingCartRetriever currentShoppingCartRetriever)
        {
            this.currentShoppingCartRetriever = currentShoppingCartRetriever;
        }


        /// <summary>
        /// Renders the badge with the total number of items in the current shopping cart.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var shoppingCart = await currentShoppingCartRetriever.Get(HttpContext.RequestAborted);
            var count = shoppingCart?.GetShoppingCartDataModel().Items.Sum(item => item.Quantity) ?? 0;

            return View("~/Components/ViewComponents/CartBadge/Default.cshtml", count);
        }
    }
}
