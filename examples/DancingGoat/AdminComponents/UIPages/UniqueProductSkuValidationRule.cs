using System;
using System.Threading.Tasks;

using CMS.ContentEngine;

using DancingGoat.AdminComponents.UIPages;
using DancingGoat.Commerce;

using Kentico.Xperience.Admin.Base.Authentication;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormValidationRule(UniqueProductSkuValidationRule.IDENTIFIER, typeof(UniqueProductSkuValidationRule), "Unique SKU value", "Checks whether the field does not contain a product SKU that is already being used.")]

namespace DancingGoat.AdminComponents.UIPages;

/// <summary>
/// Rule validates that underlying field does not contain a product SKU that is already used in another product.
/// </summary>
internal class UniqueProductSkuValidationRule : ValidationRule<string>
{
    public const string IDENTIFIER = "DancingGoat.UniqueSkuValidationRule";

    private readonly ProductSkuValidator productSkuValidator;
    private readonly IContentItemManagerFactory contentItemManagerFactory;
    private readonly IAuthenticatedUserAccessor authenticatedUserAccessor;


    public UniqueProductSkuValidationRule(ProductSkuValidator productSkuValidator, IContentItemManagerFactory contentItemManagerFactory,
        IAuthenticatedUserAccessor authenticatedUserAccessor)
    {
        this.productSkuValidator = productSkuValidator;
        this.contentItemManagerFactory = contentItemManagerFactory;
        this.authenticatedUserAccessor = authenticatedUserAccessor;
    }


    /// <summary>
    /// Returns <see cref="ValidationResult.Success"/> validation result if the product SKU is not used in another product; otherwise <see cref="ValidationResult.Fail"/>
    /// </summary>
    /// <param name="value">Value to be validated.</param>
    /// <param name="formFieldValueProvider">Provider of values of other form fields for contextual validation.</param>
    /// <returns>Returns the validation result.</returns>
    public override async Task<ValidationResult> Validate(string value, IFormFieldValueProvider formFieldValueProvider)
    {
        var contentItemFormContext = FormContext as IContentItemFormContextBase;
        if (contentItemFormContext == null)
        {
            throw new InvalidOperationException("The validation rule can only be used in a content item form context.");
        }

        int contentItemId = contentItemFormContext.ItemId;

        // Try to find a colliding content item using the provided SKU code
        int? collidingContentItemIdentifier = await productSkuValidator.GetCollidingContentItem(value, contentItemId);

        if (collidingContentItemIdentifier == null)
        {
            // The SKU code is unique, the validation passes 
            return ValidationResult.Success;
        }
        else
        {
            // The SKU code is already used in another product, the validation fails
            var user = await authenticatedUserAccessor.Get();
            if (user == null)
            {
                throw new InvalidOperationException("No authenticated user was found.");
            }

            var contentItemManager = contentItemManagerFactory.Create(user.UserID);

            var metadata = await contentItemManager.GetContentItemLanguageMetadata(collidingContentItemIdentifier.Value, contentItemFormContext.LanguageName);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Content item metadata with ID {contentItemId} was not found.");
            }

            return new(false, $"Product SKU is already being used in the product '{metadata.DisplayName}'.");
        }
    }
}
