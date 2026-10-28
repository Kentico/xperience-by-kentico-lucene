using DancingGoat.EmailComponents;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DancingGoatTextWidget.IDENTIFIER,
    name: "{$dancinggoat.textwidget.title$}",
    componentType: typeof(DancingGoatTextWidget),
    PropertiesType = typeof(DancingGoatTextWidgetProperties),
    IconClass = "icon-l-header-text",
    Description = "{$dancinggoat.textwidget.description$}"
    )]

namespace DancingGoat.EmailComponents;

/// <summary>
/// Text widget component.
/// </summary>
public partial class DancingGoatTextWidget : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatTextWidget)}";


    private EmailContext emailContext;


    /// <summary>
    /// The widget properties.
    /// </summary>
    [Parameter]
    public DancingGoatTextWidgetProperties Properties { get; set; } = null!;


    /// <summary>
    /// Gets or sets the email context accessor service.
    /// </summary>
    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; } = null!;


    /// <summary>
    /// Gets the current email context.
    /// </summary>
    private EmailContext EmailContext => emailContext ??= EmailContextAccessor.GetContext();
}
