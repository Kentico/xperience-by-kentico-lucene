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
	/// Represents a collection of links to social networks.
	/// </summary>
	public class SocialLinkRepository : ContentRepositoryBase
	{
		public SocialLinkRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IWebPageQueryResultMapper mapper, IProgressiveCache cache)
			: base(websiteChannelContext, executor, mapper, cache)
		{
		}


		/// <summary>
		/// Returns list of <see cref="SocialLink"/> content items.
		/// </summary>
		public async Task<IEnumerable<SocialLink>> GetSocialLinks(string languageName, CancellationToken cancellationToken = default)
		{
			var cacheSettings = new CacheSettings(5, WebsiteConstants.WEBSITE_CHANNEL_NAME, nameof(SocialLinkRepository), nameof(GetSocialLinks), languageName);

			return await GetCachedQueryResult<SocialLink>(GetSocialLinksQuery(languageName), null, cacheSettings, (result) => GetDependencyCacheKeys(result), cancellationToken);
		}


		private static ContentItemQueryBuilder GetSocialLinksQuery(string languageName)
		{
			return new ContentItemQueryBuilder()
					.ForContentType(SocialLink.CONTENT_TYPE_NAME, config => config.WithLinkedItems(1))
					.InLanguage(languageName);
		}


		private ISet<string> GetDependencyCacheKeys(IEnumerable<SocialLink> socialLinks)
		{
			return GetCacheByIdKeys(socialLinks.Select(socialLink => socialLink.SystemFields.ContentItemID))
				.Concat(GetCacheByIdKeys(socialLinks.SelectMany(socialLink => socialLink.SocialLinkIcon.Select(image => image.SystemFields.ContentItemID))))
				.Append(CacheHelper.GetCacheItemName(null, ContentLanguageInfo.OBJECT_TYPE, "all"))
				.ToHashSet(StringComparer.InvariantCultureIgnoreCase);
		}


		private static IEnumerable<string> GetCacheByIdKeys(IEnumerable<int> itemIds)
		{
			foreach (var id in itemIds)
			{
				yield return CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", id.ToString() }, false);
			}
		}
	}
}