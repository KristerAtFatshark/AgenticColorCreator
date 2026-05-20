# Status

## General Status
- WPF desktop app scaffold is working with `AgenticColorCreator.App`, `AgenticColorCreator.Core`, and `AgenticColorCreator.Tests`.
- The app can create, load, edit, validate, and save `agentic_colors.md` markdown files.
- The main UI uses `Colors` and `UI Preview` tabs for editor work and themed control previewing.
- Shared theme resources live in `AgenticColorCreator.App\Styles\CFDarkStyles.xaml` and use explicit `CF.` brush keys plus keyed `CF...` styles/templates.
- Custom previewed controls currently include `CFTextBox`, `CFInt`, and `CFTreeView`.
- `CFTextBox` now supports delayed external value commits plus immediate validation feedback while typing.
- `CFInt` now uses `CFTextBox` as its delayed-commit text layer and applies actual integer validation through that shared control.
- `Color\agentic_colors.md` remains the tracked UI color source, with the latest recorded source timestamp still `2026-05-20 13:18:33`.

## Active Issues
- Rebuilding the full debug solution can fail while another process is locking `AgenticColorCreator.Core.dll`.
- The new `CFTextBox` and `CFInt` validation behavior has been build-verified but still needs manual visual confirmation in the running UI.
- `CFTextBox` validation currently uses a direct red foreground override for invalid text, so final visual approval should confirm that this matches the intended theme.
- Descriptions are still saved as a single canonical markdown line even if entered over multiple lines in the UI.
- No drag/drop reordering or search/filtering yet.

## Workarounds
- Close the running app or other locking process before rebuilding the full debug solution.
- Use `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` when the debug solution build is blocked by the locked core DLL.
- Use the `UI Preview` tab to manually confirm delayed commit timing and invalid-text red feedback for `CFTextBox` and `CFInt`.
- Use category expanders to manage larger color sets until search/filtering exists.

## Recent Important Changes
- Added reusable delayed-commit `CFTextBox` behavior so the externally bound `Value` commits on Enter, on focus loss, or after 1000ms idle while the control still has focus.
- Reworked `CFInt` to rely on that single `CFTextBox` delayed-commit layer instead of stacking a separate timer.
- Added immediate `CFTextBox` validation modes for alphanumeric path input and actual integer input.
- Updated `CFTextBox` so invalid text turns red while typing, valid text clears the local foreground override back to the normal themed style, and invalid text is not externally committed.
- Updated `CFTextBox` external value application so bound value updates refresh the visible text and validation state without restarting the delayed commit cycle.
- Configured `CFInt` to use `ValidationMode="NumberOnly"` on its internal `CFTextBox`, with number validation now using real invariant integer parsing instead of character-only matching.
- Restored `CFInt` clamped delegated updates so when committed text is outside `Minimum` or `Maximum`, the visible text is updated back to the coerced in-range value.
- Verified the current change with `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` and `dotnet test "AgenticColorCreator.Tests\AgenticColorCreator.Tests.csproj"`.
- Verified formatting for the touched files and normalized `AgenticColorCreator.App\UserControls\CFTextBoxControl\CFTextBox.xaml.cs` and `AgenticColorCreator.App\UserControls\CFIntControl\CFInt.xaml` back to `CRLF`.

## Notes For Next Contributor
- Read this file before making changes.
- For code, config, or test changes, read `codestandards.md` before editing.
- Keep this file short and practical; remove stale entries instead of appending long history.
- If the task touches UI colors, compare `Color\agentic_colors.md` against the recorded timestamp in `AGENTS.md` before changing mappings.
- Manually verify the current `CFTextBox` and `CFInt` validation behavior in the `UI Preview` tab before treating the UX as final.
