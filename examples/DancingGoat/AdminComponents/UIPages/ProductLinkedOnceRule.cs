using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CMS.ContentEngine;
using CMS.Core;

using DancingGoat.AdminComponents.UIPages;
using DancingGoat.Models;

using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormValidationRule(ProductLinkedOnceRule.IDENTIFIER, typeof(ProductLinkedOnceRule), "{$dancinggoat.formvalidationrule.productlinkedonce.name$}", "{$dancinggoat.formvalidationrule.productlinkedonce.description$}")]

namespace DancingGoat.AdminComponents.UIPages;


/// <summary>
/// Validates whether value for the field was set.
/// </summary>
/// <remarks>
/// The rule considers <see langword="null"/> value, empty string or empty collection as invalid values.
/// </remarks>
public sealed class ProductLinkedOnceRule : ValidationRule<ProductLinkedOnceRuleProperties, IEnumerable<ContentItemReference>>
{
    internal const string IDENTIFIER = "DancingGoat.ProductLinkedOnce";
    private readonly ILocalizationService localizationService;
    private readonly IContentQueryExecutor executor;


    /// <inheritdoc/>
    protected override string DefaultErrorMessage => localizationService.GetString("Product already linked once and can not be linked again.");


    /// <summary>
    /// Creates a new instance of <see cref="ProductLinkedOnceRule"/> class.
    /// </summary>
    public ProductLinkedOnceRule(ILocalizationService localizationService, IContentQueryExecutor executor)
    {
        this.localizationService = localizationService;
        this.executor = executor;
    }


    /// <summary>
    /// Validates whether product is linked only once.
    /// </summary>
    /// <param name="value">Value to be validated.</param>
    /// <param name="formFieldValueProvider">Provider of values of other form fields for contextual validation.</param>
    /// <returns>Returns the validation result.</returns>
    public override async Task<ValidationResult> Validate(IEnumerable<ContentItemReference> value, IFormFieldValueProvider formFieldValueProvider)
    {
        if (FormContext is not IContentItemFormContextBase contentItemFormContext)
        {
            throw new InvalidOperationException("The validation rule can only be used in a content item form context.");
        }

        if (!(value?.Any() ?? false))
        {
            return await ValidationResult.FailResult();
        }

        var contentItemId = contentItemFormContext.ItemId;

        var identifier = value.First().Identifier;

        var contentQueryBuilder = new ContentItemQueryBuilder()
            .ForContentTypes(a => a.OfReusableSchema(IProductFields.REUSABLE_FIELD_SCHEMA_NAME))
            .Parameters(p => p.Where(x => x.WhereEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemGUID), identifier))
                .Columns(nameof(IContentItemFieldsSource.SystemFields.ContentItemID)));

        var contentItem = (await executor.GetMappedResult<IContentItemFieldsSource>(contentQueryBuilder, new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true })).FirstOrDefault();

        if (contentItem == null)
        {
            return await ValidationResult.FailResult();
        }

        var queryBuilder = new ContentItemQueryBuilder()
            .ForContentType(ProductPage.CONTENT_TYPE_NAME, config => config.WithLinkedItems(1)
                .Linking(nameof(ProductPage.ProductPageProduct), [contentItem.SystemFields.ContentItemID])
                .Where(x => x.WhereNotEquals(nameof(IContentItemFieldsSource.SystemFields.ContentItemID), contentItemId))
        );

        var duplicateRecords = await executor.GetMappedResult<object>(queryBuilder, new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true });

        if (duplicateRecords.Any())
        {
            return await ValidationResult.FailResult();
        }

        return await ValidationResult.SuccessResult();
    }
}
