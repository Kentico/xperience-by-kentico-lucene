using System.Collections.Generic;

using CMS.ContentEngine;

using Kentico.PageBuilder.Web.Mvc.PageTemplates;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.PageTemplates
{
    public class LandingPageSingleColumnProperties : IPageTemplateProperties
    {
        /// <summary>
        /// Indicates if logo should be shown.
        /// </summary>
        [CheckBoxComponent(Label = "{$dancinggoat.landingpagesinglecolumn.showlogo.label$}", Order = 1)]
        public bool ShowLogo { get; set; } = true;


        /// <summary>
        /// Background color CSS class of the header.
        /// </summary>
        [RequiredValidationRule]
        [DropDownComponent(Label = "{$dancinggoat.landingpagesinglecolumn.headercolor.label$}", Order = 2,
            Options = "first-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.chocolate$}\nsecond-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.gold$}\nthird-color;{$dancinggoat.landingpagesinglecolumn.headercolor.option.espresso$}")]
        [ExcludeFromAiraTranslation]
        public string HeaderColorCssClass { get; set; } = "first-color";


        /// <summary>
        /// Hero background image.
        /// </summary>
        [ContentItemSelectorComponent(Models.Image.CONTENT_TYPE_NAME, Label = "{$dancinggoat.landingpagesinglecolumn.heroimage.label$}", MaximumItems = 1, Order = 3)]
        public IEnumerable<ContentItemReference> HeroImage { get; set; } = new List<ContentItemReference>();


        /// <summary>
        /// Small uppercase label on the hero ticket (e.g. "Weekly sample").
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.landingpagesinglecolumn.ticketlabel.label$}", Order = 4)]
        public string TicketLabel { get; set; }


        /// <summary>
        /// Large value on the hero ticket (e.g. "FREE"). The ticket only renders when set.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.landingpagesinglecolumn.ticketvalue.label$}", Order = 5)]
        public string TicketValue { get; set; }


        /// <summary>
        /// Schedule line on the hero ticket (e.g. "Every Thursday").
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.landingpagesinglecolumn.ticketwhen.label$}", Order = 6)]
        public string TicketWhen { get; set; }


        /// <summary>
        /// Fine-print items on the hero ticket, separated by "·" (e.g. "Valid 7 days · Pick-up only").
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.landingpagesinglecolumn.ticketnote.label$}", Order = 7)]
        public string TicketNote { get; set; }
    }
}
