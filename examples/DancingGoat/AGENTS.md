# AGENTS.md

## Project overview

- **Name:** Dancing Goat
- **Stack:** ASP.NET Core MVC, Xperience by Kentico, Tailwind CSS + esbuild frontend pipeline
- **Purpose:** Sample specialty-coffee website and store demonstrating Xperience by Kentico content management, Page Builder, email marketing, and digital commerce.
- **Running app URL:** See `Properties/launchSettings.json`; the administration UI is at `/admin`.
- **Local administrator:** user `administrator`, password `Pass@12345`. It is stored in the CI repository, so every `--kxp-ci-restore` sets it. Local demo only.

## Repository layout

| Path                        | What lives there                                                                 |
| --------------------------- | -------------------------------------------------------------------------------- |
| `Components/`               | Page Builder widgets, sections, inline editors, form sections, view components   |
| `AdminComponents/`          | Admin UI customizations (excluded from live-site-only deployments)               |
| `Controllers/`              | MVC controllers (content pages, store, account, checkout)                        |
| `Models/`                   | Content type models — `*.generated.cs` files are generated, do not edit by hand  |
| `Views/`                    | Razor views and layouts                                                          |
| `EmailComponents/`          | Email Builder components                                                         |
| `Helpers/`                  | Tag helpers, generators, utilities                                               |
| `Search/`                   | Lucene search: index definition, indexing strategy, search service, controller   |
| `Styles/`                   | Stylesheet sources — top-level files are bundle entry points, shared building blocks in `Styles/partials/` (design tokens in `partials/theme.css`) |
| `wwwroot/Content/Styles/`   | Compiled `Site.css` / `Landing-page.css` (never edit compiled CSS)               |
| `wwwroot/Content/Fonts/`    | Self-hosted fonts (never link font CDNs)                                         |
| `Tools/`                    | Frontend build scripts (Tailwind CSS compile, component bundle concat + minify)  |

## Useful commands

| Task                      | Command                                     |
| ------------------------- | ------------------------------------------- |
| Run site                  | `dotnet run`                                |
| Build                     | `dotnet build`                              |
| Install frontend deps     | `npm install`                               |
| Recompile styles          | `npm run build:css`                         |
| Recompile styles on change | `npm run watch:css`                        |
| Rebuild component bundles | `npm run build:bundles`                     |
| Recompile styles + bundles | `npm run build`                            |
| Accessibility scan        | `npm run a11y -- <url> [<url> ...]`         |

## Content changes

If you change the site's content model (add or remove fields, define new content types or schemas, etc.), you must run the following commands to regenerate the code files.

`dotnet run -- --kxp-codegen` (see [docs](https://docs.kentico.com/documentation/developers-and-admins/api/generate-code-files-for-system-objects))

## Coding conventions

- Never hand-edit `Models/**/*.generated.cs` — change the content type using Management MCP and regenerate.
- File names must match class names exactly.
- One empty line at the end of every file.
- Avoid unnecessary inline comments.
- Use `CMS.IO` instead of `System.IO` for all file system operations.
- Avoid regions — they signal a class is doing too much.
- Do not use the ternary operator for complex multi-line or nested expressions, or when passing parameters — use a variable instead.
- No abbreviations or contractions in identifiers (`GetWindow`, not `GetWin`); no underscores or non-alphanumeric characters except in constants.
- Constants: ALL_CAPS with underscore separators. Non-public fields: camelCase noun/adjective, no `m` prefix.
- Collection properties: plural noun, never a `List`/`Collection` suffix (`Items`, not `ItemList`).

## Search

The site search uses this repository's Lucene library (project reference to `src/`). Code lives in `Search/`; content and configuration live in the CI repository.

| File | Role |
| ---- | ---- |
| `DancingGoatSearchIndexingStrategy.cs` | Maps article and product pages to Lucene documents (`INDEX_NAME`, `STRATEGY_NAME`), adds facet fields, and finds product pages to reindex when a linked product changes. Secured (members-only) pages are indexed with an `IsSecured` field (`CMSLuceneSearch:IncludeSecuredItems` is on in `appsettings.json`). |
| `DancingGoatSearchFacets.cs` | Single list of taxonomy facets (`TAXONOMY_FACETS`) and the content type facet constants. Used by the strategy, service, and controller. |
| `DancingGoatSearchService.cs` | Runs the query with drill sideways so facet counts reflect the other selected filters. Hides facet groups with no matches. Leaves out secured pages unless the visitor is signed in. Handles sort, paging, highlighting. |
| `DancingGoatSearchController.cs` | Route handler for the `DancingGoat.SearchPage` page type. Returns a `TemplateResult` with the search view model. |
| `DancingGoatSearchIndexRebuildModule.cs` | Rebuilds the index on startup when it has no documents. |

The page template view is `PageTemplates/SearchPage/_DancingGoat_SearchPage.cshtml`. It builds every search link from the current page path (`DancingGoatSearchPageViewModel.GetUrl`), so language versions of the page work without extra routing.

Stored in the CI repository (`App_Data/CIRepository`), not in code:

- The `DancingGoatGlobal` index: channel `DancingGoatPages`, path `/%`, article and product page types, reusable product types, `en` and `es`, Standard analyzer.
- The `DancingGoat.SearchPage` page type and its pages (`/Search` tree path; `/search` and `/es/buscar` URLs).
- The `ArticleTopics` taxonomy and the `ArticlePageTopics` field on `DancingGoat.ArticlePage`.

Change these with the Management MCP (or the administration) while the site runs with CI enabled, then run `dotnet run -- --kxp-ci-store` and commit the `App_Data/CIRepository` changes with the code. Changes to the Lucene index itself need the admin UI (Lucene application); the Management MCP does not cover it.

### Add a content type to the index

1. Add the page type to the index's included path in the admin UI (Lucene application), and its reusable types if fields come from linked items.
2. Map it in `DancingGoatSearchIndexingStrategy.MapToLuceneDocumentOrNull`; add a content type constant to `DancingGoatSearchFacets` and a tab in the view if it needs one.
3. Rebuild the index in the admin UI, then store and commit the CI repository changes.

### Add a taxonomy facet

1. Create the taxonomy and add a taxonomy field to the content type (Management MCP), then regenerate models with `dotnet run -- --kxp-codegen`.
2. Add the taxonomy name to `DancingGoatTaxonomyConstants`.
3. Add a `DancingGoatSearchFacetDefinition` to `DancingGoatSearchFacets.TAXONOMY_FACETS` (dimension, query parameter, label, taxonomy name, tag selector that pattern-matches the indexed source, e.g. `source => (source as ArticlePage)?.ArticlePageTopics`).
4. Add the Spanish label to `Resources/SharedResources.es.resx`.
5. Rebuild the index in the admin UI (Lucene application). The indexing strategy, query, filters, and view pick up the new facet without other changes.
6. Store and commit the CI repository changes.

## Kentico MCP Servers

Both servers are configured in `.mcp.json` at the repository root. The values are committed, and the Management MCP URL
and secret mirror the `ManagementApi` section of `appsettings.Development.json`.

- **Kentico Docs MCP** (`kentico-docs-mcp`) is the **primary source** for any question about Xperience by Kentico APIs, configuration, and usage patterns. Prefer it over web search and over prior knowledge.
- **Kentico Management MCP** (`xperience-management-mcp`) is used to work with content in the **running local** instance (content types, content items, pages, Page Builder). Prefer it over manual changes in the admin UI.

## Validation of changes

- Always build the project after making changes.
- After changes under `Styles/`, recompile styles, verify the served CSS updated, and commit the updated compiled CSS.
- Always validate user-facing changes in the browser for content, layout, styling, and localization correctness before committing.
- After changes to views, layouts, or widgets, run an axe-core scan of the affected pages to find any accessibility issues: `npm run a11y -- <url>` (the site must be running at `http://localhost:14536`, set in `Properties/launchSettings.json`).
