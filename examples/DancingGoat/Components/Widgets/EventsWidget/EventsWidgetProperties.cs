using System.Collections.Generic;

using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Events widget properties.
    /// </summary>
    public class EventsWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Section heading. Falls back to a localized default when empty.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.eventswidget.heading.label$}", ExplanationText = "{$dancinggoat.widget.heading.explanation$}", Order = 0)]
        public string Heading { get; set; }


        /// <summary>
        /// Selected events.
        /// </summary>
        [ContentItemSelectorComponent(Event.CONTENT_TYPE_NAME, Label = "{$dancinggoat.eventswidget.selectedevents.label$}", Order = 1)]
        public IEnumerable<ContentItemReference> SelectedEvents { get; set; } = new List<ContentItemReference>();


        /// <summary>
        /// Indicates if only upcoming events are displayed.
        /// </summary>
        [CheckBoxComponent(Label = "{$dancinggoat.eventswidget.showonlyupcoming.label$}", Order = 2)]
        public bool ShowOnlyUpcoming { get; set; }
    }
}
