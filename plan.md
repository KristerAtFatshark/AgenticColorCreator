# Agentic Color Creator Plan

## Product Shape

1. Build a desktop Windows app in `C#` with `WPF`.
2. The app's primary purpose is to create, load, edit, validate, and save an `agentic_colors.md` file.
3. The file should stay human-readable first, but structured enough that other agentic tools can parse it reliably.

## Recommended File Format

Use Markdown with a small, strict structure rather than freeform prose.

Example:

```md
# Agentic Colors

## Metadata
- format: agentic-colors/v1
- name: Default Theme

## Category: Surface

### Surface / App Background
- value: #FF101418
- description: Main application window background.

### Surface / Panel
- value: #FF1A2026
- description: Secondary container backgrounds such as cards and panels.

## Category: Text

### Text / Primary
- value: #FFF3F5F7
- description: Default foreground color for high emphasis text.
```

Why this format:

1. Easy for humans to scan.
2. Easy for agents to parse by headings and list keys.
3. Categories are naturally collapsible in the UI.
4. Each color entry has the required fields: `name`, `value`, `category`, `description`.

## Internal Data Model

Use a minimal model first:

1. `AgenticColorsDocument`
   - `Title`
   - `FormatVersion`
   - `ObservableCollection<ColorCategory> Categories`
2. `ColorCategory`
   - `Name`
   - `ObservableCollection<AgenticColorItem> Colors`
   - `IsExpanded`
3. `AgenticColorItem`
   - `Name`
   - `HexValue`
   - `Description`
   - Derived/display `ColorPreview`

This keeps the saved file and UI model closely aligned.

## Application Architecture

Recommended approach: `MVVM`.

Main pieces:

1. `Models`
   - document/category/color item models
2. `Services`
   - markdown parser
   - markdown writer
   - color validation/conversion service
   - file dialog service
3. `ViewModels`
   - `MainWindowViewModel`
   - `CategoryViewModel`
   - `ColorItemViewModel`
4. `Views`
   - `MainWindow.xaml`
   - data templates for category groups and color items

Why MVVM here:

1. WPF data binding fits it naturally.
2. Easier to keep editing state, validation, and save/load logic clean.
3. Better for future additions like import/export or search.

## UI Plan

Main window layout:

1. Top toolbar
   - `New`
   - `Open`
   - `Save`
   - `Save As`
   - `Add Category`
   - `Add Color`
2. Main content area
   - scrollable list of categories
   - each category shown in an `Expander`
3. Inside each category
   - vertically stacked color items
   - button to add a color into that category

## Color Item Layout

Per the requirement, each item should look like this:

1. First row
   - `Name` textbox
   - color preview rectangle
   - hex textbox using full ARGB form like `#FFAABBCC`
2. Second row
   - multiline `Description` textbox

Recommended details:

1. The preview rectangle updates live from the hex string.
2. The hex field validates on edit.
3. Invalid hex shows a validation state instead of silently failing.
4. Optional small category label can be shown if items are reused outside grouped view.

## Editing Behavior

1. User can add/remove categories.
2. User can add/remove colors within categories.
3. User can edit:
   - name
   - hex value
   - description
   - category via grouping location
4. Drag/drop reordering can wait until phase 2 unless explicitly needed in v1.

## Parsing and Saving Rules

1. On open:
   - parse Markdown headings and list fields
   - preserve known structure
   - reject malformed required fields with a clear error
2. On save:
   - normalize output to a canonical format
   - always write full 8-digit ARGB hex
   - keep one consistent heading/list style

Canonicalizing the file is useful because agent tools prefer predictable formatting.

## Validation Rules

1. `Name` required.
2. `Category` required.
3. `HexValue` required.
4. `HexValue` must be `#AARRGGBB`.
5. `Description` should be allowed empty, but recommended.
6. Duplicate names within the same category should warn.

## Implementation Phases

1. Scaffold solution
   - create WPF app
   - wire MVVM structure
2. Define models and markdown format
   - implement document model
   - implement parser/writer
3. Build main editor UI
   - toolbar
   - scrollable category list
   - collapsible sections
   - editable color items
4. Add validation and live preview
   - hex parsing
   - error states
   - unsaved changes tracking
5. File workflow
   - new/open/save/save as
   - basic error dialogs
6. Polish
   - keyboard shortcuts
   - better spacing/layout
   - sample starter file

## Initial Technical Decisions

1. Target `.NET 8` WPF unless older Windows support is required.
2. Start without external UI frameworks.
3. Use built-in WPF binding and `INotifyPropertyChanged`.
4. Use `ObservableCollection<T>` for live list updates.

## Open Questions

1. Should the file format stay exactly Markdown-only, or can it embed a fenced YAML/JSON block inside Markdown for stricter parsing?
2. Should category names be fully user-defined, or should the app ship with starter categories like `Surface`, `Text`, `Border`, `Accent`, `Status`?
3. Should v1 support editing the color visually with a picker, or only preview plus hex editing?

## Recommended v1 Scope

1. Markdown-based format as defined above.
2. WPF MVVM desktop app.
3. Load/save `agentic_colors.md`.
4. Collapsible categories.
5. Scrollable editable color items.
6. Live hex-to-preview binding.
7. Validation for malformed colors.
