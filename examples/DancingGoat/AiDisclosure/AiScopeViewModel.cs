using Microsoft.AspNetCore.Html;

namespace DancingGoat.AiDisclosure;

/// <summary>
/// View model of the <c>_AiScope</c> partial view rendered by <see cref="AiScopeTagHelper"/>.
/// </summary>
/// <param name="LabelFileName">File name of the label artwork in <c>wwwroot/Content/Images/AiLabels</c>, without extension.</param>
/// <param name="LabelWidth">Width of the label in pixels.</param>
/// <param name="LabelText">Text of the label, used as its accessible name.</param>
/// <param name="Caption">Caption shown next to the label.</param>
/// <param name="BodyId">Identifier of the element wrapping the field, controlled by the label.</param>
/// <param name="LabelPosition">Position of the label relative to the field.</param>
/// <param name="OnDarkSurface">Indicates whether the scope is rendered on a dark surface.</param>
/// <param name="Content">The wrapped field.</param>
public sealed record AiScopeViewModel(
    string LabelFileName,
    int LabelWidth,
    string LabelText,
    string Caption,
    string BodyId,
    AiLabelPosition LabelPosition,
    bool OnDarkSurface,
    IHtmlContent Content);
