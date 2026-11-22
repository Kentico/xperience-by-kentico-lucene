---
name: update-template
description: Check newer releases of the Kentico repository template whose tooling Lucene adopted, preserving its three-package layout, supported Xperience versions, sample search and module identity. Use when updating or checking template tooling.
---

Brings applicable changes from newer releases of `Kentico/repo-template-xperience` into this repository. `.config/template.json` records the pinned template source, reviewed release, setup choices, and changes skipped on purpose. Decide per change whether it fits this project; each step ends on its completion criterion.

The manifest pins the reviewed template release/source and records adaptation
provenance separately from integration validation; preserve these fields.
The compare renderer narrowly maps the template's base implementation to `.Core`,
retains the aggregate sample reference, includes all three solution/package entries,
and uses `kentico-xperience-integrations-lucene-admin`. Preserve these adaptations
and the existing NUnit project filename/assembly `Kentico.Xperience.Lucene.Core.Tests`
when updating the skill itself; run `scripts/Test-TemplateRendering.ps1`.

## 1. Check the starting state

- `git status --short` is empty. If not, stop and ask the user how to proceed.
- `gh auth status` succeeds.
- Work on the app-managed feature branch; do not switch or rename branches with raw Git commands.

Done when all hold.

## 2. Read the manifest

Read `.config/template.json`:

- `template`: the template repository
- `commit`, `version`: the pinned source commit and template release last reviewed
- `setup`: the `setup-repo` choices (family, name, display name, example, admin, GitHub owner, repository slug, author)
- `skipped`: changes not adopted, each with its release and reason

If the file is missing, record the reviewed template source and setup choices after inspecting the current package layout and repository remote. Use the complete adoption inventory to identify applicable files. Done when the manifest identifies the reviewed source and local setup.

## 3. Fetch the template

Clone it outside the repository, without file contents until needed:

```powershell
$tmpl = Join-Path ([IO.Path]::GetTempPath()) "template-$(Get-Random)"
git clone --bare --filter=blob:none https://github.com/Kentico/repo-template-xperience.git $tmpl
```

Run every later template command as `git -C $tmpl ...`. Done when `git -C $tmpl tag` runs.

## 4. Find the baseline

Use the manifest's pinned commit as the comparison baseline:

1. Verify `commit` exists in the template and corresponds to the recorded release/source.
2. If no commit is recorded, use the adoption inventory and inspect the candidate release's full relevant file set. `Find-TemplateBaseline.ps1` may rank candidates for review only; similarity is not proof of a historical baseline. Record the chosen reviewed source commit and uncertainty in `provenance`.

Done when the comparison source is verified and recorded.

## 5. List newer releases

Newer releases are the template's `v*` tags whose commit is not an ancestor of the baseline (`git -C $tmpl merge-base --is-ancestor <tag> <baseline>` fails), oldest first. Read each one's notes with `gh release view <tag> --repo Kentico/repo-template-xperience`.

If there are none, report that no newer release is available. Done when the list and notes are read.

## 6. Update this skill first

If `git -C $tmpl diff --stat <baseline> <latest tag> -- .claude/skills/update-template` lists changes, export it outside the repository and merge it into the local skill. Preserve Lucene's renderer adaptations, tests, and adopted-tooling guidance; never replace this skill wholesale:

```powershell
$zip = Join-Path $tmpl 'skill.zip'
git -C $tmpl archive --output $zip <latest tag> .claude/skills/update-template
Expand-Archive $zip -DestinationPath <outside-repository-review-folder>
```

Done when relevant changes are incorporated and Lucene-specific adaptations remain intact.

## 7. Review each release

For each release, oldest first (`<from>` is the previous release, or the baseline for the first):

1. List its changes: `git -C $tmpl log --first-parent --format="%h %s" <from>..<tag>`. Release notes list only pull requests; commits pushed straight to `main` (before v1.0.0) appear only here.
2. For each pull request, read `gh pr view <number> --repo Kentico/repo-template-xperience --json title,body,files`. Its **Downstream** section says who should adopt it, why, and the follow-up steps. Find a commit's pull request with `gh api repos/Kentico/repo-template-xperience/commits/<sha>/pulls --jq ".[].number"`.
3. See what each changed file means here. The script renders the template's files with this repository's names, in a folder outside the repository, and merges each change into this repository's copy without changing it:
   `./.claude/skills/update-template/scripts/Compare-TemplateFiles.ps1 -Template $tmpl -Base <from> -Target <tag>`
   It lists a status per file (the script's header explains each) and writes `<path>.base`, `.target`, and `.merged` to its output folder. Read the rendered change with `git diff --no-index <out>/<path>.base <out>/<path>.target`.
4. Decide per change, and note the reason:
   - **adopt**: it fits as is
   - **adapt**: it fits, but this repository's names or local changes need a different form
   - **skip**: not relevant, or already done here

Skip unless the release notes or the Downstream section say otherwise:

- template-only files: `.github/workflows/template-smoke.yml`, the `setup-repo` skill, the `---Template info---` part of `README.md`, `template:upstream` blocks, the template repository's own `.config/template.json` values
- Kentico package versions: preserve library minimum 31.0.0 and sample 31.10.0 unless explicitly approved; their version separation is intentional
- `examples/DancingGoat` changes from an Xperience update: the `update-xperience` skill owns them
- naming convention changes and folder moves
- changes for parts this repository lacks: `.Admin` project when `setup.noAdmin`, `.azuredevops/` for Community or examples, `publish.yml` for Kentico, Labs, or examples, packaging for examples
- items in `skipped`, unless the project changed so that the reason no longer holds

Weigh the rest against this project: what it builds, what it changed on purpose (see its git history for the files), and whether the change fixes a problem it has. Recommended changes, security fixes, and SDK or Node.js minimum raises are adopted unless something here conflicts.

Non-Xperience dependencies, including test libraries, helper tools and npm devdeps,
must not be older than the adopted template's versions. Align them to compatible
template versions, or newer eligible versions needed to resolve warnings or
vulnerabilities. Respect the configured cooldown/minimum-release-age. Regenerate
locks with the package manager; preserve existing tests, APIs and package graph.
If compatibility or age eligibility prevents alignment, report the blocker rather
than silently retaining an older dependency or bypassing the age policy.

Done when every change in every newer release has a decision and a reason.

## 8. Apply

Apply the adopt and adapt decisions, release by release:

- Files adopted whole with status `new` or `merged`: rerun the compare script with `-Apply -Paths <those files>`. It writes only those files, with this repository's names and its local changes (ports, secrets) kept.
- Status `conflict`, or a file adopted only in part: edit the repository's file by hand, using `<out>/<path>.merged` (conflict markers show local and template text) or the rendered diff. Keep this repository's own content (project-specific docs text, `references/dancinggoat-overlay.md`, customized skills) and bring in only the template's change around it.
- Status `missing` (this repository removed the file) or `deleted` (the template removed it): decide by hand.
- Write files with LF line endings and no added byte order mark. PowerShell's `Set-Content` writes CRLF on Windows; `-Apply` and the Edit tool do not.

If the compare script fails or its naming is wrong for this repository, the ordered token list is in `.claude/skills/setup-repo/scripts/Rename-Template.ps1` at the tag (`git -C $tmpl show <tag>:<path>`), with values from `setup`; template-marked blocks (`template:<tag>:start` / `end`) are kept or dropped by the same choices as in `setup-repo`.

Then do each adopted change's follow-up steps, and the checks in `AGENTS.md` ("Development") for what changed: `dotnet restore --force-evaluate` after `global.json` or project changes, `dotnet format`, npm format, lint, and typecheck, build and test, screenshots when screenshot tooling or captured UI changed.

Done when every adopted change is applied and the checks pass. On a failure you can't fix, revert that change, record it as skipped with the reason, and tell the user.

## 9. Update the manifest

In `.config/template.json`:

- `version`: the latest release reviewed
- `commit`: that release's commit (`git -C $tmpl rev-list -n 1 <tag>`)
- `skipped`: add each skip whose reason is a choice for this project (not "template-only" or "package version"), as `{ "release", "change", "reason" }`; remove entries adopted now

Delete the template clone. Done when the manifest is updated and the clone is gone.

## 10. Report

List, per release: each change, the decision, and the reason. Then the steps that need a repository admin or maintainer (repository settings, imported rulesets, secrets, `./scripts/Test-RepositorySettings.ps1`).

Leave the changes uncommitted; commit only when the user approves, then open the pull request with the `create-pull-request` skill. Done when the report is given.
