using DancingGoat.EmailComponents;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailSection(
    identifier: DancingGoatFullWidthEmailSection.IDENTIFIER,
    name: "Full-width section",
    componentType: typeof(DancingGoatFullWidthEmailSection),
    IconClass = "icon-l-header-text")]

namespace DancingGoat.EmailComponents;

/// <summary>
/// Basic section with one column.
/// </summary>
public partial class DancingGoatFullWidthEmailSection : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatFullWidthEmailSection)}";
}
