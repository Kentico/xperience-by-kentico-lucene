using System.Text;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Indexing;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
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

    protected IndexModificationResult ValidateAndProcess(LuceneConfigurationModel model)
    {
        model.IndexName = RemoveWhitespacesUsingStringBuilder(model.IndexName ?? "");

        if (StorageService.GetIndexIds().Exists(x => x == model.Id))
        {
            bool edited = StorageService.TryEditIndex(model);

            if (edited)
            {
                LuceneSearchModule.AddRegisteredIndices();

                return IndexModificationResult.Success;
            }

            return IndexModificationResult.Failure;
        }
        else
        {
            bool created = !string.IsNullOrWhiteSpace(model.IndexName) && StorageService.TryCreateIndex(model);

            if (created)
            {
                LuceneIndexStore.Instance.AddIndex(new LuceneIndex(
                    new StandardAnalyzer(LuceneVersion.LUCENE_48),
                    model.IndexName ?? "",
                    model.ChannelName ?? "",
                    model.LanguageNames?.ToList() ?? new(),
                    model.Id,
                    model.Paths ?? new(),
                    indexPath: null,
                    luceneIndexingStrategyType: StrategyStorage.GetOrDefault(model.StrategyName)
                ));

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
