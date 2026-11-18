using CMS.ContentEngine;

using Kentico.Builder.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace DancingGoat.EmailComponents;

/// <summary>
/// Configurable properties of the <see cref="DancingGoatTextWidget"/>.
/// </summary>
public class DancingGoatTextWidgetProperties : AiProvenanceWidgetProperties, IEmailWidgetProperties
{
    /// <summary>
    /// The widget content.
    /// </summary>
    [RichTextEditorComponent(Label = "{$dancinggoat.textwidget.text.label$}", Order = 1)]
    [TrackContentItemReference(typeof(ContentItemReferenceExtractor))]
    public string Text { get; set; } = string.Empty;
}
