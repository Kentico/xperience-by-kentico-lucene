using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of product pages.
    /// </summary>
    public class ProductPageRepository : ContentRepositoryBase
    {
        private readonly IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever;


        /// <summary>
        /// Initializes new instance of <see cref="ProductPageRepository"/>.
        /// </summary>
        public ProductPageRepository(
            IWebsiteChannelContext websiteChannelContext,
            IContentQueryExecutor executor,
            IProgressiveCache cache,
            IWebPageLinkedItemsDependencyAsyncRetriever webPageLinkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, cache)
        {
            this.webPageLinkedItemsDependencyRetriever = webPageLinkedItemsDependencyRetriever;
        }


        /// <summary>
        /// Returns list of <see cref="IProductPage"/> web pages.
        /// </summary>
        public async Task<IEnumerable<IProductPage>> GetProducts(string treePath, string languageName, IEnumerable<IProductFields> linkedProducts, bool includeSecuredItems = true, CancellationToken cancellationToken = default)
        {
            if (!linkedProducts.Any())
            {
                return Enumerable.Empty<IProductPage>();
            }

            var queryBuilder = GetQueryBuilder(treePath, languageName, linkedProducts);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var linkedProductCacheParts = linkedProducts.Select(product => product.ProductFieldsName).Join("|");
            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, treePath, languageName, includeSecuredItems, nameof(IProductPage), linkedProductCacheParts);

            return await GetCachedQueryResult<IProductPage>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);
        }


        public async Task<ProductPageType> GetProduct<ProductPageType>(string contentTypeName, int id, string languageName, bool includeSecuredItems = true, CancellationToken cancellationToken = default)
            where ProductPageType : IWebPageFieldsSource, new()
        {
            var queryBuilder = GetQueryBuilder(id, languageName, contentTypeName);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ProductPageType), id, languageName);

            var result = await GetCachedQueryResult<ProductPageType>(queryBuilder, options, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private ContentItemQueryBuilder GetQueryBuilder(string treePath, string languageName, IEnumerable<IProductFields> linkedProducts)
        {
            return GetQueryBuilder(
                languageName,
                config => config
                    .Linking(nameof(IProductPage.RelatedItem), linkedProducts.Select(linkedProduct => ((IContentItemFieldsSource)linkedProduct).SystemFields.ContentItemID))
                    .WithLinkedItems(2)
                    .ForWebsite(WebsiteChannelContext.WebsiteChannelName, PathMatch.Children(treePath)));
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName, Action<ContentTypeQueryParameters> configure = null)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(CoffeePage.CONTENT_TYPE_NAME, configure)
                    .ForContentType(GrinderPage.CONTENT_TYPE_NAME, configure)
                    .InLanguage(languageName);
        }


        private ContentItemQueryBuilder GetQueryBuilder(int id, string languageName, string contentTypeName)
        {
            return GetQueryBuilder(
                languageName,
                contentTypeName,
                config => config
                    .WithLinkedItems(2)
                    .ForWebsite(WebsiteChannelContext.WebsiteChannelName)
                    .Where(where => where.WhereEquals(nameof(IWebPageContentQueryDataContainer.WebPageItemID), id)));
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName, string contentTypeName, Action<ContentTypeQueryParameters> configureQuery = null)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(contentTypeName, configureQuery)
                    .InLanguage(languageName);
        }


        private async Task<ISet<string>> GetDependencyCacheKeys<ProductPageType>(IEnumerable<ProductPageType> products, CancellationToken cancellationToken)
            where ProductPageType : IWebPageFieldsSource
        {
            var dependencyCacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var product in products)
            {
                dependencyCacheKeys.UnionWith(GetDependencyCacheKeys(product));
            }

            dependencyCacheKeys.UnionWith(await webPageLinkedItemsDependencyRetriever.Get(products.Select(productPage => productPage.SystemFields.WebPageItemID), 1, cancellationToken));
            dependencyCacheKeys.Add(CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID));

            return dependencyCacheKeys;
        }


        private IEnumerable<string> GetDependencyCacheKeys(IWebPageFieldsSource product)
        {
            if (product == null)
            {
                return Enumerable.Empty<string>();
            }

            return new List<string>()
            {
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "byid", product.SystemFields.WebPageItemID.ToString() }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteChannelContext.WebsiteChannelName, "bypath", product.SystemFields.WebPageItemTreePath }, false),
                CacheHelper.BuildCacheItemName(new[] { "webpageitem", "bychannel", WebsiteChannelContext.WebsiteChannelName, "childrenofpath", DataHelper.GetParentPath(product.SystemFields.WebPageItemTreePath) }, false),
                CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all")
            };
        }
    }
}
