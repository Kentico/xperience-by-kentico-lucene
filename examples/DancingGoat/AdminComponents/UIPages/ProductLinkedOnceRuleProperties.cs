using CMS.Core;

using Kentico.Xperience.Admin.Base.Forms;

namespace DancingGoat.AdminComponents.UIPages;

/// <summary>
/// Configuration properties for <see cref="ProductLinkedOnceRule"/>.
/// </summary>
public sealed class ProductLinkedOnceRuleProperties : ValidationRuleProperties
{
    /// <inheritdoc/>
    public override string GetDescriptionText(ILocalizationService localizationService)
        => localizationService.GetString("Checks whether product is already linked once.");
}
