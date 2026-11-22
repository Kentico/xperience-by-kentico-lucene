# Lucene contributor guidance

This fully supported Kentico integration **adopted** tooling from
`Kentico/repo-template-xperience`; the repository was not generated from that
template. See `.config/template.json` for pinned sources and intentional differences.
Integration validation is required before treating the migration as complete.

## Compatibility and layout

- SDK: exact .NET 11 SDK in `global.json`; targets: .NET 10.
- Library release: 16.0.0 (dropping .NET 8 requires this major version).
- Library Xperience minimum: **31.0.0** in root `Directory.Packages.props` and admin `@kentico/*` packages.
- DancingGoat Xperience: **31.10.0** in `examples/Directory.Packages.props`; dbmanager and Management MCP follow this version.
- Node.js: supported Node 26 baseline from the client/e2e manifests.
- `src/Kentico.Xperience.Lucene.Core`: indexing, storage, configuration and search.
- `src/Kentico.Xperience.Lucene.Admin`: administration and embedded webpack assets.
- `src/Kentico.Xperience.Lucene`: intentional aggregate package referencing Core and Admin.
- `tests/Kentico.Xperience.Lucene.Tests`: existing NUnit tests; preserve all fixtures and assertions.
- `tests/e2e`: browser coverage; `examples/DancingGoat`: localized faceted search sample.
- `examples/AppHost`: local orchestration; `src/Kentico.Xperience.Lucene.Libs.slnx`: all three packages.

Do not restructure public APIs, package IDs or the dependency graph as template cleanup.
Recommend further breaking changes separately. Do not raise the library Xperience
minimum while updating only the sample.

## Development

Run from the repository root:

```powershell
dotnet tool restore
dotnet restore Kentico.Xperience.Lucene.slnx --locked-mode
dotnet build Kentico.Xperience.Lucene.slnx --configuration Release --no-restore -p:SIGN_FILE=false
dotnet test Kentico.Xperience.Lucene.slnx --configuration Release --no-build --no-restore
dotnet format Kentico.Xperience.Lucene.slnx --no-restore --verify-no-changes
```

Regenerate affected lockfiles with `dotnet restore --force-evaluate` after dependency
changes, then verify locked restore. Never hand-merge locks. Run client/e2e npm
format, lint and typecheck commands when those sources change. Standard Release
packaging embeds admin assets; there is no `LOCAL_NUGET` emulation.
Non-Xperience dependency versions must be at least the adopted template's versions.
Use newer compatible releases when needed to resolve warnings or vulnerabilities,
after the configured Dependabot cooldown/npm minimum-release-age; never bypass the
age policy. Kentico library 31.0.0 and sample 31.10.0 are explicit exceptions.
Keep existing NUnit fixtures, assertions and behavior when updating test packages.

## Local environment

Use the `setup-local` skill. Root `.config/dotnet-tools.json` owns dbmanager.
`scripts/Initialize-Database.ps1` keeps the committed macro salt and restores the
sample's existing CI objects. Never recreate a database without explicit approval.
Use a separately named database for fresh setup verification.

Use existing SQL Server and Mailpit services by default; container-managed services
are an explicit alternative. `aspire run` starts DancingGoat and the admin client.
Keep the site at `http://localhost:14536`, admin client at port **3074**, and module
identity `kentico-xperience-integrations-lucene-admin`. Development uses Proxy mode;
CI uses embedded assets. Preserve the sample's database, user-secrets ID,
Management API identity, committed salt, and CI repository.

## Shared skills and MCP

Root `.claude/skills` is shared by **Copilot and Claude**. No separate per-tool
skill setup is needed. Use `setup-local`, `update-xperience`, `update-template`,
`create-pull-request`, and DancingGoat-scoped `design-conventions`.
The one-time `setup-repo` skill is intentionally absent.

Root `.mcp.json` preserves Kentico Docs and Management MCP configuration and adds
Aspire. Management tools require the running sample; Aspire requires its CLI.
Do not print license files, connection strings, secrets, or user-secrets contents.
Do not replace the Management API secret or site port when updating MCP packages.
Use the app's server Sign in/reconnect controls when needed, not CLI-only commands.

Read `examples/DancingGoat/AGENTS.md` before sample changes. Preserve its entire
search overlay, localized routes/views/assets, facets, service registrations and
CI search objects. `update-xperience/references/dancinggoat-overlay.md` inventories
these differences. Update content through Xperience and store CI changes rather
than hand-editing or re-signing CI objects.

Run `scripts/Test-RepositorySettings.ps1` read-only to report maintainer actions.
Do not apply settings, publish packages, or open a PR without authorization.
