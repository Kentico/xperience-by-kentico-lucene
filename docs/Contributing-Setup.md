# Contributing Setup

This documents the steps maintainers and developers follow to work on the library and sample.

## Required Software

The requirements to set up, develop, and build this project are listed below.

### .NET SDK

Use the exact SDK version in `global.json`. The projects target .NET 10.

- See `global.json` for the pinned SDK version.
- The library's Xperience minimum is 31.6.0; DancingGoat uses 31.10.0.

### Node.js Runtime

- [Node.js](https://nodejs.org/en/download) 26 LTS.
- [NVM for Windows](https://github.com/coreybutler/nvm-windows) to manage multiple installed versions.
- See `engines` in the admin client and e2e `package.json` files for exact requirements.

#### Minimum package age

The admin client and e2e npm folders set `min-release-age=3`. Dependabot uses a three-day default cooldown and seven days for major updates. Apply the configured age policy to dependency updates, including warning and vulnerability fixes; do not bypass it with exclusions. `npm ci` uses the committed lockfile. An `npm install` or `npm update` that resolves a newer release may need to wait until that release is eligible. npm audit advisories are informational, not a validation gate.

### Database

SQL Server 2019 or newer compatible database.

- [SQL Server on Linux](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/setup?view=sql-server-ver15)

### Email

The local sample uses Mailpit to capture email. The existing service listens on `localhost:1025` (SMTP) and its inbox is at <http://localhost:8025>.

## Sample Project

### Database Setup

Use the root [setup-local skill](../.claude/skills/setup-local/SKILL.md) for local setup. It uses the existing SQL Server and Mailpit services, preserves the committed macro salt and CI objects, and does not recreate a database during routine setup. Use a separately named database for fresh verification; see the skill before creating or updating one.

### Continuous Integration repository

`examples/DancingGoat/App_Data/CIRepository` stores the sample's Xperience objects, including the Lucene index, search page, taxonomy, and related content types. Make intended content or configuration changes through Xperience while CI is enabled, then store and review them with:

```powershell
dotnet run --project examples/DancingGoat -- --kxp-ci-store
```

Restore committed objects after pulling changes or when deliberately discarding local administration changes:

```powershell
dotnet run --project examples/DancingGoat -- --kxp-ci-restore
```

Do not hand-edit or re-sign CI objects. Keep the existing index, localized routes, facets, and content behavior intact.

### Run with Aspire

[Aspire](https://aspire.dev) starts DancingGoat and the admin client together. Install the [Aspire CLI](https://aspire.dev/get-started/install-cli/), then run from the repository root:

```powershell
aspire run
```

The site is at <http://localhost:14536>. Development uses Proxy mode for `kentico-xperience-integrations-lucene-admin` on port 3074; CI uses embedded admin assets. Aspire uses the existing local SQL Server and Mailpit services by default. Container-managed services are an explicit alternative.

Without Aspire, start the admin client with `npm start` from `src/Kentico.Xperience.Lucene.Admin/Client`, then run DancingGoat with `dotnet run --project examples/DancingGoat`.

### End-to-end tests

Playwright tests live in `tests/e2e` and require a licensed database. See the [e2e README](../tests/e2e/README.md) for browser setup and commands. Run `npm ci` in the affected client/e2e folder to install dependencies. The sample's `.npmrc` skips install scripts; before its first accessibility scan, run `npm rebuild chromedriver --ignore-scripts=false` in `examples/DancingGoat`.

## Development Workflow

1. Create a branch with a suitable prefix:
   - `feat/` for new functionality
   - `refactor/` for restructuring
   - `fix/` for bug fixes
2. Run `dotnet format --no-restore --verify-no-changes` from the repository root (or use the `dotnet: format` VS Code task).
3. If you change TypeScript, run `npm run format`, `npm run lint`, and `npm run typecheck` in the affected admin client or e2e folder. CI fails on formatting or type/lint errors.
4. Run the build and test commands in the root `AGENTS.md`.
5. Commit changes with a clear message, preferably following [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#summary).
6. Create a pull request with the `create-pull-request` skill and `.github/PULL_REQUEST_TEMPLATE.md`. Include screenshots or video for UI changes and note any maintainer actions.

This repository uses LF line endings for text files. EditorConfig and Git enforce them; no Git line-ending override is needed.

## Merging pull requests

Use **Merge when ready** to add a pull request to the merge queue. The required CI jobs are `build_and_test` and `e2e`; the queue tests each change on top of `main` and earlier queued changes.

## Repository settings

An admin applies these settings. Check them with `./scripts/Test-RepositorySettings.ps1` while signed in with GitHub CLI; the script reports missing settings and does not apply changes.

- `.github/CODEOWNERS` names an owner that GitHub resolves; the owner team has the Admin role, and outside collaborators are added through teams.
- **Settings** > **General** > **Automatically delete head branches**: on.
- Import the `main` branch rulesets from `.github/rulesets`:
  - `protect-main.json` blocks deleting `main` and force-pushing.
  - `merge-queue.json` requires the `Build and Test` and `E2E Tests` checks and the merge queue; repository admins can bypass it.
- **Settings** > **Advanced Security**:
  - Enable the dependency graph and confirm manifests are detected.
  - Enable Dependabot alerts, with an auto-triage rule for low-impact development dependency alerts.
  - Enable Dependabot security updates and grouped security updates.

## Releasing

The package version comes from `VersionPrefix` and `VersionSuffix` in `Directory.Build.props`.

1. Set `VersionPrefix` to the new version. For a pre-release, set `VersionSuffix` to `prerelease-1` and raise the number for each further pre-release of the same version.
2. Merge the version change to `main` through a pull request.
3. Run the manual Azure DevOps pipeline at `.azuredevops/pipelines/build-and-release.yaml`. It builds, signs, tests, and packs the packages, then publishes them to NuGet.org after approval.
4. Test a pre-release in a real project. When it is ready, remove `VersionSuffix`, merge the change, and run the pipeline for the full release.
5. After a successful full release, create the GitHub release and tag `v<version>` on the released commit, with generated release notes. If a version is already on NuGet.org, raise it instead of rerunning the publish.
