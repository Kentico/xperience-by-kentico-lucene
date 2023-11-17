using System;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;
using CMS.Helpers;

namespace DancingGoat.Models
{
    public class SocialLinkViewModel
    {
        public string IconPath { get; set; }


        public string Title { get; set; }


        public string Url { get; set; }


        public static SocialLinkViewModel GetViewModel(SocialLink socialLink)
        {
            if (!string.IsNullOrEmpty(socialLink.Fields.Url))
            {
                var protocol = URLHelper.GetProtocol(socialLink.Fields.Url);
                if (!string.Equals("http", protocol, StringComparison.OrdinalIgnoreCase) && !string.Equals("https", protocol, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The link must begin with lowercased 'http://' or 'https://'");
                }
            }

            return new SocialLinkViewModel
            {
                Title = socialLink.Fields.Title,
                Url = socialLink.Fields.Url,
                IconPath = (socialLink.Fields.Icon.FirstOrDefault() as Media)?.Fields.File?.Url
            };
        }
    }
}