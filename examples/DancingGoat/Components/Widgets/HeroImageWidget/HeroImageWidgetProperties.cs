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
        public string Text { get; set; }


        /// <summary>
        /// Button text.
        /// </summary>
        public string ButtonText { get; set; }


        /// <summary>
        /// Target of button link.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.heroimagewidget.buttontarget.label$}", Order = 2)]
        [UrlValidationRule(AllowRelativeUrl = true, AllowFragmentUrl = true)]
        [ExcludeFromAiraTranslation]
        public string ButtonTarget { get; set; }


        /// <summary>
        /// Theme of the widget.
        /// </summary>
        [DropDownComponent(Label = "{$dancinggoat.heroimagewidget.theme.label$}", Order = 3,
            Options = "light;{$dancinggoat.heroimagewidget.theme.option.light$}\ndark;{$dancinggoat.heroimagewidget.theme.option.dark$}")]
        [ExcludeFromAiraTranslation]
        public string Theme { get; set; } = "dark";
    }
}
