using System;
using System.Collections.Generic;
using System.Linq;

namespace DancingGoat.Models
{
    public record HomePageViewModel(string NavigationTitle, BannerViewModel Banner, EventViewModel Event, string OurStoryText, ReferenceViewModel Reference, IEnumerable<CafeViewModel> Cafes)
    {
        /// <summary>
        /// Validates and maps <see cref="HomePage"/> to a <see cref="HomePageViewModel"/>.
        /// </summary>
        public static HomePageViewModel GetViewModel(HomePage home)
        {
            if (home == null)
            {
                return null;
            }

            return new HomePageViewModel(
                home.NavigationTitle,
                BannerViewModel.GetViewModel(home.HomePageBanner.FirstOrDefault()),
                EventViewModel.GetViewModel(home.HomePageEvent.OrderBy(o => Math.Abs((o.EventDate - DateTime.Today).TotalDays)).FirstOrDefault()),
                home.HomePageOurStory,
                ReferenceViewModel.GetViewModel(home.HomePageReference.FirstOrDefault()),
                home.HomePageCafes.Select(CafeViewModel.GetViewModel));
        }
    }
}
