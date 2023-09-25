using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using CMS.Websites;

using DancingGoat.Helpers;

namespace DancingGoat.Models
{
	public class ImageRepository : ContentRepositoryBase
	{
		public ImageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
			: base(websiteChannelContext, executor, mapper, cache)
		{
		}


		/// <summary>
		/// Returns <see cref="Image"/> content item.
		/// </summary>
		public async Task<Image> GetImage(Guid imageGuid, CancellationToken cancellationToken = default)
		{
			var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(ImageRepository), nameof(GetImage), imageGuid);

			var result = await GetCachedQueryResult<Image>(GetImageQuery(imageGuid), null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

			return result.FirstOrDefault();
		}


		private static ContentItemQueryBuilder GetImageQuery(Guid imageGuid)
		{
			return new ContentItemQueryBuilder()
					.ForContentType(Image.CONTENT_TYPE_NAME,
						config => config
								.TopN(1)
								.Where(where => where.WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), imageGuid)));
		}


		private ISet<string> GetDependencyCacheKeys(IEnumerable<Image> images)
		{
			var dependencyCacheKeys = new HashSet<string>();

			var image = images.FirstOrDefault();

			if (image != null)
			{
				dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", image.SystemFields.ContentItemID.ToString() }, false));
			}

			return dependencyCacheKeys;
		}
	}
}
