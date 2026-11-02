using CMS;
using CMS.Base;
using CMS.ContentEngine;
using CMS.Core;
using CMS.DataEngine;
using CMS.DataEngine.CollectionExtensions;

using DancingGoat.Models;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;
using Kentico.Xperience.Lucene.Core.Search;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(DancingGoat.Search.DancingGoatLuceneIndexModule))]

namespace DancingGoat.Search;

internal sealed class DancingGoatLuceneIndexModule : Module
{
    public DancingGoatLuceneIndexModule()
        : base(nameof(DancingGoatLuceneIndexModule))
    {
    }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        parameters.Services.GetRequiredService<LuceneModuleInstaller>().Install();

        var channelInfo = parameters.Services
            .GetRequiredService<IInfoProvider<ChannelInfo>>()
            .Get()
            .WhereEquals(nameof(ChannelInfo.ChannelName), DancingGoatConstants.WEBSITE_CHANNEL_NAME)
            .FirstOrDefault();

        if (channelInfo is null || channelInfo.ChannelType != ChannelType.Website)
        {
            throw new InvalidOperationException(
                $"Website channel '{DancingGoatConstants.WEBSITE_CHANNEL_NAME}' is required to create the Dancing Goat Lucene index.");
        }

        var includedPath = new LuceneIndexIncludedPath("/%")
        {
            ContentTypes =
            [
                new LuceneIndexContentType(ArticlePage.CONTENT_TYPE_NAME, "Article page", 0),
                new LuceneIndexContentType(ProductPage.CONTENT_TYPE_NAME, "Product page", 0)
            ]
        };

        var indexConfiguration = new LuceneIndexModel
        {
            IndexName = DancingGoatSearchService.INDEX_NAME,
            StrategyName = "DancingGoatGlobalSearchWithTaxonomyFacetsV2",
            AnalyzerName = "Standard",
            LanguageNames = ["en", "es"],
            Channels =
            [
                new LuceneIndexChannelConfiguration(
                    channelInfo.ChannelName,
                    channelInfo.ChannelDisplayName,
                    [includedPath])
            ],
            ReusableContentTypeNames =
            [
                ProductAccessory.CONTENT_TYPE_NAME,
                ProductBrewer.CONTENT_TYPE_NAME,
                ProductCoffee.CONTENT_TYPE_NAME,
                ProductGrinder.CONTENT_TYPE_NAME,
                ProductTemplateAlphaSize.CONTENT_TYPE_NAME
            ]
        };

        var configurationStorage = parameters.Services.GetRequiredService<ILuceneConfigurationStorageService>();
        var indexManager = parameters.Services.GetRequiredService<ILuceneIndexManager>();
        var existingConfiguration = configurationStorage
            .GetIndexDataOrNullAsync(DancingGoatSearchService.INDEX_NAME)
            .GetAwaiter()
            .GetResult();
        var configurationChanged = false;

        if (existingConfiguration is null)
        {
            indexManager.AddIndex(indexConfiguration);
            configurationChanged = true;
        }
        else if (!existingConfiguration.Channels.Any(channel =>
                     channel.WebsiteChannelName == channelInfo.ChannelName
                     && channel.IncludedPaths.Any(path => path.AliasPath == includedPath.AliasPath))
                 || existingConfiguration.StrategyName != indexConfiguration.StrategyName)
        {
            indexConfiguration.Id = existingConfiguration.Id;
            if (!configurationStorage.TryEditIndexAsync(indexConfiguration).GetAwaiter().GetResult())
            {
                throw new InvalidOperationException($"Could not update the Dancing Goat Lucene index configuration.");
            }

            configurationChanged = true;
        }

        var luceneClient = parameters.Services.GetRequiredService<ILuceneClient>();
        ApplicationEvents.Initialized.Execute += (sender, eventArgs) =>
        {
            var index = indexManager.GetRequiredIndex(DancingGoatSearchService.INDEX_NAME);
            var searchService = parameters.Services.GetRequiredService<ILuceneSearchService>();
            var documentCount = searchService.UseSearcher(index, searcher => searcher.IndexReader.NumDocs);

            if (configurationChanged || documentCount == 0)
            {
                luceneClient.Rebuild(DancingGoatSearchService.INDEX_NAME, CancellationToken.None).GetAwaiter().GetResult();
            }
        };
    }
}
