# Usage Guide

This library supports using Lucene.NET to index both unstructured and high structured, interrelated content in an Xperience by Kentico solution. This indexed content can then be programmatically queried and displayed in a website channel.

Below are the steps to integrate the library into your solution.

## Create a custom Indexing Strategy

See [Custom index strategy](Custom-index-strategy.md)

## Continuous Integration

When starting your application for the first time after adding this library to your solution, a custom module and custom module classes will automatically be created
to support managing search index configuration within the administration UI.

If you do not see new items added to your [CI repository](https://docs.xperience.io/x/FAKQC) for the new auto-generated Lucene search data types, stop your application and perform a [CI store](https://docs.xperience.io/xp/developers-and-admins/ci-cd/continuous-integration#ContinuousIntegration-Storeobjectdatatotherepository) to add the library's custom module configuration to the CI repository.

You should now be able to run a [CI restore](https://docs.xperience.io/xp/developers-and-admins/ci-cd/continuous-integration#ContinuousIntegration-Restorerepositoryfilestothedatabase).
Attempting to run a CI restore without the CI files in the CI repository will result in a SQL error during the restore.

When team members are merging changes that include the addition of this library, they _must_ first run a CI restore to ensure they have the same object metadata for the search custom module as your database.

Future updates to indexes will be tracked in the CI repository [unless they are excluded](https://docs.xperience.io/x/ygAcCQ).

## Managing search indexes

See [Managing search indexes](Managing-Indexes.md)

## Defining an index in code

Indexes can also be created from code, for example in a custom module, so every environment gets the same configuration.

1. Build a `LuceneIndexModel` with the index name, strategy name (as registered with `RegisterStrategy`), analyzer name, language names, reusable content type names, and one `LuceneIndexChannelConfiguration` per website channel. Use `new LuceneIndexChannelConfiguration(websiteChannelName, includedPaths)`; the channel display name is not stored, so there is no need to load it.
2. Read the stored configuration with `ILuceneConfigurationStorageService.GetIndexDataOrNullAsync(indexName)`.
3. If it does not exist, call `ILuceneConfigurationStorageService.TryCreateIndex(model)` and check the returned `bool`. `ILuceneIndexManager.AddIndex(model)` also works but ignores that result.
4. If it exists and differs, set `model.Id` to the stored `Id` and call `TryEditIndexAsync(model)`. Set `LuceneIndexIncludedPath.Identifier` to the stored path's identifier to keep an existing path; paths without an identifier replace the stored ones.
5. Rebuild the index (`ILuceneClient.Rebuild`) after creating it or changing anything that affects the stored documents: strategy, analyzer, languages, channels, paths, or content types. Changes to the strategy's mapping code also need a rebuild. Run the rebuild after `ApplicationEvents.Initialized` and do not block startup on it.

The library creates its configuration tables on `ApplicationEvents.Initialized`. If your module reads or writes index configuration in `OnInit`, call `LuceneModuleInstaller.Install()` first, or the tables may not exist yet on a new database.

If your project uses Continuous Integration, index configuration created in the administration is serialized like other objects, which is usually simpler than defining it in code. The DancingGoat example does this.

The following module creates an index on startup, or updates it when the stored configuration differs:

```csharp
using CMS;
using CMS.Base;
using CMS.Core;
using CMS.DataEngine;

using Kentico.Xperience.Lucene.Core;
using Kentico.Xperience.Lucene.Core.Indexing;

using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(SearchIndexModule))]

internal sealed class SearchIndexModule : Module
{
    private const string INDEX_NAME = "SiteSearch";

    public SearchIndexModule() : base(nameof(SearchIndexModule)) { }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        var services = parameters.Services;

        // Configuration tables are installed on ApplicationEvents.Initialized, which runs after OnInit.
        services.GetRequiredService<LuceneModuleInstaller>().Install();

        var desired = new LuceneIndexModel
        {
            IndexName = INDEX_NAME,
            StrategyName = "SiteSearchStrategy", // as registered with RegisterStrategy
            AnalyzerName = "Standard",
            LanguageNames = ["en"],
            Channels =
            [
                new LuceneIndexChannelConfiguration("MyWebsiteChannel",
                [
                    new LuceneIndexIncludedPath("/%") { ContentTypes = [new LuceneIndexContentType("MySite.ArticlePage", "Article page", 0)] }
                ])
            ]
        };

        var storage = services.GetRequiredService<ILuceneConfigurationStorageService>();
        var existing = storage.GetIndexDataOrNullAsync(INDEX_NAME).GetAwaiter().GetResult();
        bool changed;

        if (existing is null)
        {
            changed = storage.TryCreateIndex(desired);
        }
        else if (!IsSameConfiguration(existing, desired))
        {
            desired.Id = existing.Id;
            changed = storage.TryEditIndexAsync(desired).GetAwaiter().GetResult();
        }
        else
        {
            changed = false;
        }

        if (changed)
        {
            ApplicationEvents.Initialized.Execute += (_, _) =>
            {
                using (ExecutionContext.SuppressFlow())
                {
                    _ = Task.Run(() => services.GetRequiredService<ILuceneClient>().Rebuild(INDEX_NAME, CancellationToken.None));
                }
            };
        }
    }

    // Compare every part of the model (strategy, analyzer, languages, reusable types,
    // channels, paths, content types) so any change is saved and triggers a rebuild.
    private static bool IsSameConfiguration(LuceneIndexModel existing, LuceneIndexModel desired) => /* ... */ false;
}
```

`IsSameConfiguration` is left to you. Compare the whole model, not only the parts you expect to change, or edits such as a new content type or language are silently ignored on existing databases.

## Search index querying

See [Search index querying](Search-index-querying.md)

## Using Lucene Analyzer

See [Text analyzing](Text-analyzing.md)

## Indexing Secured Items

See [Indexing Secured Items](Indexing-Secured-Items.md)

## Implementing document decay

You can score indexed items by "freshness" or "recency" using several techniques, each with different tradeoffs.

1. Boost relevant fields by setting field boost (preferable method, but requires more work).
2. Boost one field with constant value, that is always present in search query (shown in the example project, less desirable method.

   The Downside of this method is that all documents get matched, usable only for scenarios where total number of result is not required).

3. Use a sort expression. Implementation details can be found in Lucene.NET unit tests, Lucene.NET implementations

> Small differences in boosts will be ignored by Lucene.

## Index Storage

See [Index Storage](Index-Storage.md) for configuring how and where Lucene indexes are stored.

## Auto-scaling Support

See [Auto-Scaling](Auto-Scaling.md)

## Upgrades and Uninstalling

See [Upgrades](Upgrades.md)
