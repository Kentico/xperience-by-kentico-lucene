using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: PageParameterConstants.PARAMETERIZED_SLUG,
   uiPageType: typeof(IndexEditPage),
   name: "Edit index",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

[UIEvaluatePermission(SystemPermissions.UPDATE)]
internal class IndexEditPage : BaseIndexEditPage
{
    private LuceneConfigurationModel? model = null;

    [PageParameter(typeof(IntPageModelBinder))]
    public int IndexIdentifier { get; set; }

    public IndexEditPage(Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
                 IFormDataBinder formDataBinder,
                 ILuceneConfigurationStorageService storageService,
                 ILuceneIndexManager indexManager)
        : base(formItemCollectionProvider, formDataBinder, storageService, indexManager) { }

    protected override LuceneConfigurationModel Model
    {
        get
        {
            model ??= StorageService.GetIndexDataOrNull(IndexIdentifier) ?? new();

            return model;
        }
    }

    protected override Task<ICommandResponse> ProcessFormData(LuceneConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = ValidateAndProcess(model);

        var response = ResponseFrom(new FormSubmissionResult(
            result == IndexModificationResult.Success
                ? FormSubmissionStatus.ValidationSuccess
                : FormSubmissionStatus.ValidationFailure));

        _ = result == IndexModificationResult.Success
            ? response.AddSuccessMessage("Index edited")
            : response.AddErrorMessage("Could not update index");

        return Task.FromResult<ICommandResponse>(response);
    }
}
