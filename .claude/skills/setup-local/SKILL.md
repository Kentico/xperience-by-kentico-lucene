---
name: setup-local
description: Set up Lucene's local DancingGoat environment with existing SQL Server and Mailpit, root dbmanager, shared skills, and optional Aspire orchestration. Use after cloning or when creating a separately named local database.
---

Run from the repository root. Root `.claude/skills` works for both Copilot and
Claude; do not install duplicate per-tool skills or run the one-time setup-repo.

## 1. Prerequisites

- Exact .NET 11 SDK in `global.json`, .NET 10 runtime/targets.
- Supported Node 26 from the admin client/e2e `engines` declarations.
- Existing SQL Server and Mailpit (SMTP localhost:1025, UI localhost:8025) by default.
- Aspire CLI for orchestration, optional for direct site/client startup.

Check service availability before starting anything. Do not start, replace or stop
shared services without permission. Container-managed services are an explicit
alternative; use AppHost's documented opt-in, not a second server on occupied ports.

## 2. License

Ask for the license file path or use `XPERIENCE_LICENSE_FILE`. Pass the path only:
never read, print or copy the contents. An explicitly supplied missing path is an
error. If the user chooses no license, explain that licensed runtime/e2e checks
cannot pass until one is installed.

## 3. Dependencies and build

```powershell
dotnet tool restore
dotnet restore Kentico.Xperience.Lucene.slnx --locked-mode
dotnet build Kentico.Xperience.Lucene.slnx --configuration Release --no-restore -p:SIGN_FILE=false
```

Use `npm ci` in `src/Kentico.Xperience.Lucene.Admin/Client` and `tests/e2e`,
then `npm run install:browsers` in `tests/e2e` when browser checks are required.
Release builds embed admin assets; there is no LOCAL_NUGET emulation.
dbmanager comes from root `.config/dotnet-tools.json` and must match sample
Xperience **31.10.0**; library minimum **31.0.0** is intentionally separate.

## 4. New database

Use a separately named database for verification. Check whether the target exists
before creation. Never use `-Recreate` without explicit approval to drop that exact
database. Never rotate the committed macro salt, sample identities or user-secrets ID.

```powershell
.\scripts\Initialize-Database.ps1 -DatabaseName <new-local-database> -LicenseFile <path> -Configuration Release
```

Optional `-ServerName <server>` and `-Username ""` select Windows authentication.
Default `-SaveTo UserSecrets` saves the connection into the existing sample's
user-secrets store; disclose that it switches that sample's local database.
`-SaveTo None` makes no persistent connection change; supply the matching
`ConnectionStrings__CMSConnectionString` when running the sample afterward.
CI explicitly uses `-SaveTo AppSettings` in an isolated CI checkout.
`-IfNotReady` may **update** an existing database; use only with approval for that
database update.
After Ready status or a successful update, UserSecrets/Salt modes remove a stale
local macro salt override so ordinary startup uses the committed CI salt.
None mode never accesses user secrets.

The script seeds stock data, signs with the existing committed salt, restores all
Lucene CI objects and enables CI. It restores committed appsettings on failure or
non-AppSettings saves. A conflicting salt is rejected. Ordinary setup must leave
`appsettings.json` and `App_Data/CIRepository` unchanged.

## 5. Start and verify

`aspire run` starts the site and client using existing services by default.
Without Aspire: `npm start` in `src/Kentico.Xperience.Lucene.Admin/Client`,
then `dotnet run --project examples/DancingGoat`.
Development Proxy uses `kentico-xperience-integrations-lucene-admin` on port
**3074**; CI uses embedded assets. Site: **http://localhost:14536**.
Mailpit receives local system/channel emails; production settings are unchanged.

Verify DB `Ready`, CI enabled, and health/search:

```powershell
dotnet run --project examples/DancingGoat --no-build -- --kxp-db-status --format json
dotnet run --project examples/DancingGoat --no-build -- --kxp-ci-status --format json
git status --short examples/DancingGoat/App_Data examples/DancingGoat/appsettings.json
```

Wait for `/admin`, `/search` and `/es/buscar`; the empty Lucene index rebuilds on
startup. Check results/facets and localized links, not just HTTP success.
Run licensed e2e as documented in `tests/e2e/README.md`. Report skips explicitly.

Management MCP is pinned to the sample version in root `.mcp.json` and requires
the running site; preserve its secret and port. In the Copilot app use the server's
inline reconnect/Sign in action or its settings row, never CLI-only `/mcp auth`.
Aspire MCP requires its CLI. Do not run `aspire agent init` or alter global hooks
unless explicitly requested.

## 6. Report

Report site/admin/client URLs, database selection, license state, CI state and
actual runtime checks. Demo administrator credentials are documented in the sample
README; never repeat production credentials or user-secrets contents.
Commit only intended CI content changes. Never restore someone else's uncommitted
changes as cleanup.
