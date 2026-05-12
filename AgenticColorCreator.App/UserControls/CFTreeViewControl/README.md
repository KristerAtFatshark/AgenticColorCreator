# CFTreeViewControl

This folder contains the custom TreeView implementation used by the UI preview.

Files in this folder:

- `CFTreeView.xaml`
- `CFTreeView.xaml.cs`
- `CFTreeViewItem.cs`
- `TreeViewSourceEntry.cs`
- `TreeViewNode.cs`
- `TreeViewIconMap.cs`

## Overview

`CFTreeView` is a custom WPF user control that builds a visible tree from a flat list of path-based source items.

The input data is not hierarchical. Instead, the control expects a flat collection of entries like:

```csharp
new TreeViewSourceEntry { Value = "controls/inputs/textbox", Type = "control" }
new TreeViewSourceEntry { Value = "palette/primary", Type = "palette" }
```

`CFTreeView` splits the `Value` by `/` and creates the visible hierarchy at runtime.

Example:

```text
controls/inputs/textbox
```

becomes:

```text
controls
  inputs
    textbox
```

## Main Classes

### `CFTreeView`

The main user control.

Public properties:

- `IsMultiSelect`
  - Type: `bool`
  - Enables custom multi-selection behavior.
  - `false`: normal single selection behavior.
  - `true`: supports multi-selection by selected value list and by `Ctrl` + click.

- `NodesSource`
  - Type: `ObservableCollection<TreeViewSourceEntry>`
  - The flat source data for the tree.
  - This is the main collection you bind from a view model.
  - When entries are added or removed, the tree rebuilds automatically.

- `SelectedValues`
  - Type: `ObservableCollection<string>`
  - External selection input/output based on `Value` paths.
  - This is the property to bind if selection should be controlled from outside the TreeView.
  - The control also writes back to this collection when the user selects items manually.

- `SelectedTreeViewItems`
  - Type: `IReadOnlyList<CFTreeViewItem>`
  - Output-only view of the currently selected rendered items.
  - Useful when the host wants access to the actual visual TreeView items.

### `CFTreeViewItem`

The custom item container used inside the tree.

Public properties:

- `Icon`
  - The icon text shown for the item.

- `Text`
  - The visible label shown for the item.

- `Value`
  - The full path value for the item.
  - Example: `controls/inputs/textbox`

- `IsMultiSelected`
  - Internal custom selection flag used for multi-selection visuals.

### `TreeViewSourceEntry`

This is the source data structure you should normally create and bind.

Properties:

- `Value`
  - The path for the item.
  - Example: `palette/primary`

- `Type`
  - The semantic type for the leaf item.
  - Example: `palette`, `control`

This is the data structure intended for external use.

### `TreeViewNode`

This is the internal hierarchical structure used by `CFTreeView` after transforming `TreeViewSourceEntry` items.

Properties:

- `Text`
- `Value`
- `Type`
- `Icon`
- `Children`

You usually do not need to bind to `TreeViewNode` directly.

## How To Bind It

Typical usage:

```xaml
<userControls:CFTreeView
	NodesSource="{Binding PreviewTreeViewNodes}"
	SelectedValues="{Binding PreviewSelectedTreeViewValues}"
	SelectedTreeViewItems="{Binding RelativeSource={RelativeSource AncestorType=Window}, Path=SelectedPreviewTreeViewItems, Mode=OneWayToSource}"
	IsMultiSelect="True" />
```

Typical view model properties:

```csharp
public ObservableCollection<TreeViewSourceEntry> PreviewTreeViewNodes { get; } = [];

public ObservableCollection<string> PreviewSelectedTreeViewValues { get; } = [];
```

## Selection Rules

`SelectedValues` controls selection by matching item `Value` paths.

Behavior:

- If `SelectedValues` contains `0` items:
  - no forced selection is applied.

- If `SelectedValues` contains `1` item:
  - normal single selection is used.

- If `SelectedValues` contains more than `1` item:
  - custom multi-selection visuals are used.
  - this only works correctly when `IsMultiSelect` is `true`.

Manual interaction behavior:

- Clicking an item manually updates the selection.
- If there was an externally forced selection, manual clicking clears that forced state first.
- `Ctrl` + click toggles additional items only when `IsMultiSelect` is `true`.

Important note:

- Forced selection and manual selection are treated as separate modes.
- Manual clicking overrides a forced selection.
- Updating `SelectedValues` from outside overrides any previous manual selection.

## How The Tree Is Built

`CFTreeView` reads each `TreeViewSourceEntry.Value` and splits it by `/`.

Example input:

```csharp
new TreeViewSourceEntry { Value = "controls/inputs/textbox", Type = "control" }
new TreeViewSourceEntry { Value = "controls/inputs/combobox", Type = "control" }
new TreeViewSourceEntry { Value = "palette/primary", Type = "palette" }
```

Visible tree result:

```text
controls
  inputs
    textbox
    combobox
palette
  primary
```

Intermediate path segments like `controls` and `inputs` are automatically treated as `folder` nodes.

## Icon Mapping

`TreeViewIconMap.cs` contains the type-to-icon lookup used by the control.

Current mappings:

- `folder` -> `📁`
- `control` -> `🧩`
- `palette` -> `🎨`

Usage inside the control:

```csharp
var icon = TreeViewIconMap.GetIcon(type);
```

If a type is missing from the map, `GetIcon` throws an exception.

If you add a new `Type` value to `TreeViewSourceEntry`, you should also add a mapping in `TreeViewIconMap.cs`.

## Runtime Updates

`NodesSource` is an `ObservableCollection<TreeViewSourceEntry>`.

That means these operations are supported at runtime:

- add entries
- remove entries
- replace entries

When the collection changes, `CFTreeView` rebuilds the visible tree.

Current limitation:

- changing `Value` or `Type` on an existing `TreeViewSourceEntry` instance does not automatically refresh the tree unless the collection itself changes or the item is replaced.

## Recommended Usage Pattern

Use `TreeViewSourceEntry` in the view model as the source of truth.

Recommended responsibilities:

- View model owns:
  - `ObservableCollection<TreeViewSourceEntry>`
  - `ObservableCollection<string>` for selected paths

- `CFTreeView` owns:
  - hierarchy building
  - icon resolution
  - click selection behavior
  - rendering custom `CFTreeViewItem` containers

## Example Source Data

```csharp
PreviewTreeViewNodes.Add(new TreeViewSourceEntry
{
	Value = "palette/primary",
	Type = "palette",
});

PreviewTreeViewNodes.Add(new TreeViewSourceEntry
{
	Value = "controls/inputs/textbox",
	Type = "control",
});
```

## Example External Selection

Single selection:

```csharp
PreviewSelectedTreeViewValues.Clear();
PreviewSelectedTreeViewValues.Add("palette/primary");
```

Multi selection:

```csharp
PreviewSelectedTreeViewValues.Clear();
PreviewSelectedTreeViewValues.Add("palette/primary");
PreviewSelectedTreeViewValues.Add("palette/accent");
```
