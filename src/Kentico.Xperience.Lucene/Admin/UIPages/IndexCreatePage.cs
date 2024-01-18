using CMS.Membership;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Indexing;
using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
   parentType: typeof(IndexListingPage),
   slug: "create",
   uiPageType: typeof(IndexCreatePage),
   name: "Create index",
   templateName: TemplateNames.EDIT,
   order: UIPageOrder.NoOrder)]

namespace Kentico.Xperience.Lucene.Admin;

[UIEvaluatePermission(SystemPermissions.CREATE)]
internal class IndexCreatePage : BaseIndexEditPage
{
    private readonly IPageUrlGenerator pageUrlGenerator;
    private LuceneConfigurationModel? model = null;

    public IndexCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILuceneConfigurationStorageService storageService,
        IPageUrlGenerator pageUrlGenerator)
        : base(formItemCollectionProvider, formDataBinder, storageService) => this.pageUrlGenerator = pageUrlGenerator;

    protected override LuceneConfigurationModel Model
    {
        get
        {
            model ??= new();

            return model;
        }
    }

    protected override Task<ICommandResponse> ProcessFormData(LuceneConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = ValidateAndProcess(model);

        if (result == IndexModificationResult.Success)
        {
            var index = LuceneIndexStore.Instance.GetRequiredIndex(model.IndexName);

            var successResponse = NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(index.Identifier.ToString()))
                .AddSuccessMessage("Index created.");

            return Task.FromResult<ICommandResponse>(successResponse);
        }

        var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
            .AddErrorMessage("Could not create index.");

        return Task.FromResult<ICommandResponse>(errorResponse);
    }
}
