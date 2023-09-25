using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;

using DancingGoat.Helpers;

namespace DancingGoat.Models
{
	/// <summary>
	/// Represents a collection of coffees.
	/// </summary>
	public partial class CoffeeRepository : ContentRepositoryBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoffeeRepository"/> class that returns coffees.
		/// </summary>
		public CoffeeRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
			: base(websiteChannelContext, executor, mapper, cache)
		{
		}


		/// <summary>
		/// Returns an enumerable collection of <see cref="Coffee"/> based on a given collection of content item guids.
		/// </summary>
		public async Task<IEnumerable<Coffee>> GetCoffees(ICollection<Guid> coffeeGuids, CancellationToken cancellationToken = default)
		{
			var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(CoffeeRepository), nameof(GetCoffees), coffeeGuids.Select(guid => guid.ToString()).Join("|"));

			return await GetCachedQueryResult<Coffee>(GetCoffeesQuery(coffeeGuids), null, cacheSettings, GetDependencyCacheKeys, cancellationToken);
		}


		private static ContentItemQueryBuilder GetCoffeesQuery(ICollection<Guid> coffeeGuids)
		{
			return new ContentItemQueryBuilder()
					.ForContentType(Coffee.CONTENT_TYPE_NAME,
						config => config
							.WithLinkedItems(1)
							.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), coffeeGuids)));
		}


		private ISet<string> GetDependencyCacheKeys(IEnumerable<Coffee> coffees)
		{
			var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var coffee in coffees)
			{
				cacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", coffee.SystemFields.ContentItemID.ToString() }, false));
			}

			return cacheKeys;
		}
	}
}