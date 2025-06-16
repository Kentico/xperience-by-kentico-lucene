using System.Threading;
using System.Threading.Tasks;

using CMS.Websites;
using CMS.Websites.Routing;

using Kentico.Content.Web.Mvc.Routing;

namespace DancingGoat.Services
{
    /// <summary>
    /// Provides URLs of the web pages in the Dancing Goat sample application.
    /// </summary>
    public sealed class WebPageUrlProvider
    {
        private readonly IWebPageUrlRetriever webPageUrlRetriever;
        private readonly IWebsiteChannelContext websiteChannelContext;
        private readonly IPreferredLanguageRetriever preferredLanguageRetriever;


        public WebPageUrlProvider(IWebPageUrlRetriever webPageUrlRetriever, IWebsiteChannelContext websiteChannelContext, IPreferredLanguageRetriever preferredLanguageRetriever)
        {
            this.webPageUrlRetriever = webPageUrlRetriever;
            this.websiteChannelContext = websiteChannelContext;
            this.preferredLanguageRetriever = preferredLanguageRetriever;
        }


        public async Task<string> StorePageUrl(string languageName = null, CancellationToken cancellationToken = default)
        {
            return await GetRelativeWebPagePath(DancingGoatConstants.STORE_PAGE_TREE_PATH, languageName, cancellationToken);
        }


        public async Task<string> ShoppingCartPageUrl(string languageName = null, CancellationToken cancellationToken = default)
        {
            return await GetRelativeWebPagePath(DancingGoatConstants.SHOPPING_CART_PAGE_TREE_PATH, languageName, cancellationToken);
        }


        public async Task<string> CheckoutPageUrl(string languageName = null, CancellationToken cancellationToken = default)
        {
            return await GetRelativeWebPagePath(DancingGoatConstants.CHECKOUT_PAGE_TREE_PATH, languageName, cancellationToken);
        }


        private async Task<string> GetRelativeWebPagePath(string webPageTreePath, string languageName, CancellationToken cancellationToken)
        {
            languageName ??= preferredLanguageRetriever.Get();

            return (await webPageUrlRetriever.Retrieve(webPageTreePath, websiteChannelContext.WebsiteChannelName, languageName, websiteChannelContext.IsPreview, cancellationToken)).RelativePath;
        }
    }
}
