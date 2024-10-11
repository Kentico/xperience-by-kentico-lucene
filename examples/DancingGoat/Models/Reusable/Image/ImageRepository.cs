using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites.Routing;

namespace DancingGoat.Models
{
    public class ImageRepository : ContentRepositoryBase
    {
        public ImageRepository(IWebsiteChannelContext websiteChannelContext, IContentQueryExecutor executor, IProgressiveCache cache)
            : base(websiteChannelContext, executor, cache)
        {
        }


        /// <summary>
        /// Returns <see cref="Image"/> content item.
        /// </summary>
        public async Task<Image> GetImage(Guid imageGuid, string languageName, CancellationToken cancellationToken = default)
        {
            var queryBuilder = GetQueryBuilder(imageGuid, languageName);

            var cacheSettings = new CacheSettings(5, WebsiteChannelContext.WebsiteChannelName, nameof(ImageRepository), nameof(GetImage), languageName, imageGuid);

            var result = await GetCachedQueryResult<Image>(queryBuilder, null, cacheSettings, GetDependencyCacheKeys, cancellationToken);

            return result.FirstOrDefault();
        }


        private static ContentItemQueryBuilder GetQueryBuilder(Guid imageGuid, string languageName)
        {
            return new ContentItemQueryBuilder()
                    .ForContentType(Image.CONTENT_TYPE_NAME,
                        config => config
                                .TopN(1)
                                .Where(where => where.WhereEquals(nameof(IContentQueryDataContainer.ContentItemGUID), imageGuid)))
                    .InLanguage(languageName);
        }


        private static Task<ISet<string>> GetDependencyCacheKeys(IEnumerable<Image> images, CancellationToken cancellationToken)
        {
            var dependencyCacheKeys = new HashSet<string>();

            var image = images.FirstOrDefault();

            if (image != null)
            {
                dependencyCacheKeys.Add(CacheHelper.BuildCacheItemName(new[] { "contentitem", "byid", image.SystemFields.ContentItemID.ToString() }, false));
            }

            return Task.FromResult<ISet<string>>(dependencyCacheKeys);
        }
    }
}
