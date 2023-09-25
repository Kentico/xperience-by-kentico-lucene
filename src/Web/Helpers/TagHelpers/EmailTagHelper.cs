using System.Web;

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers
{
    public class EmailTagHelper : TagHelper
    {
        public string Address { get; set; }


        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Address))
            {
                output.TagName = null;
                return;
            }

            output.TagName = "a";
            output.Attributes.SetAttribute("href", "mailto:" + HttpUtility.HtmlAttributeEncode(Address));
            output.Content.SetContent(HttpUtility.HtmlEncode(Address));
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}
