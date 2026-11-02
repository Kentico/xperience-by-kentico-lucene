using Kentico.Builder.Web.Mvc;
using Kentico.PageBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.Widgets
{
    /// <summary>
    /// Key takeaways widget properties.
    /// </summary>
    public class KeyTakeawaysWidgetProperties : AiProvenanceWidgetProperties, IWidgetProperties
    {
        /// <summary>
        /// Heading of the takeaways.
        /// </summary>
        [TextInputComponent(Label = "{$dancinggoat.keytakeawayswidget.heading.label$}", Order = 1)]
        public string Heading { get; set; }


        /// <summary>
        /// Main points of the article, one per line.
        /// </summary>
        [TextAreaComponent(Label = "{$dancinggoat.keytakeawayswidget.text.label$}", ExplanationText = "{$dancinggoat.keytakeawayswidget.text.explanationtext$}", Order = 2)]
        public string Text { get; set; }
    }
}
