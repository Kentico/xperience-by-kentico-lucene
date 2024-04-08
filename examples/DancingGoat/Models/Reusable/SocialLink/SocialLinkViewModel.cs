using System;
using System.Linq;

namespace DancingGoat.Models
{
    public record SocialLinkViewModel(string Title, string Url, string IconPath)
    {
        /// <summary>
        /// Validates and maps <see cref="SocialLink"/> to a <see cref="SocialLinkViewModel"/>.
        /// </summary>
        public static SocialLinkViewModel GetViewModel(SocialLink socialLink)
        {
            var socialLinkUrl = Uri.TryCreate(socialLink.SocialLinkUrl, UriKind.Absolute, out var _) ? socialLink.SocialLinkUrl : String.Empty;
            return new SocialLinkViewModel(socialLink.SocialLinkTitle, socialLinkUrl, socialLink.SocialLinkIcon.FirstOrDefault()?.ImageFile?.Url);
        }
    }
}
