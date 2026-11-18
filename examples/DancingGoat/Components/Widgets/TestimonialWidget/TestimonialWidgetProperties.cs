using System.Collections.Generic;

using CMS.ContentEngine;

using DancingGoat.Models;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Properties for Testimonial widget.
    /// </summary>
    public class TestimonialWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Section heading. Falls back to a localized default when empty.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.testimonialwidget.heading.label$}", ExplanationText = "{$dancinggoat.widget.heading.explanation$}", Order = 0)]
        public string Heading { get; set; }


        /// <summary>
        /// Displayed testimonial.
        /// </summary>
        [ContentItemSelectorComponent(Testimonial.CONTENT_TYPE_NAME, Label = "{$dancinggoat.testimonialwidget.testimonial.label$}", MaximumItems = 1, Order = 1)]
        public IEnumerable<ContentItemReference> SelectedTestimonial { get; set; } = new List<ContentItemReference>();


        /// <summary>
        /// Optional author photo displayed next to the quote.
        /// </summary>
        [ContentItemSelectorComponent(Models.Image.CONTENT_TYPE_NAME, Label = "{$dancinggoat.testimonialwidget.image.label$}", MaximumItems = 1, Order = 3)]
        public IEnumerable<ContentItemReference> Image { get; set; } = new List<ContentItemReference>();
    }
}
