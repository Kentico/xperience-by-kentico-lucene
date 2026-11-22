---
name: create-pull-request
description: Create or update the pull request for the current branch, with a description filled from this repository's PR template using the diff and CI evidence. Use when opening a PR, or writing or refreshing a PR description, checklist, or review summary.
---

Pushes the current branch, creates its pull request if none exists, and fills `.github/PULL_REQUEST_TEMPLATE.md` from verified data: git diff, commits, and status checks. Optimize for reviewer scanability and minimize noise. Each step ends on its completion criterion; move on only when it holds.

## 1. Check the branch

- `gh auth status` succeeds.
- Base branch: the app-selected base (including a stacked parent), or the user's explicit choice. Do not guess the repository default.
- The current branch is not the base branch. If it is, stop and ask the user for a branch name.
- `git status --short` is empty. If not, stop and ask whether to commit the changes first; never commit without the user's approval.
- `git log --oneline <base>..HEAD` lists at least one commit.

Done when all hold. Otherwise report the blocker and stop.

## 2. Push

If the branch has no upstream, run `git push -u origin HEAD`; if it is ahead of its upstream, run `git push`. Never force-push. Done when `git status -sb` shows the branch even with its upstream.

## 3. Find the pull request

Run `gh pr view --json number,url,state,title,body`. Done when the result is known:

- open PR: update it in step 6
- no PR, or only a closed/merged one: create a new one in step 6

## 4. Gather evidence

Source of truth, in order:

1. GitHub status checks (`gh pr checks`) for CI result, when a PR exists. A new PR has no checks yet; do not wait for them.
2. `git diff --name-only <base>...HEAD` and `git diff <base>...HEAD` for changes.
3. `git log <base>..HEAD` for intent.
4. `.github/PULL_REQUEST_TEMPLATE.md` headings (authoritative section schema).
5. Existing PR title/body and linked issues.

Do not infer test pass/fail from local assumptions. Done when every area heading under `Changes by Area` in the template is matched to its changed files, or found unchanged.

## 5. Draft

Title: one line, under 72 characters, concrete verb first. Keep an existing PR's title unless it no longer fits the changes.

Body: the template with each section filled per the rules below, the leading `<!-- -->` guidance comment removed. Done when the draft passes every rule; revise until it does.

### Population rules

1. Keep bullets single-line.
2. Use concrete verbs first (Added, Updated, Removed, Migrated, Fixed).
3. Prefer grouped summaries over file dumps.
4. Remove unpopulated optional sections/subsections entirely.
5. Write `None` only for required fields that must remain present.
6. Cap sections to high-signal bullets:
   - `TL;DR`: max 3 bullets
   - `Changes by Area`: max 6 bullets per area
7. Exclude noisy generated paths unless material to review:
   - `**/dist/**`, `**/wwwroot/**`, `**/assets/**` (compiled/bundled client assets)
   - `**/packages.lock.json`, `**/package-lock.json`
   - Generated build artifacts or dumps
8. If excluded generated content materially affects behavior, summarize impact in the relevant area without listing individual files.
9. Do not claim manual testing unless explicit manual steps/evidence exist.
10. Do not add placeholders like "fixes #issue number" when the issue is unknown; use `None`.
11. Keep wording neutral and review-focused.
12. Discover `Changes by Area` headings from the template at runtime; do not hardcode area names.
13. Do not include "Tests are passing" as a checkbox; reflect test state only in `Validation/Automated`.

### Section mapping

- `Summary`: 1 short paragraph with why + impact.
- `TL;DR`: top change, top risk, first review focus.
- `Status`: risk, breaking change, manual test need, data impact.
- `Linked Work`: only verified issue references, else `None`.
- `Review Map`: 3 ordered review entry points by area.
- `Changes by Area`: only template-defined headings for changed areas; remove empty ones.
- `Downstream` (template repository only): who should adopt the change and what they must do after; `Not needed (template-only)` for changes to template-only files such as the `setup-repo` skill.
- `Validation/Automated`: default to "See PR checks"; name checks only when non-obvious or already failing.
- `Validation/Manual`: include only when manual testing is required.
- `Risk and Rollback`: one concise line each.
- `Out of Scope`: include only when there are explicit non-goals.
- `Reviewer Notes`: include only when there are tradeoffs/open questions.

## 6. Create or update

Use the app's native pull request tools by default. Use CLI creation only when
explicitly requested or when the native tool's failure directs it.

- New: `create_pull_request` with title/body; draft only when requested.
- Existing: `update_pull_request` with title/body and its concurrent-edit guard;
  do not use GraphQL-based `gh pr edit`, and never edit a closed or merged PR.

Done when `gh pr view --json body` returns the draft with line breaks intact.

## 7. Report

1. PR number and URL, and whether it was created or updated.
2. Validation/Automated summary used.
3. Any assumptions or unknowns.
4. A short note if generated content was intentionally summarized.
