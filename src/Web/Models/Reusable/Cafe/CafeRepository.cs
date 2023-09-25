using System;
using System.Collections.Generic;
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
	/// Represents a collection of cafes.
	/// </summary>
	public partial class CafeRepository : ContentRepositoryBase
	{
		public CafeRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
			: base(websiteChannelContext, executor, mapper, cache)
		{
		}

		/// <summary>
		/// Returns an enumerable collection of company cafes ordered by a position in the content tree.
		/// </summary>
		/// <param name="nodeAliasPath">The node alias path of the articles section in the content tree.</param>
		/// <param name="count">The number of cafes to return. Use 0 as value to return all records.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public async Task<IEnumerable<Cafe>> GetCompanyCafes(int count = 0, CancellationToken cancellationToken = default)
		{
			var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(CafeRepository), nameof(GetCompanyCafes), count);

			return await GetCachedQueryResult<Cafe>(GetCafeQuery(count), null, cacheSettings, GetDependencyCacheKeys, cancellationToken);
		}


		private ContentItemQueryBuilder GetCafeQuery(int count)
		{
			return new ContentItemQueryBuilder()
					.ForContentType(Cafe.CONTENT_TYPE_NAME,
					config => config
						.WithLinkedItems(1)
						.TopN(count)
						.Where(where => where.WhereTrue(nameof(Cafe.CafeIsCompanyCafe))));
		}


		private ISet<string> GetDependencyCacheKeys(IEnumerable<Cafe> cafes)
		{
			var cacheKeys = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var cafe in cafes)
			{
				cacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", cafe.SystemFields.ContentItemID.ToString() }, false));
			}

			return cacheKeys;
		}

	}
}