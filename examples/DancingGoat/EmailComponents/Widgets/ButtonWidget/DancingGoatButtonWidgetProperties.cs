using DancingGoat.EmailComponents.Enums;

using Kentico.EmailBuilder.Web.Mvc;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;

namespace DancingGoat.EmailComponents;

/// <summary>
/// Configurable properties of the <see cref="DancingGoatButtonWidget"/>.
/// </summary>
public class DancingGoatButtonWidgetProperties : IEmailWidgetProperties
{
    /// <summary>
    /// The button text.
    /// </summary>
    [TextInputComponent(
        Label = "{$dancinggoat.buttonwidget.text.label$}",
        Order = 1,
        ExplanationText = "{$dancinggoat.buttonwidget.text.explanationtext$}")]
    public string Text { get; set; } = string.Empty;


    /// <summary>
    /// The URL linked by button.
    /// </summary>
    [UrlSelectorComponent(Label = "{$dancinggoat.buttonwidget.linkurl.label$}",
        Order = 2)]
    public string Url { get; set; }


    /// <summary>
    /// The button HTML element type. <see cref="ButtonType"/>
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.buttonwidget.buttontype.label$}",
        Order = 3,
        ExplanationText = "{$dancinggoat.buttonwidget.buttontype.explanationtext$}",
        Options = $"{nameof(DancingGoatButtonType.Button)};{{$dancinggoat.buttonwidget.buttontype.option.button$}}\r\n{{nameof(DancingGoatButtonType.Link)}};{{$dancinggoat.buttonwidget.buttontype.option.link$}}",
        OptionsValueSeparator = ";")]
    public string ButtonType { get; set; } = nameof(DancingGoatButtonType.Button);


    /// <summary>
    /// The horizontal alignment of the button. <see cref="DancingGoatHorizontalAlignment"/>
    /// </summary>
    [DropDownComponent(
        Label = "{$dancinggoat.buttonwidget.alignment.label$}",
        Order = 4,
        ExplanationText = "{$dancinggoat.buttonwidget.alignment.explanationtext$}",
        Options = $"{nameof(DancingGoatHorizontalAlignment.Left)};{{$dancinggoat.buttonwidget.alignment.option.left$}}\r\n{nameof(DancingGoatHorizontalAlignment.Center)};{{$dancinggoat.buttonwidget.alignment.option.center$}}\r\n{nameof(DancingGoatHorizontalAlignment.Right)};{{$dancinggoat.buttonwidget.alignment.option.right$}}",
        OptionsValueSeparator = ";")]
    public string ButtonHorizontalAlignment { get; set; } = nameof(DancingGoatHorizontalAlignment.Center);
}
