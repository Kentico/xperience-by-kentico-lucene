using System.Threading.Tasks;

using CMS.EmailMarketing;

using DancingGoat.EmailComponents;
using DancingGoat.Models;

using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailTemplate(
    identifier: DancingGoatEmailBuilderTemplate.IDENTIFIER,
    name: "{$dancinggoat.emailbuildertemplate.title$}",
    componentType: typeof(DancingGoatEmailBuilderTemplate),
    ContentTypeNames = ["DancingGoat.BuilderEmail"])
]

namespace DancingGoat.EmailComponents;

/// <summary>
/// The email builder template component.
/// </summary>
public partial class DancingGoatEmailBuilderTemplate : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatEmailBuilderTemplate)}";


    private BuilderEmail EmailModel { get; set; }


    private EmailRecipientContext EmailRecipientContext { get; set; }


    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; }


    [Inject]
    private IEmailRecipientContextAccessor EmailRecipientContextAccessor { get; set; }


    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var context = EmailContextAccessor.GetContext();
        EmailModel = await context.GetEmail<BuilderEmail>();

        EmailRecipientContext = EmailRecipientContextAccessor.GetContext();
    }
}
