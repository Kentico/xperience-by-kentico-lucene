# Dancing Goat: Lucene search example

This project is the Xperience by Kentico Dancing Goat sample site (a fictional coffee shop) with site search built on [Xperience by Kentico Lucene](../../README.md). It references the library projects in `../../src` directly, so library changes are picked up when you run the example.

## What the example shows

All search code is in [`Search/`](./Search/):

| File                                   | What it shows                                                                                                                                                                                                                        |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `DancingGoatSearchIndexingStrategy.cs` | Custom indexing strategy. Maps article and product pages to Lucene documents, extracts plain text from rich text, adds a content type facet and taxonomy facets, and reindexes product pages when a linked product item changes. |
| `DancingGoatSearchFacets.cs`           | The single list of taxonomy facets (product categories, tags, manufacturers, coffee processing, coffee tastes, article topics) shared by the strategy, service, and controller. |
| `DancingGoatSearchService.cs`          | Querying. Boosted phrase and term queries built with the index analyzer, language filtering, drill-sideways facet counts, A-Z / Z-A sorting, paging, taxonomy tag labels in the current language, and result highlighting with `Lucene.Net.Highlighter`. |
| `DancingGoatSearchController.cs`       | Handles the `DancingGoat.SearchPage` page type and returns a Page Builder `TemplateResult`. The page's language selects the indexed language. |
| `DancingGoatSearchIndexRebuildModule.cs` | Rebuilds the index on startup when it is empty (for example, right after setup, because index files are not in source control). |
| `DancingGoatSearchStartupExtensions.cs` | Registers the Lucene services and the custom strategy.                                                                                                                                                                              |

The results page is the `PageTemplates/SearchPage/_DancingGoat_SearchPage.cshtml` page template. It has an editable area above the results.

Content and configuration that search depends on is stored in the CI repository (`App_Data/CIRepository`):

- The `DancingGoatGlobal` index (Lucene application in the administration)
- The `DancingGoat.SearchPage` page type and the `/search` (`/es/buscar`) pages
- The `ArticleTopics` taxonomy and the `ArticlePageTopics` field on article pages

## Run the example

### Prerequisites

- Exact .NET 11 SDK from [`global.json`](../../global.json), with .NET 10 runtime/targets
- SQL Server 2019 or newer
- Node.js version from `engines` in [`src/Kentico.Xperience.Lucene.Admin/Client/package.json`](../../src/Kentico.Xperience.Lucene.Admin/Client/package.json) (needed for the admin UI dev server)
- An Xperience by Kentico [license key](https://docs.kentico.com/documentation/developers-and-admins/installation/licenses) saved in a text file

### 1. Initialize a local database

Use the shared root `setup-local` skill, or run from the repository root after a
Release build:

```powershell
dotnet tool restore
dotnet restore Kentico.Xperience.Lucene.slnx --locked-mode
dotnet build examples\DancingGoat --configuration Release --no-restore -p:SIGN_FILE=false
.\scripts\Initialize-Database.ps1 -DatabaseName "<new-local-database>" -LicenseFile "<license_file_path>" -Configuration Release
```

The root dbmanager tool follows the sample's Xperience version (31.10.0); library
minimum remains 31.6.0. The helper preserves the committed macro salt, saves the
connection in the sample's existing user-secrets store, restores Lucene's CI
objects and enables CI. Choosing a new database changes the sample's local
user-secrets connection; `-SaveTo None` avoids that and requires a runtime
connection override. Never recreate an existing database without approval.

Existing SQL Server and Mailpit are the default; SMTP uses localhost:1025 and
Mailpit UI localhost:8025. Container-managed services are an explicit AppHost
alternative. `aspire run` starts the site and admin client together.
Development configures email processing and a 48-hour admin session; CI uses
embedded client assets. Production settings are unchanged.

### Manual database setup (alternative)

The site's content is included in `Data/Template.zip`. The `kentico-xperience-dbmanager` tool creates the database from it and writes the `CMSConnectionString` to `appsettings.json`.

Run from this folder:

```powershell
dotnet tool restore
dotnet kentico-xperience-dbmanager -- -s "<sql_server_name>" -d "xperience-by-kentico-lucene" -a "Pass@12345" --hash-string-salt "<hash_string_salt>" --license-file "<license_file_path>"
```

- The administrator password is `Pass@12345`. The CI repository stores the `administrator` user, so the restore in the next step sets this password whatever you pass to `-a`. It is a local demo password; don't reuse it anywhere else.

- Add `-u` and `-p` for SQL authentication. Without them Windows authentication is used.
- Use the `CMSHashStringSalt` value from `appsettings.json` for `--hash-string-salt`.
- Run `dotnet kentico-xperience-dbmanager -- --help` for all options.

See [Create the project database](https://docs.kentico.com/documentation/developers-and-admins/installation#create-the-project-database) for details.

`appsettings.json` contains a default connection string for a local SQL Server (`localhost,1433`, `sa` user). If you use a different server, don't commit your connection string. Use [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or revert the change before committing.

### 2. Restore the CI repository

`Template.zip` holds the stock Dancing Goat content. The search-specific objects (index, search page, article topics) are in the CI repository. Turn on Continuous Integration and restore them into the new database:

```powershell
dotnet run -- --kxp-ci-enable
dotnet run -- --kxp-ci-restore
```

With CI on, changes you make in the administration are written to `App_Data/CIRepository`. Commit them with the code that depends on them. See [Continuous Integration](https://docs.kentico.com/documentation/developers-and-admins/ci-cd/continuous-integration).

### 3. Start the admin UI dev server

`appsettings.Development.json` configures the Lucene admin client module in proxy mode:

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-integrations-lucene-admin": {
    "Mode": "Proxy",
    "Port": 3074
  }
}
```

In this mode the administration loads the Lucene admin UI scripts from a webpack dev server on port `3074` (set in `src/Kentico.Xperience.Lucene.Admin/Client/webpack.config.js`). Start it before you open the Search application in the administration:

```powershell
cd ../../src/Kentico.Xperience.Lucene.Admin/Client
npm install
npm run start
```

See [Prepare your environment for admin development](https://docs.kentico.com/documentation/developers-and-admins/customization/extend-the-administration-interface/prepare-your-environment-for-admin-development) for more on proxy mode.

### 4. Run the site

Run from this folder:

```powershell
dotnet run
```

The site runs at `http://localhost:14536` (see `Properties/launchSettings.json`). The administration is at `/admin`. Sign in as `administrator` with the password `Pass@12345`.

Some articles are members-only. Search shows them, with a "Members only" label, only to signed-in members. Register a member account on the site (**Sign in → Register**) to see them.

On the first start, the empty `DancingGoatGlobal` index is rebuilt automatically. Open `http://localhost:14536/search` (or `/es/buscar`) to try it.

In VS Code you can also use the `.NET Launch (DancingGoat)` launch configuration.

## AI-assisted development

The project is set up for AI coding assistants: root and sample `AGENTS.md`, shared skills in root `.claude/skills` (for both Copilot and Claude), and Kentico [Documentation MCP](https://docs.kentico.com/x/mcp_server_xp), [Management MCP](https://docs.kentico.com/x/management_api_xp) and Aspire servers in root `.mcp.json`. Design guidance is scoped to this sample.

To learn more about Kentico's suite of AI development tools, see the [KentiCopilot](https://docs.kentico.com/x/kenticocopilot_guides) documentation.

We recommend installing the plugins and skills from the [KentiCopilot GitHub repository](https://github.com/Kentico/xperience-by-kentico-kenticopilot). The provided skills are optimized to help agents perform development tasks in Xperience by Kentico.
