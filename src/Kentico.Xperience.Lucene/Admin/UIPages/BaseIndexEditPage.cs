using System.Text;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Indexing;
using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Kentico.Xperience.Lucene.Admin;

internal abstract class BaseIndexEditPage : ModelEditPage<LuceneConfigurationModel>
{
    protected readonly ILuceneConfigurationStorageService StorageService;

    protected BaseIndexEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        ILuceneConfigurationStorageService storageService)
        : base(formItemCollectionProvider, formDataBinder) => StorageService = storageService;

    protected IndexModificationResult ValidateAndProcess(LuceneConfigurationModel configuration)
    {
        configuration.IndexName = RemoveWhitespacesUsingStringBuilder(configuration.IndexName ?? "");

        if (StorageService.GetIndexIds().Exists(x => x == configuration.Id))
        {
            bool edited = StorageService.TryEditIndex(configuration);

            if (edited)
            {
                LuceneSearchModule.AddRegisteredIndices();

                return IndexModificationResult.Success;
            }

            return IndexModificationResult.Failure;
        }
        else
        {
            bool created = !string.IsNullOrWhiteSpace(configuration.IndexName) && StorageService.TryCreateIndex(configuration);

            if (created)
            {
                LuceneIndexStore.Instance.AddIndex(new LuceneIndex(configuration, StrategyStorage.Strategies));

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
