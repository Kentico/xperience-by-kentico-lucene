namespace DancingGoat
{
    internal static class DancingGoatConstants
    {
        /// <summary>
        /// This is a route controller constraint for pages not handled by the content tree-based router.
        /// The constraint limits the match to a list of specified controllers for pages not handled by the content tree-based router.
        /// The constraint ensures that broken URLs lead to a "404 page not found" page and are not handled by a controller dedicated to the component or
        /// to a page handled by the content tree-based router (which would lead to an exception).
        /// </summary>
        public const string CONSTRAINT_FOR_NON_ROUTER_PAGE_CONTROLLERS = "Account|Consent|SiteMap";


        public const string DEFAULT_ROUTE_NAME = "default";


        public const string DEFAULT_ROUTE_WITHOUT_LANGUAGE_PREFIX_NAME = "defaultWithoutLanguagePrefix";


        public const string WEBSITE_CHANNEL_NAME = "DancingGoatPages";


        public const string COMMERCE_WORKSPACE_NAME = "DancingGoat.DancingGoatCommerce";


        public const string HOME_PAGE_TREE_PATH = "/Home";


        public const string SITE_NAVIGATION_MENU_TREE_PATH = "/Navigation_menu";


        public const string STORE_NAVIGATION_MENU_TREE_PATH = "/Navigation_menu/Store";


        public const string PRODUCTS_PAGE_TREE_PATH = "/Products";


        public const string STORE_PAGE_TREE_PATH = "/Store";


        public const string SHOPPING_CART_PAGE_TREE_PATH = "/Specials/ShoppingCart";


        public const string CHECKOUT_PAGE_TREE_PATH = "/Specials/Checkout";
    }
}
