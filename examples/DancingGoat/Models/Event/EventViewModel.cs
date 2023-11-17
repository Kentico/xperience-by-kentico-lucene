using System;
using System.Collections.Generic;
using System.Linq;

using CMS.DocumentEngine.Types.DancingGoatCore;

namespace DancingGoat.Models
{
    public class EventViewModel
    {
        public string Title { get; set; }

        public string PromoText { get; set; }

        public DateTime Date { get; set; }

        public string HeroBannerImagePath { get; set; }

        public string HeroBannerImageShortDescription { get; set; }

        public string Location { get; set; }

        public IEnumerable<string> Coffees { get; set; }


        public static EventViewModel GetViewModel(Event cuppingEvent)
        {
            if (cuppingEvent == null)
            {
                return null;
            }

            var cafe = cuppingEvent.Fields.Cafe.FirstOrDefault() as Cafe;
            var heroBannerImage = cuppingEvent.Fields.HeroBannerImage.FirstOrDefault() as Media;

            return new EventViewModel
            {
                Title = cuppingEvent.DocumentName,
                Date = cuppingEvent.EventDate,
                PromoText = cuppingEvent.EventPromoText,
                Location = cafe?.DocumentName,
                Coffees = cafe?.Fields.CuppingOffer.Select(p => p.DocumentName),
                HeroBannerImagePath = heroBannerImage?.Fields.File?.Url,
                HeroBannerImageShortDescription = heroBannerImage?.Fields.ShortDescription ?? string.Empty
            };
        }
    }
}