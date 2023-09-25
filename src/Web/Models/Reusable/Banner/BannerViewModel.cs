using System.Linq;

namespace DancingGoat.Models
{
    public record BannerViewModel(string BackgroundImageUrl, string HeaderText, string Text)
    {
        /// <summary>
        /// Validates and maps <see cref="Banner"/> to a <see cref="BannerViewModel"/>.
        /// </summary>
        public static BannerViewModel GetViewModel(Banner banner)
        {
            if (banner == null)
            {
                return null;
            }

            var image = banner.BannerBackgroundImage.FirstOrDefault();

            return new BannerViewModel(image?.ImageFile.Url, banner.BannerHeaderText, banner.BannerText);
        }
    }
}
