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
    /// Represents a collection of coffees.
    /// </summary>
    public class CoffeeRepository : ContentRepositoryBase
    {
        private readonly ILinkedItemsDependencyRetriever linkedItemsDependencyRetriever;


        /// <summary>
        /// Initializes a new instance of the <see cref="CoffeeRepository"/> class that returns coffees.
        /// </summary>
        public CoffeeRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache, ILinkedItemsDependencyRetriever linkedItemsDependencyRetriever)
            : base(websiteChannelContext, executor, mapper, cache)
        {
            this.linkedItemsDependencyRetriever = linkedItemsDependencyRetriever;
        }


        /// <summary>
        /// Returns an enumerable collection of <see cref="Coffee"/> based on a given collection of content item guids.
        /// </summary>
        public async Task<IEnumerable<Coffee>> GetCoffees(ICollection<Guid> coffeeGuids, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(coffeeGuids, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(CoffeeRepository), nameof(GetCoffees), languageName, coffeeGuids.Select(guid => guid.ToString()).Join("|"));

            return await GetCachedQueryResult<Coffee>(queryBuilder, null, cacheSettings, (coffees, cancellationToken) => GetDependencyCacheKeys(coffees, coffeeGuids), cancellationToken);
        }


        private static ContentItemQueryBuilder GetQueryBuilder(ICollection<Guid> coffeeGuids, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(Coffee.CONTENT_TYPE_NAME,
                        config => config
                            .WithLinkedItems(1)
                            .Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), coffeeGuids)))
                    .InLanguage(languageName);
        }


        private Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<Coffee> coffees, IEnumerable<Guid> coffeeGuids)
        {
            var dependencyCacheKeys = linkedItemsDependencyRetriever.Get(coffees.Select(coffee => coffee.SystemFields.ContentItemID), 1).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            foreach (var guid in coffeeGuids)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byguid", guid.ToString() }, false));
            }

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }
    }
}
