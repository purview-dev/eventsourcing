---
name: changesets-prerelease
description: Create and apply a prerelease bump using Changesets CLI, with the new changeset markdown summarizing the actual changes since the last shipped release.
---

# Changesets Prerelease Skill

Use this skill when preparing the next prerelease version in this repository.

## Steps

1. Add a new changeset:
   - `npx @changesets/cli add --empty --message "<summary>"`
   - update the generated `.changeset/*.md` frontmatter with:
     - `"purview-eventsourcing": patch`
   - replace the placeholder body with a concise summary of the **actual user-facing changes since the last shipped release**, not just a generic "prepare prerelease" note.
   - if the immediately previous prerelease number (for example `.24`) has **not** been released yet and you are preparing `.25`, the new markdown should still describe the cumulative changes that matter for the next published prerelease.
2. Bump versions/changelog:
   - `npx @changesets/cli version`
3. Commit the resulting changes (`package.json`, `CHANGELOG.md`, and consumed `.changeset` files).

## Writing the new `.changeset/*.md` body

- Summarize the functional changes that should appear in the changelog for the next published prerelease.
- Prefer short release-note language such as:
  - fixed SQL snapshot translation for directly mapped complex mirror properties
  - clarified provider documentation for scalar value object query behavior
  - aligned repo-local agent skills and instructions
- Do **not** leave the body as a procedural placeholder such as `Prepare next prerelease` unless there were truly no meaningful changes.
- If multiple unreleased prerelease bumps exist locally, write the new file so the generated changelog entry remains useful to an external consumer reading the next shipped release.

## Notes

- Repository currently runs prerelease mode with `tag: prerelease` (`.changeset/pre.json`).
- Releasing is done from `package.json` version in the `release.yml` workflow.
- In prerelease mode, `changeset version` can record a changeset in `.changeset/pre.json` even if the new file was created with placeholder/empty content first. If that happens, correct the `.md` frontmatter/body before finalizing the release notes so the next shipped prerelease entry is accurate.
