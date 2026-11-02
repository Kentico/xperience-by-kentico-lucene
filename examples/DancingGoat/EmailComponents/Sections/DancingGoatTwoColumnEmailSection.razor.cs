using DancingGoat.EmailComponents;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailSection(
    identifier: DancingGoatTwoColumnEmailSection.IDENTIFIER,
    name: "{$dancinggoat.twocolumneemailsection.title$}",
    componentType: typeof(DancingGoatTwoColumnEmailSection),
    IconClass = "icon-l-cols-2")]

namespace DancingGoat.EmailComponents;

/// <summary>
/// Basic section with two columns.
/// </summary>
public partial class DancingGoatTwoColumnEmailSection : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatTwoColumnEmailSection)}";
}
