# AGENTS.md

## Project Workflow Rules

Before making any code, config, test, or documentation changes in this project, always read `status.md`.

Before making any code, config, or test changes in this project, always read `codestandards.md` and follow its rules.

`codestandards.md` does not apply to `*.md` documentation files unless a rule in that file explicitly says it does.
This includes generic formatting rules defined there, such as indentation and line-ending requirements.

Before making any UI color changes, always check the timestamp of `Color/agentic_colors.md` and compare it to the latest color file version recorded in this file.

After completing any change, always update `status.md`.

## status.md Requirements

`status.md` must be used as the short operational memory for this repository.

It should include:
- Current general project status
- Active issues
- Known workarounds
- Recent important changes
- Anything another agent or contributor should know before making further changes

Keep `status.md` short and practical.
Target length: below 150 lines.

## Required Process

1. Read `status.md` before making changes.
2. If the change touches code, config, or tests, read `codestandards.md` before making changes and follow all rules defined there.
3. If the task touches UI colors, check `Color/agentic_colors.md` timestamp and compare it to the latest recorded color-file timestamp in this file.
4. If `Color/agentic_colors.md` is newer than the last recorded UI color update source, reevaluate all current UI color mappings and update them where needed.
5. Perform the requested work.
6. Verify formatting for every touched file:
   - `CRLF` line endings
   - tab indentation for `*.cs`, `*.csproj`, and `*.xaml`
   - include newly created files in this check, not only edited files
7. Run relevant verification.
8. Update `status.md` with:
   - what changed
   - current issues
   - current workarounds
   - current overall status

## Testing Expectations

- Write tests for every new feature.
- Write tests for bug fixes.
- Update tests when existing behavior is intentionally changed.
- If a bug is fixed, add a regression test for that bug when practical.
- Always run tests after every build and reflect on any failures before considering the change complete.

## Writing Style For status.md

- Keep entries concise
- Prefer bullet points
- Remove outdated information
- Avoid long historical logs
- Preserve only information useful for the next contributor

## UI Color Tracking

- Treat `Color/agentic_colors.md` as the source of truth for UI colors.
- Always record the latest `Color/agentic_colors.md` file path and timestamp in this file after a UI color update pass.
- If the file timestamp is newer than the recorded timestamp, all existing UI colors must be reevaluated against the current color definitions and `colors.md` guidance.
- That reevaluation must include both changed color values and newly added color entries, since new entries may provide a better semantic match than the prior mapping.
- That reevaluation must also include newly added top-level categories, since a new category may replace an older broad category that was previously used as the closest available match.
- That reevaluation must also include removed categories, since a deleted category means any existing mapping to it must be replaced with the most specific remaining category.

Latest recorded UI color source:
- File: `Color/agentic_colors.md`
- LastWriteTime: `2026-06-10 12:36:14`

## If status.md Does Not Exist

Create it before making further changes, then continue following the normal process.
