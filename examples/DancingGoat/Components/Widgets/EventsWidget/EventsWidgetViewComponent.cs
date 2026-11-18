using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.Models;
using DancingGoat.Widgets;

using Kentico.Content.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;

[assembly: RegisterWidget(EventsWidgetViewComponent.IDENTIFIER, typeof(EventsWidgetViewComponent), "{$dancinggoat.eventswidget.title$}", typeof(EventsWidgetProperties), Description = "{$dancinggoat.eventswidget.description$}", IconClass = "icon-calendar")]

namespace DancingGoat.Widgets
{
    /// <summary>
    /// View component for events widget.
    /// </summary>
    public class EventsWidgetViewComponent : ViewComponent
    {
        /// <summary>
        /// Widget identifier.
        /// </summary>
        public const string IDENTIFIER = "DancingGoat.General.EventsWidget";


        private readonly IContentRetriever contentRetriever;


        /// <summary>
        /// Creates an instance of <see cref="EventsWidgetViewComponent"/> class.
        /// </summary>
        /// <param name="contentRetriever">Content retriever.</param>
        public EventsWidgetViewComponent(IContentRetriever contentRetriever)
        {
            this.contentRetriever = contentRetriever;
        }


        public async Task<ViewViewComponentResult> InvokeAsync(EventsWidgetProperties properties, CancellationToken cancellationToken)
        {
            var selectedEventGuids = properties.SelectedEvents.Select(eventReference => eventReference.Identifier).ToList();
            if (selectedEventGuids.Count == 0)
            {
                return View("~/Components/Widgets/EventsWidget/_EventsWidget.cshtml", EventsWidgetViewModel.GetViewModel(Enumerable.Empty<Event>(), properties.Heading));
            }

            var events = await contentRetriever.RetrieveContent<Event>(
                new RetrieveContentParameters { LinkedItemsMaxLevel = 2 },
                query => query.Where(where => where.WhereIn(nameof(IContentQueryDataContainer.ContentItemGUID), selectedEventGuids)),
                new RetrievalCacheSettings($"WhereIn_{nameof(IContentQueryDataContainer.ContentItemGUID)}_{string.Join("_", selectedEventGuids)}"),
                cancellationToken
            );

            if (properties.ShowOnlyUpcoming)
            {
                events = events.Where(eventContentItem => eventContentItem.EventDate >= DateTime.Today);
            }

            var orderedEvents = events.OrderBy(eventContentItem => eventContentItem.EventDate);
            var model = EventsWidgetViewModel.GetViewModel(orderedEvents, properties.Heading);

            return View("~/Components/Widgets/EventsWidget/_EventsWidget.cshtml", model);
        }
    }
}
