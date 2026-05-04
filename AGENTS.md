# AGENTS.md

## Project Workflow Rules

Before making any code, config, test, or documentation changes in this project, always read `status.md`.

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
2. Perform the requested work.
3. Run relevant verification.
4. Update `status.md` with:
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

## Indentation Rules

- Use real tab characters for indentation in all repository files instead of groups of four spaces.
- Preserve tabs when editing existing files and correct space-based indentation to tabs when touching files.

## If status.md Does Not Exist

Create it before making further changes, then continue following the normal process.
