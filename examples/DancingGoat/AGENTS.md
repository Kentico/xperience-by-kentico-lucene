# AGENTS.md

## Project overview

- **Name:** Dancing Goat
- **Stack:** ASP.NET Core MVC, Xperience by Kentico, Tailwind CSS + esbuild frontend pipeline
- **Purpose:** Sample specialty-coffee website and store demonstrating Xperience by Kentico content management, Page Builder, email marketing, and digital commerce.
- **Running app URL:** See `Properties/launchSettings.json`; the administration UI is at `/admin`.

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

## Kentico MCP Servers

Both servers are configured in `.mcp.json`. The Management MCP reads the instance URL and secret from that file; both
values are generated when the project is installed from the template and mirror the `ManagementApi` section of
`appsettings.Development.json`.

- **Kentico Docs MCP** (`kentico-docs-mcp`) is the **primary source** for any question about Xperience by Kentico APIs, configuration, and usage patterns. Prefer it over web search and over prior knowledge.
- **Kentico Management MCP** (`xperience-management-mcp`) is used to work with content in the **running local** instance (content types, content items, pages, Page Builder). Prefer it over manual changes in the admin UI.

## Validation of changes

- Always build the project after making changes.
- After changes under `Styles/`, recompile styles, verify the served CSS updated, and commit the updated compiled CSS.
- Always validate user-facing changes in the browser for content, layout, styling, and localization correctness before committing.
- After changes to views, layouts, or widgets, run an axe-core scan of the affected pages to find any accessibility issues: `npm run a11y -- <url>` (the site must be running; the port is dynamic under Aspire).
