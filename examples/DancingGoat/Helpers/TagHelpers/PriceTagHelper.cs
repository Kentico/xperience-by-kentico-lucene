using CMS.Commerce;

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers;

[HtmlTargetElement("price")]
public class PriceTagHelper : TagHelper
{
#pragma warning disable KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private readonly IPriceFormatter priceFormatter;


    public PriceTagHelper(IPriceFormatter priceFormatter)
    {
        this.priceFormatter = priceFormatter;
    }


    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "price";
        var content = output.GetChildContentAsync().Result.GetContent();

        if (decimal.TryParse(content, out var amount))
        {
            output.Content.SetContent(priceFormatter.Format(amount, new PriceFormatConfiguration()));
        }
        else
        {
            output.Content.SetContent(content);
        }
    }
#pragma warning restore KXE0002 // Commerce feature is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
