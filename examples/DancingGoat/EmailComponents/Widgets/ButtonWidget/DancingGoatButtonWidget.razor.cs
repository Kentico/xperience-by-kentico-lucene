using DancingGoat.EmailComponents;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DancingGoatButtonWidget.IDENTIFIER,
    name: "Button",
    componentType: typeof(DancingGoatButtonWidget),
    PropertiesType = typeof(DancingGoatButtonWidgetProperties),
    IconClass = "icon-arrow-right-top-square",
    Description = "Displays a button that opens a specified URL when clicked."
    )]

namespace DancingGoat.EmailComponents;

/// <summary>
/// Button widget component.
/// </summary>
public partial class DancingGoatButtonWidget : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatButtonWidget)}";


    /// <summary>
    /// The widget properties.
    /// </summary>
    [Parameter]
    public DancingGoatButtonWidgetProperties Properties { get; set; } = null!;
}
