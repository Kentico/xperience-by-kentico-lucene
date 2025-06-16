using CMS.ContentEngine;

using Kentico.EmailBuilder.Web.Mvc;

namespace DancingGoat.EmailComponents;

/// <summary>
/// Configurable properties of the <see cref="DancingGoatTextWidget"/>.
/// </summary>
public class DancingGoatTextWidgetProperties : IEmailWidgetProperties
{
    /// <summary>
    /// The widget content.
    /// </summary>
    [TrackContentItemReference(typeof(ContentItemReferenceExtractor))]
    public string Text { get; set; } = string.Empty;
}
