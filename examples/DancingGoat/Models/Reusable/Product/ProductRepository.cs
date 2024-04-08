using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of product pages.
    /// </summary>
    public class ProductRepository : ContentRepositoryBase
    {
        private const string COFFEE_PROCESSING = "CoffeeProcessing";
        private const string COFFEE_TASTES = "CoffeeTastes";
        private const string GRINDER_MANUFACTURER = "GrinderManufacturer";
        private const string GRINDER_TYPE = "GrinderType";


        private readonly ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyRetriever;
        private readonly IInfoProvider<TaxonomyInfo> taxonomyInfoProvider;


        /// <summary>
        /// Initializes new instance of <see cref="ProductRepository"/>.
        /// </summary>
        public ProductRepository(
            IWebsiteChannelContext websiteChannelContext,
            IContentQueryExecutor executor,
            IWebPageQueryResultMapper mapper,
            IProgressiveCache cache,
            ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyRetriever,
            IInfoProvider<TaxonomyInfo> taxonomyInfoProvider)
            : base(websiteChannelContext, executor, mapper, cache)
        {
            this.linkedItemsDependencyRetriever = linkedItemsDependencyRetriever;
            this.taxonomyInfoProvider = taxonomyInfoProvider;
        }


        /// <summary>
        /// Returns list of <see cref="Product"/> content items.
        /// </summary>
        public async Task<IEnumerable<Product>> GetProducts(string languageName, IDictionary<string, TaxonomyViewModel> filter, bool includeSecuredItems = true, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(languageName, filter);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var filterCacheItemNameParts = filter.Values.Where(value => value != null && value.Tags != null).SelectMany(value => value.Tags.Where(tag => tag.IsChecked)).Select(id => id.Value.ToString()).Join("|");

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, languageName, includeSecuredItems, nameof(Product), filterCacheItemNameParts);

            return await GetCachedQueryResult<Product>(queryBuilder, options, cacheSettings, (_, _) => GetDependencyCacheKeys(languageName), cancellationToken);
        }


        private static ContentItemQueryBuilder GetQueryBuilder(string languageName, IDictionary<string, TaxonomyViewModel> filter)
        {
            var baseBuilder = new ContentItemQueryBuilder()
                    .ForContentType(Coffee.CONTENT_TYPE_NAME)
                    .ForContentType(Grinder.CONTENT_TYPE_NAME)
                    .InLanguage(languageName);

            if (!filter.Any())
            {
                return baseBuilder;
            }

            return baseBuilder
                .Parameters(query => query.Where(where => where
                    .Where(async coffeeWhere => coffeeWhere
                        .WhereContainsTags(nameof(Coffee.CoffeeProcessing), await GetSelectedTags(filter, COFFEE_PROCESSING))
                        .WhereContainsTags(nameof(Coffee.CoffeeTastes), await GetSelectedTags(filter, COFFEE_TASTES))
                    .Where(async grinderWhere => grinderWhere
                        .WhereContainsTags(nameof(Grinder.GrinderManufacturer), await GetSelectedTags(filter, GRINDER_MANUFACTURER))
                        .WhereContainsTags(nameof(Grinder.GrinderType), await GetSelectedTags(filter, GRINDER_TYPE))))
                ));
        }


        private static async Task<TagCollection> GetSelectedTags(IDictionary<string, TaxonomyViewModel> filter, string taxonomyName)
        {
            if (filter.TryGetValue(taxonomyName, out var taxonomy))
            {
                return await taxonomy.GetSelectedTags();
            }

            return null;
        }


        private async Task<ISet<string>> GetDependencyCacheKeys(string languageName)
        {
            var dependencyCacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
            {
                CacheHelper.GetCacheItemName(null, WebsiteChannelInfo.OBJECT_TYPE, "byid", WebsiteChannelContext.WebsiteChannelID),
                CacheHelper.GetCacheItemName(null, "contentitem", "bycontenttype", Coffee.CONTENT_TYPE_NAME, languageName),
                CacheHelper.GetCacheItemName(null, "contentitem", "bycontenttype", Grinder.CONTENT_TYPE_NAME, languageName),
                await GetTaxonomyTagsCacheDependencyKey(COFFEE_PROCESSING),
                await GetTaxonomyTagsCacheDependencyKey(COFFEE_TASTES),
                await GetTaxonomyTagsCacheDependencyKey(GRINDER_MANUFACTURER),
                await GetTaxonomyTagsCacheDependencyKey(GRINDER_TYPE)
            };

            return dependencyCacheKeys;
        }


        private async Task<string> GetTaxonomyTagsCacheDependencyKey(string taxonomyName)
        {
            var taxonomyID = (await taxonomyInfoProvider.GetAsync(taxonomyName)).TaxonomyID;
            return CacheHelper.GetCacheItemName(null, TaxonomyInfo.OBJECT_TYPE, "byid", taxonomyID, "children");
        }
    }
}
