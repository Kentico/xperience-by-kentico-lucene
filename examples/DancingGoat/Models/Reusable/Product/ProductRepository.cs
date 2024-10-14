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
            IProgressiveCache cache,
            ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyRetriever,
            IInfoProvider<TaxonomyInfo> taxonomyInfoProvider)
            : base(websiteChannelContext, executor, cache)
        {
            this.linkedItemsDependencyRetriever = linkedItemsDependencyRetriever;
            this.taxonomyInfoProvider = taxonomyInfoProvider;
        }


        /// <summary>
        /// Returns list of <see cref="IProductFields"/> content items.
        /// </summary>
        public async Task<IEnumerable<IProductFields>> GetProducts(string languageName, IDictionary<string, TaxonomyViewModel> filter, bool includeSecuredItems = true, CancellationToken cancellationToken = default)
        {
            var queryBuilder = await GetQueryBuilder(languageName, filter: filter);

            var options = new ContentQueryExecutionOptions
            {
                IncludeSecuredItems = includeSecuredItems
            };

            var filterCacheItemNameParts = filter.Values.Where(value => value != null && value.Tags != null).SelectMany(value => value.Tags.Where(tag => tag.IsChecked)).Select(id => id.Value.ToString()).Join("|");

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, languageName, includeSecuredItems, nameof(IProductFields), filterCacheItemNameParts);

            return await GetCachedQueryResult<IProductFields>(queryBuilder, options, cacheSettings, (_, _) => GetDependencyCacheKeys(languageName), cancellationToken);
        }


        /// <summary>
        /// Returns list of <see cref="IProductFields"/> content items.
        /// </summary>
        public async Task<IEnumerable<IProductFields>> GetProducts(ICollection<Guid> productGuids, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = await GetQueryBuilder(languageName, productGuids);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, languageName, nameof(IProductFields), productGuids.Select(guid => guid.ToString()).Join("|"));

            return await GetCachedQueryResult<IProductFields>(queryBuilder, new ContentQueryExecutionOptions(), cacheSettings, (_, _) => GetDependencyCacheKeys(languageName, productGuids), cancellationToken);
        }


        private static async Task<ContentItemQueryBuilder> GetQueryBuilder(string languageName, IEnumerable<Guid> productGuids = null, IDictionary<string, TaxonomyViewModel> filter = null)
        {
            var baseBuilder = new ContentItemQueryBuilder().ForContentTypes(ct =>
                {
                    ct.OfReusableSchema(IProductFields.REUSABLE_FIELD_SCHEMA_NAME)
                      .WithContentTypeFields()
                      .WithLinkedItems(1);
                }).InLanguage(languageName);

            if (productGuids != null)
            {
                baseBuilder.Parameters(query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), productGuids)));
            }

            if (filter == null || !filter.Any())
            {
                return baseBuilder;
            }

            var coffeeProcessingTags = await GetSelectedTags(filter, COFFEE_PROCESSING);
            var coffeeTastesTags = await GetSelectedTags(filter, COFFEE_TASTES);
            var grinderManufacturerTags = await GetSelectedTags(filter, GRINDER_MANUFACTURER);
            var grinderTypeTags = await GetSelectedTags(filter, GRINDER_TYPE);

            return baseBuilder
                .Parameters(query => query.Where(where => where
                    .Where(coffeeWhere => coffeeWhere
                        .WhereContainsTags(nameof(Coffee.CoffeeProcessing), coffeeProcessingTags)
                        .WhereContainsTags(nameof(Coffee.CoffeeTastes), coffeeTastesTags))
                    .Where(grinderWhere => grinderWhere
                        .WhereContainsTags(nameof(Grinder.GrinderManufacturer), grinderManufacturerTags)
                        .WhereContainsTags(nameof(Grinder.GrinderType), grinderTypeTags))
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


        private async Task<ISet<string>> GetDependencyCacheKeys(string languageName, ICollection<Guid> productGuids = null)
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
            GetProductPageDependencies(productGuids, dependencyCacheKeys);

            return dependencyCacheKeys;
        }


        private static void GetProductPageDependencies(ICollection<Guid> productGuids, HashSet<string> dependencyCacheKeys)
        {
            if (productGuids == null || !productGuids.Any())
            {
                return;
            }

            foreach (var guid in productGuids)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byguid", guid.ToString() }, false));
            }
        }


        private async Task<string> GetTaxonomyTagsCacheDependencyKey(string taxonomyName)
        {
            var taxonomyID = (await taxonomyInfoProvider.GetAsync(taxonomyName)).TaxonomyID;
            return CacheHelper.GetCacheItemName(null, TaxonomyInfo.OBJECT_TYPE, "byid", taxonomyID, "children");
        }
    }
}
