using System.Threading.Tasks;

using CMS.Commerce;

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers;

[HtmlTargetElement("price")]
public class PriceTagHelper : TagHelper
{
    private readonly IPriceFormatter priceFormatter;


    public PriceTagHelper(IPriceFormatter priceFormatter)
    {
        this.priceFormatter = priceFormatter;
    }


    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "price";
        var content = (await output.GetChildContentAsync()).GetContent();

        if (decimal.TryParse(content, out var amount))
        {
            output.Content.SetContent(priceFormatter.Format(amount, new PriceFormatContext()));
        }
        else
        {
            output.Content.SetContent(content);
        }
    }
}
