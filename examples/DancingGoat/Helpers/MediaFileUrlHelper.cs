using CMS.Base;
using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DancingGoat.Helpers
{
    /// <summary>
    /// Helper methods for media file URL retrieval from asset collection.
    /// </summary>
    public sealed class MediaFileUrlHelper
    {
        private readonly ISiteService siteService;
        private readonly IMediaFileInfoProvider mediaFileInfoProvider;
        private readonly IMediaFileUrlRetriever mediaFileUrlRetriever;


        /// <summary>
        /// Creates new instance of <see cref="MediaFileUrlHelper"/>.
        /// </summary>
        /// <param name="siteService">Site service.</param>
        /// <param name="mediaFileInfoProvider">Media file info provider.</param>
        /// <param name="mediaFileUrlRetriever">Media file URL retriever.</param>
        /// <param name="progressiveCache">Progressive cache service.</param>
        public MediaFileUrlHelper(ISiteService siteService, IMediaFileInfoProvider mediaFileInfoProvider, IMediaFileUrlRetriever mediaFileUrlRetriever)
        {
            this.siteService = siteService;
            this.mediaFileInfoProvider = mediaFileInfoProvider;
            this.mediaFileUrlRetriever = mediaFileUrlRetriever;
        }


        /// <summary>
        /// Returns media file URL for the first asset in the <paramref name="assets"/> collection.
        /// </summary>
        /// <param name="assets">Assets collection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IMediaFileUrl> GetMediaFileUrl(IEnumerable<AssetRelatedItem> assets, CancellationToken cancellationToken = default)
        {
            var assetId = assets.FirstOrDefault()?.Identifier ?? Guid.Empty;
            if (assetId == Guid.Empty)
            {
                return null;
            }

            var mediaFile = await mediaFileInfoProvider
                .GetAsync(assetId, siteService.CurrentSite.SiteID, cancellationToken);

            if (mediaFile == null)
            {
                return null;
            }

            return mediaFileUrlRetriever.Retrieve(mediaFile);
        }
    }
}
