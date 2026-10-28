using CMS.ContentEngine;
using CMS.Core;

using Microsoft.AspNetCore.Components;

namespace DancingGoat.AiDisclosure;

/// <summary>
/// Wraps a field shown to email recipients in an AI scope: the AI generated / AI modified label with a caption, and the field itself.
/// Email counterpart of the AI scope on the website. Renders only the field when it is not marked.
/// </summary>
/// <remarks>
/// The label is an image (<c>wwwroot/Content/Images/AiLabels</c>), because email clients do not support the SVG artwork used on the website.
/// Sent emails resolve the image path to an absolute URL. The label is not a toggle, because emails do not run scripts.
/// </remarks>
/// <example>
/// <code>
/// &lt;AiScope Origin="@Properties.GetAiProvenanceOrigin(nameof(Properties.Text))"&gt;
///     &lt;mj-text&gt;@Properties.Text&lt;/mj-text&gt;
/// &lt;/AiScope&gt;
/// </code>
/// </example>
public partial class AiScope : ComponentBase
{
    private const string LABELS_PATH = "/Content/Images/AiLabels/";


    /// <summary>
    /// AI origin of the wrapped field.
    /// </summary>
    [Parameter]
    public AiProvenanceOrigin Origin { get; set; }


    /// <summary>
    /// Position of the label relative to the field. Defaults to <see cref="AiLabelPosition.Before"/>.
    /// </summary>
    [Parameter]
    public AiLabelPosition LabelPosition { get; set; } = AiLabelPosition.Before;


    /// <summary>
    /// The wrapped field.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; }


    /// <summary>
    /// Localization service resolving the DancingGoat resource strings.
    /// </summary>
    [Inject]
    private ILocalizationService LocalizationService { get; set; }


    /// <summary>
    /// Gets the label for the <see cref="Origin"/>, or <c>null</c> when the field is not marked.
    /// </summary>
    private AiLabel Label => Origin switch
    {
        AiProvenanceOrigin.AiGenerated => new AiLabel(
            $"{LABELS_PATH}ai-generated.png",
            114,
            LocalizationService.GetString("dancinggoat.aidisclosure.aigenerated.label"),
            LocalizationService.GetString("dancinggoat.aidisclosure.aigenerated.caption")),
        AiProvenanceOrigin.AiModified => new AiLabel(
            $"{LABELS_PATH}ai-modified.png",
            102,
            LocalizationService.GetString("dancinggoat.aidisclosure.aimodified.label"),
            LocalizationService.GetString("dancinggoat.aidisclosure.aimodified.caption")),
        _ => null
    };


    /// <summary>
    /// AI origin label with its caption.
    /// </summary>
    /// <param name="ImagePath">Path of the label image.</param>
    /// <param name="Width">Width of the label image in pixels.</param>
    /// <param name="Text">Text of the label, used as the image alternative text.</param>
    /// <param name="Caption">Caption shown next to the label.</param>
    private sealed record AiLabel(string ImagePath, int Width, string Text, string Caption);
}
