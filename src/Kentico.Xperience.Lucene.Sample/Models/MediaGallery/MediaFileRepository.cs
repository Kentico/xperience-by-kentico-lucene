using System;
using System.Collections.Generic;

using CMS.Base;
using CMS.MediaLibrary;

using DancingGoat.Infrastructure;

namespace DancingGoat.Models
{
    /// <summary>
    /// Represents a collection of media files.
    /// </summary>
    public class MediaFileRepository
    {
        private readonly IMediaLibraryInfoProvider mediaLibraryInfoProvider;
        private readonly IMediaFileInfoProvider mediaFileInfoProvider;
        private readonly RepositoryCacheHelper repositoryCacheHelper;
        private readonly ISiteService siteService;


        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFileRepository"/> class.
        /// </summary>
        /// <param name="mediaLibraryInfoProvider">Provider for <see cref="MediaLibraryInfo"/> management.</param>
        /// <param name="mediaFileInfoProvider">Provider for <see cref="MediaFileInfo"/> management.</param>
        /// <param name="repositoryCacheHelper">Handles caching of retrieved objects.</param>
        public MediaFileRepository(IMediaLibraryInfoProvider mediaLibraryInfoProvider, 
            IMediaFileInfoProvider mediaFileInfoProvider,
            ISiteService siteService,
            RepositoryCacheHelper repositoryCacheHelper)
        {
            this.mediaLibraryInfoProvider = mediaLibraryInfoProvider;
            this.mediaFileInfoProvider = mediaFileInfoProvider;
            this.repositoryCacheHelper = repositoryCacheHelper;
            this.siteService = siteService;
        }


        /// <summary>
        /// Returns instance of <see cref="MediaFileInfo"/> specified by library name.
        /// </summary>
        /// <param name="mediaLibraryName">Name of the media library.</param>
        public MediaLibraryInfo GetByName(string mediaLibraryName)
        {
            return repositoryCacheHelper.CacheObject(() =>
            {
                return mediaLibraryInfoProvider.Get(mediaLibraryName, siteService.CurrentSite?.SiteID ?? 0);
            }, $"{nameof(MediaFileRepository)}|{nameof(GetByName)}|{mediaLibraryName}");
        }


        /// <summary>
        /// Returns all media files in the media library.
        /// </summary>
        /// <param name="mediaLibraryName">Name of the media library.</param>
        public IEnumerable<MediaFileInfo> GetMediaFiles(string mediaLibraryName)
        {
            return repositoryCacheHelper.CacheObjects(() =>
            {
                var mediaLibrary = GetByName(mediaLibraryName);

                if (mediaLibrary == null)
                {
                    throw new InvalidOperationException("Media library not found.");
                }

                return mediaFileInfoProvider.Get()
                    .WhereEquals("FileLibraryID", mediaLibrary.LibraryID);
            }, $"{nameof(MediaFileRepository)}|{nameof(GetMediaFiles)}|{mediaLibraryName}");
        }
    }
}