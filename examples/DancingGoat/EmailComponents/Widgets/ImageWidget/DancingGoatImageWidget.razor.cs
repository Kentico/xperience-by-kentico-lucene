using System;
using System.Linq;
using System.Threading.Tasks;

using DancingGoat.EmailComponents;
using DancingGoat.Models;

using Kentico.Content.Web.Mvc;
using Kentico.EmailBuilder.Web.Mvc;

using Microsoft.AspNetCore.Components;

[assembly: RegisterEmailWidget(
    identifier: DancingGoatImageWidget.IDENTIFIER,
    name: "{$dancinggoat.imagewidget.title$}",
    componentType: typeof(DancingGoatImageWidget),
    PropertiesType = typeof(DancingGoatImageWidgetProperties),
    IconClass = "icon-picture",
    Description = "{$dancinggoat.imagewidget.description$}"
    )]

namespace DancingGoat.EmailComponents;

/// <summary>
/// Image widget component.
/// </summary>
public partial class DancingGoatImageWidget : ComponentBase
{
    /// <summary>
    /// The component identifier.
    /// </summary>
    public const string IDENTIFIER = $"DancingGoat.{nameof(DancingGoatImageWidget)}";


    /// <summary>
    /// The URL of the image.
    /// </summary>
    private string Url { get; set; } = string.Empty;


    /// <summary>
    /// The alternative text for the image.
    /// </summary>
    private string AlternativeText { get; set; } = string.Empty;


    /// <summary>
    /// The email context accessor used to retrieve the current email context.
    /// </summary>
    [Inject]
    private IEmailContextAccessor EmailContextAccessor { get; set; }


    /// <summary>
    /// The content retriever used to retrieve content items.
    /// </summary>
    [Inject]
    private IContentRetriever ContentRetriever { get; set; }


    /// <summary>
    /// The widget properties.
    /// </summary>
    [Parameter]
    public DancingGoatImageWidgetProperties Properties { get; set; } = null!;


    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await BindProperties();
    }


    private async Task BindProperties()
    {
        var itemGuid = Properties.Assets?.Select(i => i.Identifier).FirstOrDefault();

        if (!itemGuid.HasValue || itemGuid.Value == Guid.Empty)
        {
            return;
        }

        var languageName = EmailContextAccessor.GetContext().LanguageName;

        var parameters = new RetrieveContentParameters
        {
            LanguageName = languageName,
            IsForPreview = false,
        };

        var image = (await ContentRetriever.RetrieveContentByGuids<Image>(
            [itemGuid.Value],
            parameters,
            query => query.TopN(1),
            new RetrievalCacheSettings(
                "TopN_1",
                useSlidingExpiration: true,
                cacheExpiration: TimeSpan.FromMinutes(1))))
            .FirstOrDefault();

        Url = image?.ImageFile?.Url ?? string.Empty;
        AlternativeText = image?.ImageShortDescription ?? string.Empty;
    }
}
