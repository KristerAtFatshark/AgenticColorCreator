# Status

## General Status

- The WPF app builds and supports creating, loading, editing, validating, and saving `agentic_colors.md`.
- The main UI contains `Colors` and `UI Preview` tabs.
- Shared theme resources live in `AgenticColorCreator.App\CFStyles\CFDarkStyles.xaml`.
- `CFListTreeView` is the only custom tree control and is backed by one recycling virtualized `ListView`.
- Other previewed controls include `CFTextBox`, `CFInt`, `CFFloat`, `CFSlider`, `CFColor`, `CFHdrColor`, `CFCheckBox`, `CFRadioButton`, `CFComboBox`, and `CFWindow`.
- UI color source: `Color\agentic_colors.md`, timestamp `2026-08-31 12:47:36`.

## CFListTreeView

- `SourceItems` builds a persistent folder/leaf graph from `ICFTreeViewItem` metadata.
- `MatchedItems` masks the graph without rebuilding it; null disables filtering and an empty collection shows no results.
- `SelectedItems` uses source-object identity and must be a mutable `IList`.
- `CollapseAllThreshold` defaults to `100`; values `<= 0` disable automatic collapse.
- `ShowResourceType` controls dark gray file extensions; selected extensions use `#FFC8C8C8`.
- Resource icons are mapped by dotless `ResourceType` and cached as frozen `DrawingImage` instances.
- Keyboard focus uses a full-size 1px dashed `#FFC8C8C8` focus visual; committed selection keeps the orange border.
- Public helpers: `CollapseAll()`, `CollapseAllExceptSelectedItemParents()`, and `SelectFirstItemAndFocus()`.
- The UI Preview includes realistic resource data, filtering, stress count, threshold, extension toggle, selection readout, and helper actions.
- `CFListTreeViewControl\README.md` documents the complete API and behavior.

## Current Verification

- Release app build passes with zero warnings.
- All remaining tests pass: 59 total, including 27 CFListTreeView functional tests and 3 CFListTreeView performance tests.
- CFWindowControl's non-`NETCORE` branch compiles with C# 7.3 against .NET Framework 4.7.2 WPF references; the net8 branch resolves `NETCORE`, its isolated `FinalOutputPath80`, disabled framework suffix appending, and `CA1416` exclusion as intended.
- CFListTreeView performance tests cover 50,000-item loading, sparse masks, flat scrolling, and expanded mixed-icon scrolling.
- Loaded scrolling tests confirm approximately one viewport of ListView containers is realized.

## Active Issues

- Debug or parallel builds can fail while another process locks intermediate DLLs; rerun builds serially.
- `CFTextBox`, numeric controls, color controls, and picker behavior still benefit from manual visual confirmation.
- Descriptions are saved as one canonical markdown line even if entered over multiple lines.
- No drag/drop reordering or general color search/filtering exists yet.

## Workarounds

- Close a running app before rebuilding if assemblies are locked.
- Use `dotnet build "AgenticColorCreator.App\AgenticColorCreator.App.csproj" -c Release` when a debug build is blocked.
- Use the `UI Preview` tab for manual visual and interaction checks.

## Recent Important Changes

- Added the conditional net8 project output block: `NETCORE` is defined for `net8.0-windows`, output is routed through externally overrideable `FinalOutputPath80` (default `bin\$(Configuration)\net8.0-windows\`), framework suffix appending is disabled, and `CA1416` is excluded for the Windows-only assembly.
- Added `NETCORE` branches to CFWindowControl for nullable/file-scoped/generic-marshal net8 syntax and plain C# 7.3/block-scoped/non-generic-marshal .NET Framework syntax. The fallback compiles against .NET Framework 4.7.2 references without defining `NETCORE`.
- Removed the superseded TreeView-backed control, preview card/state, styles, and performance tests after replacing it with `CFListTreeView`.
- Shared `CF.TreeView...` brush keys and `CF.TreeViewExpanderToggleTemplate` remain intentionally because CFListTreeView uses them; no legacy control type or style remains.
- Added class-level documentation and final behavior cleanup for CFListTreeView selection, focus, sorting, and filtering.
- Added `notes.md` with integration follow-ups for toolscommon, shared `KnownTypes`, and Charlie's branch.

## Notes For Next Contributor

- Read this file before changes and `codestandards.md` before code/config/test changes.
- Do not edit `AgenticColorCreator.App\Shared\` unless explicitly requested.
- Keep all touched files CRLF; use tab indentation in C#, XAML, and project files.
