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
        Label = "Button text",
        Order = 1,
        ExplanationText = "Enter the text displayed as the button's caption.")]
    public string Text { get; set; } = string.Empty;


    /// <summary>
    /// The URL linked by button.
    /// </summary>
    [UrlSelectorComponent(Label = "Link URL",
        Order = 2)]
    public string Url { get; set; }


    /// <summary>
    /// The button HTML element type. <see cref="ButtonType"/>
    /// </summary>
    [DropDownComponent(
        Label = "Button type",
        Order = 3,
        ExplanationText = "Choose how the button is displayed.",
        Options = $"{nameof(DancingGoatButtonType.Button)};Button\r\n{nameof(DancingGoatButtonType.Link)};Link",
        OptionsValueSeparator = ";")]
    public string ButtonType { get; set; } = nameof(DancingGoatButtonType.Button);


    /// <summary>
    /// The horizontal alignment of the button. <see cref="DancingGoatHorizontalAlignment"/>
    /// </summary>
    [DropDownComponent(
        Label = "Alignment",
        Order = 4,
        ExplanationText = "Select how you want to position the button",
        Options = $"{nameof(DancingGoatHorizontalAlignment.Left)};Left\r\n{nameof(DancingGoatHorizontalAlignment.Center)};Center\r\n{nameof(DancingGoatHorizontalAlignment.Right)};Right",
        OptionsValueSeparator = ";")]
    public string ButtonHorizontalAlignment { get; set; } = nameof(DancingGoatHorizontalAlignment.Center);
}
