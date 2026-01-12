using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// CTA button widget properties.
    /// </summary>
    public class CTAButtonWidgetProperties : IWidgetProperties
    {
        /// <summary>
        /// Button text.
        /// </summary>
        public string Text { get; set; }


        /// <summary>
        /// Page where the button points to.
        /// </summary>
        [UrlSelectorComponent(Label = "{$dancinggoat.ctabuttonwidget.linkurl.label$}", Order = 1)]
        [ExcludeFromAiraTranslation]
        public string LinkUrl { get; set; }


        /// <summary>
        /// Indicates if link should be opened in a new tab.
        /// </summary>
        [CheckBoxComponent(Label = "{$dancinggoat.ctabuttonwidget.openinnewtab.label$}", Order = 2)]
        public bool OpenInNewTab { get; set; }
    }
}
