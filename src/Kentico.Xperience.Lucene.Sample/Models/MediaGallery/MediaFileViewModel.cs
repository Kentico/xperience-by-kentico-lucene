using System;

using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;

namespace DancingGoat.Models
{
    public class MediaFileViewModel
    {
        public Guid Guid { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string RelativePath { get; set; }


        public static MediaFileViewModel GetViewModel(MediaFileInfo mediaFileInfo, IMediaFileUrlRetriever fileUrlRetriever)
        {
            return new MediaFileViewModel
            {
                Guid = mediaFileInfo.FileGUID,
                Title = mediaFileInfo.FileTitle,
                Name = mediaFileInfo.FileName,
                RelativePath = fileUrlRetriever.Retrieve(mediaFileInfo).WithSizeConstraint(SizeConstraint.MaxWidthOrHeight(400)).RelativePath
            };
        }
    }
}