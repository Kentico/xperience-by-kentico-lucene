﻿using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Lucene.Models;
using Kentico.Xperience.Lucene.Services;
using Kentico.Xperience.Lucene.Services.Implementations;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using System.Text;

namespace Kentico.Xperience.Lucene.Admin;

public class EditIndex : ModelEditPage<LuceneConfigurationModel>
{
    [PageParameter(typeof(IntPageModelBinder))]
    public int IndexIdentifier { get; set; }


    private LuceneConfigurationModel model;
    private readonly IConfigurationStorageService storageService;

    public EditIndex(Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider formItemCollectionProvider,
                 IFormDataBinder formDataBinder,
                 IConfigurationStorageService storageService)
        : base(formItemCollectionProvider, formDataBinder)
    {
        model = new();
        this.storageService = storageService;
    }

    protected override LuceneConfigurationModel Model
    {
        get
        {
            if (model == null)
            {
                if (IndexIdentifier == -1)
                {
                    model = new LuceneConfigurationModel();
                }
                else
                {
                    model = storageService.GetIndexDataOrNull(IndexIdentifier).Result ?? new LuceneConfigurationModel();
                }
            }
            return model;
        }
    }

    private static string RemoveWhitespacesUsingStringBuilder(string source)
    {
        var builder = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            if (!char.IsWhiteSpace(c))
                builder.Append(c);
        }
        return source.Length == builder.Length ? source : builder.ToString();
    }

    protected override async Task<ICommandResponse> ProcessFormData(LuceneConfigurationModel model, ICollection<IFormItem> formItems)
    {
        model.IndexName = RemoveWhitespacesUsingStringBuilder(model.IndexName ?? "");

        if ((await storageService.GetIndexIds()).Any(x => x == model.Id))
        {
            bool edited = await storageService.TryEditIndex(model);

            var response = ResponseFrom(new FormSubmissionResult(edited
                                                            ? FormSubmissionStatus.ValidationSuccess
                                                            : FormSubmissionStatus.ValidationFailure));

            if (edited)
            {
                response.AddSuccessMessage("Index edited");

                LuceneSearchModule.AddRegisteredIndices();
            }
            else
            {
                response.AddErrorMessage(string.Format("Editing failed."));
            }

            return response;
        }
        else
        {
            bool created;
            if (string.IsNullOrWhiteSpace(model.IndexName))
            {
                Response().AddErrorMessage(string.Format("Invalid Index Name"));
                created = false;
            }
            else
            {
                created = await storageService.TryCreateIndex(model);
            }

            var response = ResponseFrom(new FormSubmissionResult(created
                                                            ? FormSubmissionStatus.ValidationSuccess
                                                            : FormSubmissionStatus.ValidationFailure));

            if (created)
            {
                response.AddSuccessMessage("Index created");

                model.StrategyName ??= "";

                IndexStore.Instance.AddIndex(new LuceneIndex(
                    new StandardAnalyzer(LuceneVersion.LUCENE_48),
                    model.IndexName ?? "",
                    model.ChannelName ?? "",
                    model.LanguageNames?.ToList() ?? [],
                    model.Id,
                    model.Paths ?? [],
                    indexPath: null,
                    luceneIndexingStrategy: (ILuceneIndexingStrategy)(Activator.CreateInstance(StrategyStorage.Strategies[model.StrategyName]) ?? new DefaultLuceneIndexingStrategy()
                )));
            }
            else
            {
                response.AddErrorMessage(string.Format("Index creating failed."));
            }

            return response;
        }
    }
}