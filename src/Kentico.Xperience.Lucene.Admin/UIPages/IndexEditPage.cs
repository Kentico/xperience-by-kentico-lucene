using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

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
            model ??= new LuceneConfigurationModel(StorageService.GetIndexDataOrNullAsync(IndexIdentifier).Result ?? new());

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(LuceneConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = await ValidateAndProcess(model);

        var response = ResponseFrom(new FormSubmissionResult(
            result == IndexModificationResult.Success
                ? FormSubmissionStatus.ValidationSuccess
                : FormSubmissionStatus.ValidationFailure));

        _ = result == IndexModificationResult.Success
            ? response.AddSuccessMessage("Index edited")
            : response.AddErrorMessage("Could not update index");

        return await Task.FromResult<ICommandResponse>(response);
    }
}
