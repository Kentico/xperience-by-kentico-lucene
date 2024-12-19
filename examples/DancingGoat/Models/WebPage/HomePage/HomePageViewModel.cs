using System;
using System.Collections.Generic;
using System.Linq;

using CMS.Websites;

namespace DancingGoat.Models
{
    public record HomePageViewModel(BannerViewModel Banner, EventViewModel Event, string OurStoryText, ReferenceViewModel Reference, IEnumerable<CafeViewModel> Cafes, WebPageRelatedItem ArticlesSection)
        : IWebPageBasedViewModel
    {
        /// <inheritdoc/>
        public IWebPageFieldsSource WebPage { get; init; }


        /// <summary>
        /// Validates and maps <see cref="HomePage"/> to a <see cref="HomePageViewModel"/>.
        /// </summary>
        public static HomePageViewModel GetViewModel(HomePage home, IEnumerable<Cafe> cafes)
        {
            if (home == null)
            {
                return null;
            }

            return new HomePageViewModel(
                BannerViewModel.GetViewModel(home.HomePageBanner.FirstOrDefault()),
                EventViewModel.GetViewModel(home.HomePageEvent.OrderBy(o => Math.Abs((o.EventDate - DateTime.Today).TotalDays)).FirstOrDefault()),
                home.HomePageOurStory,
                ReferenceViewModel.GetViewModel(home.HomePageReference.FirstOrDefault()),
                cafes.Select(CafeViewModel.GetViewModel),
                home.HomePageArticlesSection.FirstOrDefault())
            {
                WebPage = home
            };
        }
    }
}
