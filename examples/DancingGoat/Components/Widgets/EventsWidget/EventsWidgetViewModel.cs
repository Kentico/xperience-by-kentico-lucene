using System.Collections.Generic;
using System.Linq;

using DancingGoat.Models;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View model for Events widget.
    /// </summary>
    public class EventsWidgetViewModel
    {
        /// <summary>
        /// Section heading, or <see langword="null"/> to use the localized default.
        /// </summary>
        public string Heading { get; set; }


        /// <summary>
        /// Collection of events.
        /// </summary>
        public IEnumerable<EventViewModel> Events { get; set; }


        /// <summary>
        /// Gets ViewModels for <paramref name="events"/>.
        /// </summary>
        public static EventsWidgetViewModel GetViewModel(IEnumerable<Event> events, string heading = null)
        {
            return new EventsWidgetViewModel
            {
                Heading = heading,
                Events = events.Where(eventContentItem => eventContentItem != null)
                    .Select(EventViewModel.GetViewModel)
                    .ToList()
            };
        }
    }
}
