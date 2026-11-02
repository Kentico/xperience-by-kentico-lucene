using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers;

/// <summary>
/// Renders an inline background-image style on elements decorated with the "background-image-url" attribute.
/// No style is rendered when the URL is empty.
/// </summary>
[HtmlTargetElement(Attributes = BACKGROUND_IMAGE_URL_ATTRIBUTE)]
public class BackgroundImageStyleTagHelper : TagHelper
{
    private const string BACKGROUND_IMAGE_URL_ATTRIBUTE = "background-image-url";

    private readonly IUrlHelperFactory urlHelperFactory;


    /// <summary>
    /// URL of the background image.
    /// </summary>
    [HtmlAttributeName(BACKGROUND_IMAGE_URL_ATTRIBUTE)]
    public string ImageUrl { get; set; }


    /// <summary>
    /// View context of the rendered view.
    /// </summary>
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }


    public BackgroundImageStyleTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        this.urlHelperFactory = urlHelperFactory;
    }


    /// <summary>
    /// Appends a background-image style attribute based on <see cref="ImageUrl"/>.
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (string.IsNullOrEmpty(ImageUrl))
        {
            return;
        }

        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var style = $"background-image: url('{urlHelper.Content(ImageUrl)}');";

        var existingStyle = output.Attributes["style"]?.Value?.ToString();
        if (!string.IsNullOrEmpty(existingStyle))
        {
            style = $"{existingStyle.TrimEnd(';')}; {style}";
        }

        output.Attributes.SetAttribute("style", style);
    }
}
