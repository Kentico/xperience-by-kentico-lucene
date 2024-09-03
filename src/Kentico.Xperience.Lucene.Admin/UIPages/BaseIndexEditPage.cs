using System.Text;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Core.Indexing;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Kentico.Xperience.Lucene.Admin;

internal abstract class BaseIndexEditPage : ModelEditPage<LuceneConfigurationModel>
{
    protected readonly ILuceneConfigurationStorageService StorageService;

    private readonly ILuceneIndexManager indexManager;

    protected BaseIndexEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILuceneConfigurationStorageService storageService,
        ILuceneIndexManager indexManager)
        : base(formItemCollectionProvider, formDataBinder)
    {
        this.indexManager = indexManager;
        StorageService = storageService;
    }

    protected async Task<IndexModificationResult> ValidateAndProcess(LuceneConfigurationModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? string.Empty);

        if (StorageService.GetIndexIds().Exists(x => x == configuration.Id))
        {
            bool edited = await StorageService.TryEditIndexAsync(configuration.ToLuceneModel());

            if (edited)
            {
                return IndexModificationResult.Success;
            }

            return IndexModificationResult.Failure;
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(configuration.IndexName))
            {
                indexManager.AddIndex(configuration.ToLuceneModel());

                return IndexModificationResult.Success;
            }

            return IndexModificationResult.Failure;
        }
    }

    protected static string RemoveWhitespacesUsingStringBuilder(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (!char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }
        return source.Length == builder.Length ? source : builder.ToString();
    }
}

internal enum IndexModificationResult
{
    Success,
    Failure
}
