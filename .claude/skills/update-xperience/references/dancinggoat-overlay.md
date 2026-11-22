# DancingGoat Lucene overlay

Preserve these customizations when merging new pristine samples. The generated
sample is not a replacement for this repository. Library minimum Xperience
31.0.0, sample 31.10.0, SDK 11 and .NET 10 targets are separate contracts.
Do not change public APIs, package graph, sample identities, or existing tests.

| Path (relative to `examples/DancingGoat`) | Retained overlay |
| --- | --- |
| `DancingGoat.csproj` | Intentional aggregate `ProjectReference` to `../../src/Kentico.Xperience.Lucene/Kentico.Xperience.Lucene.csproj`; NOT separate Core/Admin references. UserSecretsId `ecf97e0b-2de4-4b70-a247-e9e3ab70b113`, net10.0, separated-admin conditions, management API, MJML, AngleSharp and Lucene.Net.Highlighter references. No LOCAL_NUGET emulation. |
| `Program.cs` | `using DancingGoat.Search`, `SearchPage.CONTENT_TYPE_NAME` in Page Builder types, `AddDancingGoatLuceneServices(builder.Configuration)`, local environment and Management API registrations/middleware; existing localization, membership and routes. |
| `Search/` | Preserve **every** file: indexing strategy, facets and facet definition, service, controller, page/result models, startup extensions and index rebuild module. |
| `Search/DancingGoatSearchIndexingStrategy.cs` | Article/product document mapping, linked-product reindexing, secured-item field, content-type facet and taxonomy facets. Keep `INDEX_NAME` and `STRATEGY_NAME`. |
| `Search/DancingGoatSearchService.cs` | Analyzer-based boosted phrase/term query, language and member filtering, drill-sideways facet counts, localized labels, sort, paging and highlighting. |
| `Search/DancingGoatSearchFacets.cs` and `DancingGoatSearchFacetDefinition.cs` | Shared taxonomy selectors/dimensions/query parameters, product categories/tags/manufacturers/processing/tastes and article topics. |
| `Search/DancingGoatSearchController.cs` and page/result models | SearchPage route handler, TemplateResult, language-aware GetUrl links, facet selection and result presentation. |
| `Search/DancingGoatSearchStartupExtensions.cs` and rebuild module | Full Lucene registration/strategy setup; startup rebuild only when the index is empty. |
| `PageTemplates/SearchPage/_DancingGoat_SearchPage.cshtml` | Localized faceted search template, editable area, filters/tabs/paging, current-page links, highlights and member-only labels. |
| `Models/`, `Resources/`, `Styles/`, `Scripts/`, `wwwroot/`, `Components/`, `PageTemplates/` | Keep existing generated search/article taxonomy models, taxonomy constants, English/Spanish strings, search styling/assets, layouts and all existing site customizations. Never hand-edit generated models or replace compiled assets with template placeholders. |
| `appsettings.json` | Existing database and connection defaults, committed CMSHashStringSalt and `CMSLuceneSearch:IncludeSecuredItems`; never rotate salt or re-sign CI data during tooling updates. |
| `appsettings.Development.json` | Existing Management API identity/secret/port; SMTP localhost:1025; system email localhost:14536; queue/contact intervals; 48-hour local admin session; Proxy module `kentico-xperience-integrations-lucene-admin` on 3074. |
| `appsettings.CI.json` | Mailpit/system email at the same site port; no Proxy setting, so CI exercises embedded admin assets. |
| `LocalEnvironmentConfiguration.cs` | Repository-owned SMTP/options binding and configurable admin session, limited to Development and CI; production retains existing behavior. |
| `Properties/launchSettings.json` | Existing site port 14536 and sample profile. |
| `App_Data/CIRepository/` | Preserve **all existing objects** including Lucene index/path/channel/content-type settings, DancingGoatGlobal, DancingGoatPages, article/product types, reusable product types, languages en/es, Standard analyzer, SearchPage and localized `/search` / `/es/buscar` pages, ArticleTopics and ArticlePageTopics, users and channel domains. Never hand-merge, regenerate or re-sign CI objects; use Xperience CLI/admin changes and review the stored result. |
| `AGENTS.md`, `README.md`, `.npmrc`, `package.json`, tools and assets | Keep existing sample guidance/frontend behavior. Canonical shared design skill lives at root `.claude/skills/design-conventions`; no nested skill copies. |
| `packages.lock.json` | Regenerate after actual dependency changes; never hand-merge. |

Root-owned files, stripped from pristine samples by the update script:

- `Directory.Packages.props`: sample versions live in `examples/Directory.Packages.props`, importing root library minimums.
- `.config/dotnet-tools.json`: root manifest owns dbmanager (sample version) and retains AzureSignTool.
- `.mcp.json`: root retains Docs, Management (pinned to sample version), and Aspire; preserve management port/secret.
- `.claude/skills` and `.github/skills`: consolidate under root `.claude/skills`; design paths are relative to `examples/DancingGoat/` for both Copilot and Claude.

Outside the sample, preserve all three package projects, admin client identity
`kentico-xperience-integrations-lucene-admin` / port 3074, and every existing NUnit
test/fixture/reference. Future library Xperience raises require explicit approval.
