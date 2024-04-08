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


        public const string HOME_PAGE_PATH = "/Home";


        public const string WEBSITE_CHANNEL_NAME = "DancingGoatPages";


        public const string NAVIGATION_MENU_FOLDER_PATH = "/Navigation_menu";
    }
}
