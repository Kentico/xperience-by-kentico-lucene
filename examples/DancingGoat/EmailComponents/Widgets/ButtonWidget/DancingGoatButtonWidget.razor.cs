using DancingGoat.EmailComponents;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DancingGoatButtonWidget.IDENTIFIER,
    name: "{$dancinggoat.buttonwidget.title$}",
    componentType: typeof(DancingGoatButtonWidget),
    PropertiesType = typeof(DancingGoatButtonWidgetProperties),
    IconClass = "icon-arrow-right-top-square",
    Description = "{$dancinggoat.buttonwidget.description$}"
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
