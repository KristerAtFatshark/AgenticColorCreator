# Status

## General Status
- WPF desktop app scaffold is working with `AgenticColorCreator.App`, `AgenticColorCreator.Core`, and `AgenticColorCreator.Tests`.
- The app can create, load, edit, validate, and save `agentic_colors.md` markdown files.
- The main UI uses `Colors` and `UI Preview` tabs for editor work and themed control previewing.
- Shared theme resources live in `AgenticColorCreator.App\Styles\CFDarkStyles.xaml` and use explicit `CF.` brush keys plus keyed `CF...` styles/templates.
- Custom previewed controls currently include `CFTextBox`, `CFInt`, `CFFloat`, `CFColor`, and `CFTreeView`.
- `CFTextBox` now supports delayed external value commits plus immediate validation feedback while typing.
- `CFInt` now uses `CFTextBox` as its delayed-commit text layer and applies actual integer validation through that shared control.
- `CFFloat` now uses `CFTextBox` as its delayed-commit text layer and applies invariant float validation with configurable decimal-place limits.
- `CFColor` now provides a reusable color-well control with hex display, transparent checker preview, mouse-over border feedback, and picker-dialog integration.
- `Color\agentic_colors.md` remains the tracked UI color source, with the latest recorded source timestamp now `2026-05-21 10:22:00`.

## Active Issues
- Rebuilding the full debug solution can fail while another process is locking `AgenticColorCreator.Core.dll`.
- The new `CFTextBox`, `CFInt`, `CFFloat`, `CFColor`, and upgraded color-picker behavior have been build-verified but still need manual visual confirmation in the running UI.
- `CFTextBox` validation currently uses a direct red foreground override for invalid text, so final visual approval should confirm that this matches the intended theme.
- Descriptions are still saved as a single canonical markdown line even if entered over multiple lines in the UI.
- No drag/drop reordering or search/filtering yet.

## Workarounds
- Close the running app or other locking process before rebuilding the full debug solution.
- Use `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` when the debug solution build is blocked by the locked core DLL.
- Use the `UI Preview` tab to manually confirm delayed commit timing, float decimal-place limits, step-size behavior, arrow-key stepping, `CFColor` transparency preview, and invalid-text red feedback for `CFTextBox`, `CFInt`, `CFFloat`, and `CFColor`.
- Use category expanders to manage larger color sets until search/filtering exists.

## Recent Important Changes
- Added reusable delayed-commit `CFTextBox` behavior so the externally bound `Value` commits on Enter, on focus loss, or after 1000ms idle while the control still has focus.
- Added `AgenticColorCreator.Core\Services\ColorSpaceConverter.cs` plus `ColorSpaceConverterTests.cs` so HSV/RGB color conversion used by the picker is covered by unit tests.
- Extended `CFTextBox` validation with `FloatNumber` mode plus a `DecimalPlaces` property so shared delayed-commit text handling can validate invariant float text with a configurable decimal-place limit.
- Reworked `CFInt` to rely on that single `CFTextBox` delayed-commit layer instead of stacking a separate timer.
- Added `AgenticColorCreator.App\UserControls\CFFloatControl\CFFloat.xaml` and `.xaml.cs` for a float input control that mirrors `CFInt` behavior while using `float` values, configurable decimal precision, delayed commit, clamp-to-range behavior, and spinner step changes.
- Added `AgenticColorCreator.App\UserControls\CFColorControl\CFColor.xaml` and `.xaml.cs` for a reusable color-well control that shows a transparent checker under semi-transparent colors, displays the current hex value, and opens the color picker dialog on click.
- Replaced the old slider-only color picker UI in `ColorPickerWindow.xaml` and `ColorPickerViewModel.cs` with an RGB/HSV/hex-capable interactive picker that includes a hue ring plus saturation/value square for direct HSV selection.
- Updated the interactive color picker so the saturation/value square now fits fully inside the hue ring and replaced the previous blocky segmented hue ring with a smooth rendered hue wheel.
- Reduced the interactive color picker's inner saturation/value square again so it leaves a clearer visible gap from the hue ring.
- Reduced the interactive color picker's inner saturation/value square to `128x128`, which geometrically guarantees at least a 5px gap from the inner ring border with the current wheel dimensions.
- Corrected the hue-ring selector marker positioning so the marker is centered on its computed hue point instead of being offset outside the ring.
- Updated the interactive color picker's hue ring to be exactly `20px` thick and kept the hue selector marker centered at the midpoint of that ring thickness.
- Corrected the saturation/value selector marker positioning so the white square is centered on the chosen point instead of using that point as its top-left corner.
- Fixed the color picker so RGB and alpha slider/text changes now refresh the visual picker surfaces and selector markers immediately instead of only updating the numeric/hex values.
- Replaced the interactive color picker's selected-color preview placeholder blocks with a real tiled checkerboard background so transparent colors preview consistently with `CFColor`.
- Removed the selected-color preview overlay inset so the color fill now exactly matches the checkerboard preview area instead of leaving a transparent 1px border.
- Replaced the interactive color picker's left-side negative margin hack with equivalent parent/right-column spacing so the interactive section keeps the same position without relying on a brittle negative offset.
- Added immediate `CFTextBox` validation modes for alphanumeric path input and actual integer input.
- Added float preview support in `MainWindow` with `PreviewFloatValue`, `PreviewFloatMinimum`, `PreviewFloatMaximum`, and `PreviewFloatDecimals` dependency properties plus a new `CFFloat` preview card and decimals test input in the `UI Preview` tab.
- Added `PreviewColorValue` plus a new `CFColor` preview card in `MainWindow` so the custom color well and upgraded picker can be tested from the `UI Preview` tab.
- Updated `CFInt` and `CFFloat` so the keyboard up/down arrow keys now use each control's configured `Step` value, matching the spinner button behavior.
- Added `PreviewIntStep` and `PreviewFloatStep` dependency properties plus new `Step` test inputs on the `CFInt` and `CFFloat` preview cards so step-size behavior can be tested directly from the preview page.
- Reevaluated UI colors after `Color\agentic_colors.md` changed at `2026-05-21 10:22:00` and updated `CF.TreeView.Default.Border` in `AgenticColorCreator.App\Styles\CFDarkStyles.xaml` from `#FF828790` to `#FF4C4C4C` to match the latest `TreeView / Border Default` value.
- Updated `CFTextBox` so invalid text turns red while typing, valid text clears the local foreground override back to the normal themed style, and invalid text is not externally committed.
- Updated `CFTextBox` external value application so bound value updates refresh the visible text and validation state without restarting the delayed commit cycle.
- Configured `CFInt` to use `ValidationMode="NumberOnly"` on its internal `CFTextBox`, with number validation now using real invariant integer parsing instead of character-only matching.
- Restored `CFInt` clamped delegated updates so when committed text is outside `Minimum` or `Maximum`, the visible text is updated back to the coerced in-range value.
- Verified the current change with `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` and `dotnet test "AgenticColorCreator.Tests\AgenticColorCreator.Tests.csproj"`.
- Verified formatting for the touched files and normalized `AgenticColorCreator.Core\Services\ColorSpaceConverter.cs`, `AgenticColorCreator.Tests\ColorSpaceConverterTests.cs`, `AgenticColorCreator.App\UserControls\CFColorControl\TransparentCheckerBrushFactory.cs`, `AgenticColorCreator.App\UserControls\CFColorControl\CFColor.xaml`, `AgenticColorCreator.App\UserControls\CFColorControl\CFColor.xaml.cs`, `AgenticColorCreator.App\Dialogs\ColorPickerWindow.xaml`, `AgenticColorCreator.App\Dialogs\ColorPickerWindow.xaml.cs`, `AgenticColorCreator.App\ViewModels\ColorPickerViewModel.cs`, `AgenticColorCreator.App\MainWindow.xaml`, and `AgenticColorCreator.App\MainWindow.xaml.cs` back to `CRLF`.

## Notes For Next Contributor
- Read this file before making changes.
- For code, config, or test changes, read `codestandards.md` before editing.
- Keep this file short and practical; remove stale entries instead of appending long history.
- If the task touches UI colors, compare `Color\agentic_colors.md` against the recorded timestamp in `AGENTS.md` before changing mappings.
- Manually verify the current `CFTextBox`, `CFInt`, `CFFloat`, `CFColor`, and upgraded color-picker behavior in the `UI Preview` tab before treating the UX as final.
