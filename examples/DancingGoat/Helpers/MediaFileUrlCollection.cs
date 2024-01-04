using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;

using System;
using System.Collections.Generic;

namespace DancingGoat.Helpers
{
    /// <summary>
    /// Represents a collection of media file URLs accessible by asset identifiers.
    /// </summary>
    public sealed class MediaFileUrlCollection
    {
        private readonly IDictionary<Guid, IMediaFileUrl> mediaFileUrlCollection;


        /// <summary>
        /// Creates new version of <see cref="MediaFileUrlCollection"/>.
        /// </summary>
        /// <param name="mediaFileUrlCollection">Source collection of file URLs.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mediaFileUrlCollection"/></exception>
        public MediaFileUrlCollection(IDictionary<Guid, IMediaFileUrl> mediaFileUrlCollection)
        {
            this.mediaFileUrlCollection = mediaFileUrlCollection ?? throw new ArgumentNullException(nameof(mediaFileUrlCollection));
        }


        /// <summary>
        /// Returns a relative path of the asset for specified asset model.
        /// </summary>
        /// <param name="assetSelector">Asset selector function.</param>
        public IMediaFileUrl GetFileUrl(Func<AssetRelatedItem> assetSelector)
        {
            return mediaFileUrlCollection.TryGetValue(assetSelector()?.Identifier ?? Guid.Empty, out var fileUrl) ? fileUrl : default;
        }
    }
}
