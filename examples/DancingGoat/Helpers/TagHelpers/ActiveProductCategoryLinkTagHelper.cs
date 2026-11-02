using System;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers;

[HtmlTargetElement("a", Attributes = "asp-active")]
public class ActiveProductCategoryLinkTagHelper : TagHelper
{
    private readonly IUrlHelperFactory urlHelperFactory;


    [HtmlAttributeName("asp-active")]
    public string ActiveHref { get; set; }


    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }


    public ActiveProductCategoryLinkTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        this.urlHelperFactory = urlHelperFactory;
    }


    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);

        var currentPath = Normalize(ViewContext.HttpContext.Request.Path.Value);

        // Resolve ActiveHref using UrlHelper
        var activeHrefResolved = Normalize(urlHelper.Content(ActiveHref));

        if (!string.IsNullOrEmpty(currentPath) && !string.IsNullOrEmpty(activeHrefResolved) &&
            string.Equals(currentPath, activeHrefResolved, StringComparison.CurrentCultureIgnoreCase))
        {
            var existingClass = output.Attributes["class"]?.Value?.ToString() ?? "";
            output.Attributes.SetAttribute("class", $"{existingClass} active".Trim());
        }

        // Remove asp-active attribute so it doesn't appear in the rendered HTML
        output.Attributes.RemoveAll("asp-active");
    }


    private static string Normalize(string path)
    {
        return string.IsNullOrEmpty(path) ? path : path.TrimEnd('/');
    }
}
