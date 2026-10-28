# Dancing Goat Lucene replacement analysis

## Goal

Replace `examples\DancingGoat` with the `kentico-xperience-sample-mvc` template on Xperience 31.10.0, then restore and update the Lucene-backed search experience for the redesigned app. The app will not use CI to persist Lucene configuration.

## Existing Lucene integration

- `DancingGoat.csproj` references `Kentico.Xperience.Lucene` conditionally for local NuGet use and also references the repository project directly.
- `Program.cs` calls `AddKenticoDancingGoatLuceneServices`.
- `Search\DancingGoatSearchStartupExtensions.cs` registers three strategies, a Czech analyzer, crawler/sanitizer services, and two search services.
- `Search\SimpleSearchIndexingStrategy.cs` indexes a home-page banner title.
- `Search\AdvancedSearchIndexingStrategy.cs` indexes article, home-page, and product pages using rendered page content, with title sorting and content-type facets. It also reindexes pages when linked teaser or banner content changes.
- `Search\ReusableContentItemsIndexingStrategy.cs` indexes reusable banner items through their associated home page.
- `Search\Services\SimpleSearchService.cs` and `AdvancedSearchService.cs` implement query, ranking, paging, sorting, and facet behavior.
- `Search\SearchController.cs` exposes a faceted MVC search and a simple endpoint. `Views\Search\Index.cshtml` renders the faceted search UI.
- `LuceneStorageModule.cs` maps index storage to Azure Blob Storage in cloud environments; local development uses local storage.
- `appsettings.json` contains `CMSLuceneSearch` settings and Lucene admin client module settings.
- `App_Data\CIRepository\@global` contains two index definitions and associated type, language, and included-path records. The configuration is English-only, for channel `DancingGoatPages`, with the sample records tied to the legacy content types.

## Existing configuration issues

- The search UI queries index `Advanced`, but CI assigns `ReusableContentItemsIndexingStrategy` to that index.
- The advanced strategy is assigned to index `ddddd`.
- The simple endpoint queries index `Simple`, but no `Simple` index definition exists in CI.
- Some content types configured in CI are not handled by the corresponding indexing strategies.
- The current content-type mappings and page-rendering assumptions should not be carried over without checking the replacement template's models and UX.

## Decisions

- Build one global search experience with facets, paging, and sorting.
- Ensure a named sample index exists, even if other Lucene indexes already exist in the database.
- Use local Lucene storage only; do not retain the current Azure Blob storage mapping.
- Add `examples\Directory.Packages.props` importing the root package props, then update the needed Xperience package versions to 31.10.0 while inheriting other package versions.
- The replacement app will not use CI to persist Lucene configuration.

## Implementation plan

1. Replace the Dancing Goat app with the `kentico-xperience-sample-mvc` template at Xperience 31.10.0, preserving repository-level example build configuration as appropriate.
2. Add `examples\Directory.Packages.props` that imports the root `Directory.Packages.props`; update the required Xperience versions and retain inherited non-Xperience versions.
3. Inspect the new template's content types, fields, page routing, and redesigned UI. Decide the exact searchable content and facet values based on those models.
4. Integrate `Kentico.Xperience.Lucene` and implement a focused indexing strategy and search service that support the global faceted experience.
5. Add a simple admin module that runs at startup and ensures the named sample index and its required configuration records exist. Keep creation idempotent and do not depend on CI.
6. Configure local storage only. Remove obsolete cloud-specific Lucene storage mapping and any stale CI Lucene definitions left by the replacement.
7. Add or adapt the search UI and navigation for the redesigned app.
8. Validate restore/build and verify startup index creation when absent, no duplicate creation when present, and search behavior against the new content models.
