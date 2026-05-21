# Status

## General Status
- WPF desktop app scaffold is working with `AgenticColorCreator.App`, `AgenticColorCreator.Core`, and `AgenticColorCreator.Tests`.
- The app can create, load, edit, validate, and save `agentic_colors.md` markdown files.
- The main UI uses `Colors` and `UI Preview` tabs for editor work and themed control previewing.
- Shared theme resources live in `AgenticColorCreator.App\Styles\CFDarkStyles.xaml` and use explicit `CF.` brush keys plus keyed `CF...` styles/templates.
- Custom previewed controls currently include `CFTextBox`, `CFInt`, `CFFloat`, and `CFTreeView`.
- `CFTextBox` now supports delayed external value commits plus immediate validation feedback while typing.
- `CFInt` now uses `CFTextBox` as its delayed-commit text layer and applies actual integer validation through that shared control.
- `CFFloat` now uses `CFTextBox` as its delayed-commit text layer and applies invariant float validation with configurable decimal-place limits.
- `Color\agentic_colors.md` remains the tracked UI color source, with the latest recorded source timestamp still `2026-05-20 13:18:33`.

## Active Issues
- Rebuilding the full debug solution can fail while another process is locking `AgenticColorCreator.Core.dll`.
- The new `CFTextBox`, `CFInt`, and `CFFloat` validation behavior has been build-verified but still needs manual visual confirmation in the running UI.
- `CFTextBox` validation currently uses a direct red foreground override for invalid text, so final visual approval should confirm that this matches the intended theme.
- Descriptions are still saved as a single canonical markdown line even if entered over multiple lines in the UI.
- No drag/drop reordering or search/filtering yet.

## Workarounds
- Close the running app or other locking process before rebuilding the full debug solution.
- Use `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` when the debug solution build is blocked by the locked core DLL.
- Use the `UI Preview` tab to manually confirm delayed commit timing, float decimal-place limits, step-size behavior, arrow-key stepping, and invalid-text red feedback for `CFTextBox`, `CFInt`, and `CFFloat`.
- Use category expanders to manage larger color sets until search/filtering exists.

## Recent Important Changes
- Added reusable delayed-commit `CFTextBox` behavior so the externally bound `Value` commits on Enter, on focus loss, or after 1000ms idle while the control still has focus.
- Extended `CFTextBox` validation with `FloatNumber` mode plus a `DecimalPlaces` property so shared delayed-commit text handling can validate invariant float text with a configurable decimal-place limit.
- Reworked `CFInt` to rely on that single `CFTextBox` delayed-commit layer instead of stacking a separate timer.
- Added `AgenticColorCreator.App\UserControls\CFFloatControl\CFFloat.xaml` and `.xaml.cs` for a float input control that mirrors `CFInt` behavior while using `float` values, configurable decimal precision, delayed commit, clamp-to-range behavior, and spinner step changes.
- Added immediate `CFTextBox` validation modes for alphanumeric path input and actual integer input.
- Added float preview support in `MainWindow` with `PreviewFloatValue`, `PreviewFloatMinimum`, `PreviewFloatMaximum`, and `PreviewFloatDecimals` dependency properties plus a new `CFFloat` preview card and decimals test input in the `UI Preview` tab.
- Updated `CFInt` and `CFFloat` so the keyboard up/down arrow keys now use each control's configured `Step` value, matching the spinner button behavior.
- Added `PreviewIntStep` and `PreviewFloatStep` dependency properties plus new `Step` test inputs on the `CFInt` and `CFFloat` preview cards so step-size behavior can be tested directly from the preview page.
- Updated `CFTextBox` so invalid text turns red while typing, valid text clears the local foreground override back to the normal themed style, and invalid text is not externally committed.
- Updated `CFTextBox` external value application so bound value updates refresh the visible text and validation state without restarting the delayed commit cycle.
- Configured `CFInt` to use `ValidationMode="NumberOnly"` on its internal `CFTextBox`, with number validation now using real invariant integer parsing instead of character-only matching.
- Restored `CFInt` clamped delegated updates so when committed text is outside `Minimum` or `Maximum`, the visible text is updated back to the coerced in-range value.
- Verified the current change with `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` and `dotnet test "AgenticColorCreator.Tests\AgenticColorCreator.Tests.csproj"`.
- Verified formatting for the touched files and normalized `AgenticColorCreator.App\UserControls\CFTextBoxControl\CFTextBox.xaml.cs`, `AgenticColorCreator.App\UserControls\CFFloatControl\CFFloat.xaml`, `AgenticColorCreator.App\UserControls\CFFloatControl\CFFloat.xaml.cs`, `AgenticColorCreator.App\MainWindow.xaml`, and `AgenticColorCreator.App\MainWindow.xaml.cs` back to `CRLF`.

## Notes For Next Contributor
- Read this file before making changes.
- For code, config, or test changes, read `codestandards.md` before editing.
- Keep this file short and practical; remove stale entries instead of appending long history.
- If the task touches UI colors, compare `Color\agentic_colors.md` against the recorded timestamp in `AGENTS.md` before changing mappings.
- Manually verify the current `CFTextBox`, `CFInt`, and `CFFloat` validation behavior in the `UI Preview` tab before treating the UX as final.
