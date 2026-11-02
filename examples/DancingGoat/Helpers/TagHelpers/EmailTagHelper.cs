using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DancingGoat.Helpers
{
    /// <summary>
    /// Renders an <c>&lt;email address="..." /&gt;</c> element as a mailto link, or nothing
    /// when no address is set.
    /// </summary>
    public sealed class EmailTagHelper : TagHelper
    {
        /// <summary>
        /// E-mail address to link to.
        /// </summary>
        public string Address { get; set; }


        /// <inheritdoc/>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (string.IsNullOrEmpty(Address))
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "a";
            output.Attributes.SetAttribute("href", "mailto:" + Address);
            output.Content.SetContent(Address);
            output.TagMode = TagMode.StartTagAndEndTag;
        }
    }
}
