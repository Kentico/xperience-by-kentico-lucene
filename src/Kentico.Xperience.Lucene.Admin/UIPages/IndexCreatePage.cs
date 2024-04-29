using CMS.Membership;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Admin;
using Kentico.Xperience.Lucene.Core.Indexing;

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
    private readonly ILuceneIndexManager indexManager;
    private LuceneConfigurationModel? model = null;

    public IndexCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILuceneConfigurationStorageService storageService,
        ILuceneIndexManager indexManager,
        IPageUrlGenerator pageUrlGenerator)
        : base(formItemCollectionProvider, formDataBinder, storageService, indexManager)
    {
        this.pageUrlGenerator = pageUrlGenerator;
        this.indexManager = indexManager;
    }

    protected override LuceneConfigurationModel Model
    {
        get
        {
            model ??= new();

            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(LuceneConfigurationModel model, ICollection<IFormItem> formItems)
    {
        var result = await ValidateAndProcess(model);

        if (result == IndexModificationResult.Success)
        {
            var index = indexManager.GetRequiredIndex(model.IndexName);

            var successResponse = NavigateTo(pageUrlGenerator.GenerateUrl<IndexEditPage>(index.Identifier.ToString()))
                .AddSuccessMessage("Index created.");

            return await Task.FromResult<ICommandResponse>(successResponse);
        }

        var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure))
            .AddErrorMessage("Could not create index.");

        return await Task.FromResult<ICommandResponse>(errorResponse);
    }
}
