---
name: update-xperience
description: Update Xperience by Kentico to a newer version - regenerate the DancingGoat example from the new project template, keeping this repository's changes, bump its Kentico packages, and optionally raise the libraries' minimum Xperience version (Kentico NuGet and npm packages). Use when upgrading or refreshing Xperience, Kentico packages, or DancingGoat.
---

`scripts/Update-Xperience.ps1` does the mechanical work: it generates pristine DancingGoat projects for the current and target versions and three-way merges them with `examples/DancingGoat`, so template changes come in and this repository's changes stay. It also updates the Kentico versions DancingGoat uses (`examples/Directory.Packages.props`) and `.config/dotnet-tools.json`.

Two versions, kept apart on purpose:

- **DancingGoat version** – `examples/Directory.Packages.props`. Follows the latest Xperience, so the sample checks the libraries against it.
- **Library minimum** – root `Directory.Packages.props` and admin `Client/package.json` `@kentico/*`. The oldest Xperience the libraries in `src/` support. NUnit tests use their own package overrides; keep all existing tests. Raised only with `-Libraries`, a deliberate choice (it drops support for older versions).

The DancingGoat version is never below the library minimum. Each step ends on its completion criterion.

## 1. Check the starting state

Run from the repository root. Done when `git status --short` is empty. If there are local changes, stop and ask the user how to proceed.

The local DancingGoat database (the `setup-local` skill) must be on the current version: `dotnet build examples/DancingGoat`, then `dotnet run --project examples/DancingGoat --no-build -- --kxp-db-status --format json` returns `state` `Ready`.

## 2. Pick the version

Run `./.claude/skills/update-xperience/scripts/Update-Xperience.ps1 [-Version <x.y.z>] [-Libraries] -Preview`. Without `-Version` the target is the latest stable `Kentico.Xperience.Templates`. Ask the user whether to also raise the library minimum (`-Libraries`); default is no. Done when the user accepts both current → target versions. Read the Xperience changelog for breaking changes between them (<https://docs.kentico.com/changelog>) and tell the user which apply.

## 3. Run the update

First turn off Xperience Continuous Integration: `dotnet run --project examples/DancingGoat --no-build -- --kxp-ci-disable --format json`. It must run before the packages change, because the Xperience CLI refuses to start once the package and database versions differ. Done when it returns `success` `true`.

Run the same command without `-Preview`. Done when it exits 0, or exits 2 and you have resolved every conflicted file it lists: no `<<<<<<<`, `=======`, or `>>>>>>>` lines remain in `examples/DancingGoat`. Resolve conflicts with [references/dancinggoat-overlay.md](references/dancinggoat-overlay.md). The pristine projects and merge repository stay in the work folder the script prints; `git -C <work>/merge diff base theirs` shows exactly what the template changed.

The target template already includes its DancingGoat version migrations. Do not separately port changelog migrations into `examples/DancingGoat`; only intervene for unresolved merge conflicts or repository-specific overlay code that remains incompatible with the target template.

## 4. Compare root files the template also ships

Skip when DancingGoat was not updated.

The script prints two paths from the target template:

- `.mcp.json`: add any MCP server it has that the root `.mcp.json` lacks, keeping the root's port and secret.
- DancingGoat skills: for each skill folder, bring its content into the root `.claude/skills/<name>/`, keeping the root's `description` and the line saying paths are relative to `examples/DancingGoat/`. A skill with no root copy is added the same way.

Pin `@kentico/management-api-mcp` in the root `.mcp.json` to the target Xperience version; do not use `latest`. Keep the root's port and secret.

Done when every MCP server in the target file exists in the root file, the Management API MCP package version matches the target Xperience version, and every target skill body matches its root copy apart from those two lines.

## 5. Update lock files

- With `-Libraries`, in each `src/*/Client` folder: `npm install --no-audit --no-fund`
- `dotnet tool restore`
- `dotnet restore --force-evaluate`, then `dotnet restore --locked-mode`

Done when the locked restore exits 0.

Then review the `Transitive security pins` group in `Directory.Packages.props`: for each pin, remove it, run `dotnet restore --force-evaluate` and `dotnet nuget why <root .slnx> <package>`, and keep the removal when the resolved version is at least the pinned one and restore reports no NU190x vulnerability warning; otherwise restore the pin. Done when every remaining pin is still needed.

## 6. Fix breaking changes

Build with `dotnet build --configuration Release --no-restore`. Fix every error in `src/` and `tests/` caused by the new version, guided by the changelog from step 2. Without `-Libraries`, `src/` still builds against the library minimum; an error or test failure in DancingGoat or `tests/e2e` that comes from the library means it does not work on the new Xperience version: stop and ask the user whether to fix the library (keeping the minimum) or raise the minimum. Done when the build has no errors.

## 7. Update the database and the CI repository

`examples/DancingGoat/App_Data/CIRepository` is DancingGoat's committed object data; a new `Data/Template.zip` only seeds new databases. Update the existing local database and store the result, so the repository moves to the target version:

1. `dotnet run --project examples/DancingGoat --configuration Release --no-build -- --kxp-update --skip-confirmation`
2. `dotnet run --project examples/DancingGoat --configuration Release --no-build -- --kxp-ci-enable --format json`
3. `dotnet run --project examples/DancingGoat --configuration Release --no-build -- --kxp-ci-store`

Review `git status --short examples/DancingGoat/App_Data/CIRepository`: changes come from the update's data migrations; anything else (local content edits) does not belong in this change. Done when all three exit 0 and every repository change is from the update.

## 8. Validate

1. `dotnet test --configuration Release --no-build`
2. `dotnet format --no-restore --verify-no-changes`
3. In each `src/*/Client`: `npm run typecheck`, `npm run lint`, and `npm run format:check`

Done when all three pass.

## 9. Update docs

With `-Libraries`, set the minimum Xperience version in the `README.md` version matrix to the target version so it matches the root `Directory.Packages.props`. Without it, leave the matrix. Done when every Xperience version mentioned in `README.md` and `docs/` is intentional.

## 10. Report

List: current → target DancingGoat version and library minimum, packages and tools changed, conflicts and how each was resolved, breaking changes fixed, validation results, CI repository changes, and the next step for other developers: update an existing local database with `dotnet run --project examples/DancingGoat -- --kxp-update`, then `--kxp-ci-restore` (see <https://docs.kentico.com/documentation/developers-and-admins/installation/update-xperience-by-kentico-projects>), or create a new one with the `setup-local` skill.
