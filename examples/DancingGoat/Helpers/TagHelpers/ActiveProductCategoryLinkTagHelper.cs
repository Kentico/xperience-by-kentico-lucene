using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers;

[HtmlTargetElement("a", Attributes = "asp-active")]
public class ActiveProductCategoryLinkTagHelper : TagHelper
{
    private readonly IUrlHelperFactory urlHelperFactory;
    private readonly IActionContextAccessor actionContextAccessor;


    [HtmlAttributeName("asp-active")]
    public string ActiveHref { get; set; }


    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; }


    public ActiveProductCategoryLinkTagHelper(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor)
    {
        this.urlHelperFactory = urlHelperFactory;
        this.actionContextAccessor = actionContextAccessor;
    }


    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var actionContext = actionContextAccessor.ActionContext;
        var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);

        var currentPath = ViewContext.HttpContext.Request.Path.Value?.ToLowerInvariant();

        // Resolve ActiveHref using UrlHelper
        var activeHrefResolved = urlHelper.Content(ActiveHref);

        if (!string.IsNullOrEmpty(currentPath) && !string.IsNullOrEmpty(activeHrefResolved) &&
            (currentPath == activeHrefResolved || currentPath.StartsWith(activeHrefResolved)))
        {
            var existingClass = output.Attributes["class"]?.Value?.ToString() ?? "";
            output.Attributes.SetAttribute("class", $"{existingClass} active".Trim());
        }

        // Remove asp-active attribute so it doesn't appear in the rendered HTML
        output.Attributes.RemoveAll("asp-active");
    }
}
