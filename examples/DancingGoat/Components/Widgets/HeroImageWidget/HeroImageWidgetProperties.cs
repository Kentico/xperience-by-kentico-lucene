using System.Collections.Generic;

using CMS.ContentEngine;

using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Hero image widget properties.
    /// </summary>
    public class HeroImageWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Background image.
        /// </summary>
        [ContentItemSelectorComponent(Models.Image.CONTENT_TYPE_NAME, Label = "{$dancinggoat.heroimagewidget.image.label$}", Order = 1)]
        public IEnumerable<ContentItemReference> Image { get; set; } = new List<ContentItemReference>();


        /// <summary>
        /// Text to be displayed.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.text.label$}", Order = 2)]
        public string Text { get; set; }


        /// <summary>
        /// Supporting text displayed under the heading.
        /// </summary>
        [TextAreaComponent(Label = "{$dancinggoat.heroimagewidget.subtext.label$}", Order = 3)]
        public string Subtext { get; set; }


        /// <summary>
        /// Button text.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.buttontext.label$}", Order = 4)]
        public string ButtonText { get; set; }


        /// <summary>
        /// Target of button link.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.buttontarget.label$}", Order = 5)]
        [UrlValidationRule(AllowRelativeUrl = true, AllowFragmentUrl = true)]
        [ExcludeFromAiraTranslation]
        public string ButtonTarget { get; set; }


        /// <summary>
        /// Secondary (ghost) button text.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.secondarybuttontext.label$}", Order = 6)]
        public string SecondaryButtonText { get; set; }


        /// <summary>
        /// Target of the secondary button link.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.secondarybuttontarget.label$}", Order = 7)]
        [UrlValidationRule(AllowRelativeUrl = true, AllowFragmentUrl = true)]
        [ExcludeFromAiraTranslation]
        public string SecondaryButtonTarget { get; set; }
    }
}
