using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Core;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.AiDisclosure;

/// <summary>
/// Wraps a field shown to website visitors in an AI scope: the AI origin label with a caption, and the field itself.
/// The label is a toggle that underlines the text of the field. Renders only the field when it is not marked.
/// </summary>
/// <remarks>
/// The markup is rendered by the <c>_AiScope</c> partial view.
/// </remarks>
/// <example>
/// <code>
/// &lt;ai-scope origin="@Model.GetAiProvenanceOrigin(nameof(ArticlePage.ArticlePageText))"&gt;
///     &lt;div class="prose"&gt;@Model.Text&lt;/div&gt;
/// &lt;/ai-scope&gt;
/// </code>
/// </example>
[HtmlTargetElement("ai-scope")]
public sealed class AiScopeTagHelper : TagHelper
{
    private readonly ILocalizationService localizationService;
    private readonly IHtmlHelper htmlHelper;


    /// <summary>
    /// Creates an instance of <see cref="AiScopeTagHelper"/> class.
    /// </summary>
    /// <param name="localizationService">Localization service resolving the DancingGoat resource strings.</param>
    /// <param name="htmlHelper">HTML helper rendering the partial view.</param>
    public AiScopeTagHelper(ILocalizationService localizationService, IHtmlHelper htmlHelper)
    {
        this.localizationService = localizationService;
        this.htmlHelper = htmlHelper;
    }


    /// <summary>
    /// AI origin of the wrapped field.
    /// </summary>
    public AiProvenanceOrigin Origin { get; set; }


    /// <summary>
    /// Position of the label relative to the field. Defaults to <see cref="AiLabelPosition.Before"/>.
    /// </summary>
    public AiLabelPosition LabelPosition { get; set; } = AiLabelPosition.Before;


    /// <summary>
    /// Renders the scope for a dark surface, such as the story band.
    /// </summary>
    public bool OnDarkSurface { get; set; }


    /// <summary>
    /// Context of the view the tag helper is rendered in.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; }


    /// <inheritdoc/>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null;

        if (Origin is not (AiProvenanceOrigin.AiGenerated or AiProvenanceOrigin.AiModified))
        {
            return;
        }

        var model = new AiScopeViewModel(
            Origin == AiProvenanceOrigin.AiGenerated ? "ai-generated" : "ai-modified",
            Origin == AiProvenanceOrigin.AiGenerated ? 114 : 102,
            GetResourceString("label"),
            GetResourceString("caption"),
            $"ai-scope-{context.UniqueId}",
            LabelPosition,
            OnDarkSurface,
            await output.GetChildContentAsync());

        ((IViewContextAware)htmlHelper).Contextualize(ViewContext);
        output.Content.SetHtmlContent(await htmlHelper.PartialAsync("_AiScope", model));
    }


    private string GetResourceString(string suffix) =>
        localizationService.GetString($"dancinggoat.aidisclosure.{Origin.ToString().ToLowerInvariant()}.{suffix}");
}
