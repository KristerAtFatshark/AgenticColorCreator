# CFTreeViewControl

This folder contains the custom TreeView implementation used by the UI preview.

Namespace: `ClownFishUi.CFUserControls.CFTreeViewControl`

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

Public dependency properties:

- `IsMultiSelect`
  - Type: `bool`
  - Default: `false`
  - Enables custom multi-selection behavior.
  - `false`: normal single selection behavior.
  - `true`: supports multi-selection driven by `SelectedValues` and by `Ctrl` + click.

- `NodesSource`
  - Type: `ObservableCollection<TreeViewSourceEntry>`
  - The flat source data for the tree.
  - This is the main collection you bind from a view model.
  - Full replacement of the DP value triggers exactly one rebuild.
  - Full-replace bursts on the same instance (`Clear` + N × `Add`) are auto-coalesced (see "Rebuild Pipeline").

- `SelectedValues`
  - Type: `ObservableCollection<string>`
  - External selection input/output based on `Value` paths.
  - Bind this if selection should be controlled from outside the TreeView.
  - The control also writes back to this collection when the user selects items manually.

- `SelectedTreeViewItems`
  - Type: `IReadOnlyList<CFTreeViewItem>`
  - Output-only view of the currently selected rendered items.
  - Useful when the host wants access to the actual visual TreeView items.

- `CollapseAllThresholdItemCount`
  - Type: `int`
  - Default: `100`
  - Controls whether items are created already-collapsed when the source is large.
  - Decision is made from `NodesSource.Count` **before** any `CFTreeViewItem` is created:
    - `threshold <= 0` disables the auto-collapse (items always start expanded).
    - `NodesSource.Count > threshold` builds every item with `IsExpanded = false`.
    - Otherwise items start expanded (the previous default behavior).
  - Changing this DP at runtime re-evaluates the collapse state on the already-built tree; it does not rebuild.

- `EnableLazyChildMaterialization`
  - Type: `bool`
  - Default: `false`
  - When `true`, subtrees at or below `LazyChildMaterializationDepth` are not built up front. A single `TreeViewItem` sentinel with `Visibility=Collapsed`, `Focusable=false` and `IsEnabled=false` is inserted into the parent's `Items` so WPF still renders the expand chevron without showing a placeholder row, and the real `CFTreeViewItem` children are created on first `Expanded`.
  - Expansion reveals **one layer at a time**. When a lazy parent is expanded, its immediate children are materialized in a collapsed state so their own lazy sentinels stay in place until the user expands them explicitly. This prevents the whole subtree from cascade-materializing in a single click.
  - Lazy is automatically **skipped** when the current rebuild would not start collapsed — that is, when `NodesSource.Count <= CollapseAllThresholdItemCount`. In that scenario everything is visible-expanded anyway, so there is no collapse state to defer work behind.
  - Ancestor items of any value in `SelectedValues` are always force-materialized eagerly, so external selection continues to land on a real item.

- `LazyChildMaterializationDepth`
  - Type: `int`
  - Default: `2`
  - Zero-based node depth at which lazy materialization becomes active. Root items are depth `0`. A value of `2` means depths `0` and `1` are always materialized eagerly, and every subtree rooted at depth `2` or deeper defers its children until first expansion.
  - Only meaningful when `EnableLazyChildMaterialization` is `true`.

Public events:

- `RebuildCompleted`
  - Type: `EventHandler`
  - Raised on the UI thread after each rebuild finishes, including the `NodesSource == null` clear path. Useful for hosts and tests that need to await the async rebuild pipeline before inspecting the visual tree.

Public methods:

- `BeginUpdate()` / `EndUpdate()`
  - Reference-counted batch pair; nesting is supported.
  - While the suppression counter is non-zero, source-triggered rebuilds are suppressed and only marked as pending.
  - The outermost `EndUpdate` triggers exactly one rebuild if any were pending.
  - Useful for hosts that mutate `NodesSource` across multiple dispatcher frames (e.g. an async load that awaits between mutations), where the automatic same-frame coalescer can't help.
  - Call only from the UI thread.

- `CollapseAll()`
  - Collapses every item in the tree.

- `CollapseAllExceptSelectedItemParents()`
  - Collapses every item except the ancestor paths of the currently-selected items, so the selection stays visible while everything else is collapsed.

- `SelectFirstItemAndFocus()`
  - Selects the first root item, brings it into view, and focuses it on the next `DispatcherPriority.Loaded` tick so the container has time to realize before focus is moved.
  - Returns the selected `CFTreeViewItem` or `null` if the tree is empty.
  - Uses the same forced-selection-clear + `SelectSingleItem` code path as a manual click in multi-select mode, so `SelectedValues` and `SelectedTreeViewItems` update the same way as a user-driven selection.
  - The deferred focus call is guarded (`IsLoaded` / `Focusable` / `IsVisible`) and swallows `InvalidOperationException` from `PresentationCore` in case the container was virtualized away between `BringIntoView` and the callback.

### `CFTreeViewItem`

The custom item container used inside the tree. Derives from `TreeViewItem`.

Public properties:

- `Icon` (DP, `string`)
  - Resource key for the icon glyph, resolved through `EditorIconGlyphs.xaml` and rendered with the `fs-editor-icons` font (see `TreeViewIconMap`). This is populated for you when items are built from `NodesSource`; you normally do not set it directly.

- `Text` (DP, `string`)
  - The visible label shown for the item.

- `Value` (CLR, `string`)
  - The full path value for the item.
  - Example: `controls/inputs/textbox`

- `IsMultiSelected` (DP, `bool`)
  - Internal custom selection flag used for multi-selection visuals.

Internal (not part of the public API, but visible to the surrounding control code):

- `SourceNode` (`TreeViewNode`)
  - Back-reference to the intermediate node used by the lazy-materialization path.

- `HasLazyPlaceholder` (`bool`)
  - Marks the item as still holding a lazy sentinel child. Cleared by the control when the sentinel is swapped for real materialized children on first `Expanded`.

The item also gets a `ToolTip` bound to its `Value` in the shared `CF.TreeViewItem` style, so hovering shows the full path.

### `TreeViewSourceEntry`

This is the source data structure you should normally create and bind.

Properties:

- `Value` (`string`)
  - The path for the item.
  - Example: `palette/primary`

- `Type` (`string`)
  - The semantic type for the leaf item.
  - Example: `palette`, `control`, `folder`.

This is the data structure intended for external use.

### `TreeViewNode`

Internal hierarchical structure used by `CFTreeView` after transforming `TreeViewSourceEntry` items into a tree.

Properties:

- `Text`
- `Value`
- `Icon`
- `Children` (`List<TreeViewNode>`)
- `ChildIndex` (`Dictionary<string, TreeViewNode>`, ordinal-ignore-case)

Notes:

- `Children` is a plain `List<T>`, not an `ObservableCollection`, so nothing is bound to it and no `INotifyCollectionChanged` plumbing runs during the build.
- `ChildIndex` is used to look up existing children by path in O(1) while inserting new source entries; this replaced the previous per-segment linear scan.
- You should not need to construct or bind to `TreeViewNode` directly.

### `TreeViewIconMap`

Static type-to-icon lookup used by the control. The lookup no longer returns literal emoji glyphs; it returns a **resource key** into the shared `EditorIconGlyphs.xaml` dictionary (vendored via `AgenticColorCreator.App\Shared\Styles\EditorIconGlyphs.xaml`), and the treeview `DataTemplate` resolves that key to an actual glyph string through the `StringToResourceConverter` and renders it with the `fs-editor-icons` font via the `CF.IconTextBlock` style.

Current mappings (`source Type` → `resource key`):

- `default`              → `icon-resource_default`
- `folder`               → `icon-folder`
- `level`                → `icon-resource_level`
- `unit`                 → `icon-resource_unit`
- `wwise_bank`           → `icon-resource_wwise_bank`
- `wwise_event`          → `icon-resource_wwise_event`
- `state_machine`        → `icon-resource_state_machine`
- `material`             → `icon-resource_material`
- `texture`              → `icon-resource_texture`
- `shading_environment`  → `icon-resource_shading_environment`
- `item`                 → `icon-resource_item`
- `template_definition`  → `icon-resource_template_definition`
- `particles`            → `icon-resource_particles`
- `control`              → `icon-resource_default`
- `palette`              → `icon-resource_default`

Usage inside the control:

```csharp
var iconResourceKey = TreeViewIconMap.GetIcon(type);
```

Lookup rules:

- Lookup is ordinal-ignore-case on the source `Type`.
- If the requested `Type` is missing from the map, `GetIcon` falls back to the `default` entry instead of throwing.
- If the returned resource key is missing from `EditorIconGlyphs.xaml` at runtime, `StringToResourceConverter` falls back to the character `"\uE903"` so the treeview cell still renders something with the icon font.

Rendering pipeline (roughly):

1. `TreeViewIconMap.GetIcon(type)` → resource-key string (e.g. `icon-resource_level`).
2. Treeview `DataTemplate` binds that string through `StringToResourceConverter`, which does an `Application.Current.TryFindResource(key)` against the merged `EditorIconGlyphs.xaml` and returns the mapped glyph character.
3. The `TextBlock` uses the `CF.IconTextBlock` style, whose `FontFamily` is set to `/AgenticColorCreator.App;component/Shared/Fonts/#fs-editor-icons` so the returned character renders as the intended icon.

Notes for callers adding new types:

- Add the source `Type` string on the `TreeViewSourceEntry` side.
- Add the corresponding entry in `TreeViewIconMap` mapping it to a resource key that already exists in `EditorIconGlyphs.xaml`. Never edit `EditorIconGlyphs.xaml` inside this repository — that file is vendored from another project and is regenerated externally from `fs-editor-icons.css`. If a needed glyph is missing there, that has to be resolved in the upstream project first.
- The `default` entry acts as the fallback for every unmapped source `Type`, so unknown types render a generic resource icon rather than throwing.

## How To Bind It

Typical usage:

```xaml
<userControls:CFTreeView
    NodesSource="{Binding PreviewTreeViewNodes}"
    SelectedValues="{Binding PreviewSelectedTreeViewValues}"
    SelectedTreeViewItems="{Binding RelativeSource={RelativeSource AncestorType=Window}, Path=SelectedPreviewTreeViewItems, Mode=OneWayToSource}"
    CollapseAllThresholdItemCount="100"
    EnableLazyChildMaterialization="False"
    LazyChildMaterializationDepth="2"
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
  - normal single selection is used (the item's `IsSelected` is set).

- If `SelectedValues` contains more than `1` item:
  - custom multi-selection visuals are used (each match's `IsMultiSelected` is set).
  - this only works meaningfully when `IsMultiSelect` is `true`.

External selection changes:

- When `SelectedValues` changes, the tree updates selection **in place** via a `Value` → `CFTreeViewItem` lookup dictionary; it does not rebuild.
- The first externally-selected item is scrolled into view on the next `DispatcherPriority.Loaded` tick via `BringIntoView`.

Manual interaction behavior:

- Clicking an item manually updates the selection.
- If a forced selection was active, manual clicking clears that forced state first.
- `Ctrl` + click toggles additional items only when `IsMultiSelect` is `true`.
- Manual clicking on the expand chevron/`ToggleButton` does not change selection.

Manual selection is written back into `SelectedValues` when it is bound.

Important note:

- Forced selection and manual selection are treated as separate modes.
- Manual clicking overrides a forced selection.
- Updating `SelectedValues` from outside overrides any previous manual selection.

## Keyboard Behavior

- Arrow keys (`Up`/`Down`/`Left`/`Right`) navigate between items using the built-in `TreeView` navigation. Focus moves to each traversed item container.
- `Enter` commits the focused item as the current selection. It routes through the same forced-selection-clear + selection path as a mouse click, but keeps the item's built-in `IsSelected` flag set (in addition to the CF `IsMultiSelected` flag when multi-select is active). Both flags render the same selected visual in the CF item style, and keeping `IsSelected` set is what lets subsequent arrow keys continue from the just-selected row instead of jumping back to the first root.
- `Ctrl` + `Enter` toggles multi-selection on the focused item, matching `Ctrl` + click. It intentionally does not force `IsSelected` on because a toggled-off item should not remain the navigation anchor.
- `PageUp`, `PageDown`, `Home`, and `End` behave as the built-in `TreeView` defines them.
- All navigation keys (`Up`, `Down`, `Left`, `Right`, `PageUp`, `PageDown`, `Home`, `End`) are swallowed on the bubbling `KeyDown` after the built-in `TreeView` has had its turn. That prevents an ancestor `ScrollViewer` from scrolling the whole hosting page when the user hits the top or bottom row of the tree.
- Focus is not re-asserted after a keyboard commit; the item is already focused (which is how the handler found it), and re-focusing during the built-in selection-change reentry can throw `InvalidOperationException` from `PresentationCore`.

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

Intermediate path segments like `controls` and `inputs` are automatically treated as `folder` nodes and use the `folder` icon.

Empty or whitespace-only source values are ignored.

## Rebuild Pipeline

A rebuild is triggered when:

- `NodesSource` DP is replaced with a new collection.
- The bound `NodesSource` collection raises `CollectionChanged`.

The pipeline is split so the expensive intermediate work can run off the UI thread:

1. **UI thread** — Any inbound source change routes through `RequestRebuildTreeViewItems`. It:
   - Honors `_updateSuppressionCount`; if non-zero (batch is open) it just marks a rebuild pending and returns.
   - Bumps a monotonic `long` rebuild generation via `Interlocked.Increment`.
   - Clears current selection tracking and the `Value` → `CFTreeViewItem` lookup.
   - Snapshots the source into a `TreeViewSourceEntry[]` (required because `ObservableCollection` is not thread-safe).
2. **Background thread** — `Task.Run` executes the pure `BuildTreeViewNodes(snapshot)` which walks the flat entries and produces a `List<TreeViewNode>` hierarchy using per-node `ChildIndex` dictionaries for O(1) sibling lookup.
3. **UI thread** — `Dispatcher.BeginInvoke(DispatcherPriority.Background, ...)` marshals the result back. It:
   - Compares the captured generation against the current one via `Interlocked.Read`; stale results (a newer rebuild is already in flight) are dropped.
   - Constructs `CFTreeViewItem` instances from the node tree (DP thread-affinity requires this on the UI thread).
   - Skips explicit assignment of DP values equal to their default (`IsExpanded=false`, `IsSelected=false`, `IsMultiSelected=false`) to avoid unnecessary `SetValue` calls per item.
   - Assigns the final `List<CFTreeViewItem>` as a batch to `TreeView.ItemsSource` so the WPF virtualizing panel generates containers on demand instead of eagerly for every node.

### Automatic full-replace coalescing

`CFTreeView` treats its source as full-replace by design. Rather than requiring hosts to wrap `Clear` + N × `Add` bursts in `BeginUpdate`/`EndUpdate` manually, the control does it itself:

- On the first `CollectionChanged` in a burst it internally calls `BeginUpdate()` and queues `EndUpdate()` at `DispatcherPriority.Background`.
- That priority runs after all pending `Clear`/`Add` callbacks on the current dispatcher frame but before the next render pass, so the entire burst coalesces into exactly **one** rebuild.
- Guarded by an internal `_isCoalescingSourceChanges` flag so nested notifications don't open a second batch.
- Nests correctly with manual host `BeginUpdate()` / `EndUpdate()` calls — they share the same reference counter.

Replacing `NodesSource` at the DP level already produces a single notification, so it does not need coalescing.

### Lazy child materialization

When `EnableLazyChildMaterialization` is `true` and the current rebuild would otherwise start collapsed, `CreateTreeViewItem` stops recursing into children once it reaches `LazyChildMaterializationDepth`. Instead:

- A single `TreeViewItem` sentinel (`Visibility=Collapsed`, `Focusable=false`, `IsEnabled=false`) is added to the parent's `Items` so WPF reports `HasItems=true` and renders the expand chevron. Because the sentinel is collapsed and disabled it never shows a placeholder row and never accepts focus.
- The parent's `HasLazyPlaceholder` flag is set and a one-shot handler is attached to its `Expanded` event.
- On first expansion, `OnLazyItemExpanded` verifies `e.OriginalSource` matches the sender (so bubbled expansions from descendants do not trigger it), unhooks itself, clears the sentinel, and calls `MaterializeLazyChildren`.
- `MaterializeLazyChildren` walks up the parent chain via `ItemsControl.ItemsControlFromItemContainer` to compute the current depth, then reuses `CreateTreeViewItem` with `startCollapsed=true` and the current lazy config to build the next layer only. Any grandchildren still at or below the lazy depth get their own sentinel, so exactly one layer materializes per user expand.

Ancestor items of any value in `SelectedValues` are always materialized eagerly during the initial build so external selection lands on a real item even when lazy is active.

### `RebuildCompleted` event

Every rebuild ends with a `RebuildCompleted` event raised on the UI thread — including the `NodesSource == null` clear path. Hosts and tests can subscribe to await the async pipeline before inspecting the visual tree:

```csharp
treeView.RebuildCompleted += (_, _) => { /* tree is ready */ };
```

### Manual batching (`BeginUpdate` / `EndUpdate`)

For hosts that mutate `NodesSource` across multiple dispatcher frames or want to guarantee coalescing without relying on the same-frame heuristic:

```csharp
treeView.BeginUpdate();
try
{
    // Any number of mutations to NodesSource here, including full replacement.
    NodesSource.Clear();
    foreach (var entry in newEntries)
    {
        NodesSource.Add(entry);
    }
}
finally
{
    treeView.EndUpdate();
}
```

Rules:

- Every `BeginUpdate` must be paired with an `EndUpdate`.
- Pairs may nest.
- Only the outermost `EndUpdate` triggers a rebuild, and only if at least one rebuild was requested while suppressed.
- Call only from the UI thread.

## Virtualization

Both the outer `TreeView` and the item container style enable virtualization and recycling:

- `VirtualizingPanel.IsVirtualizing="True"`
- `VirtualizingPanel.VirtualizationMode="Recycling"`
- Items panel is `VirtualizingStackPanel` on both the tree and each item.

Combined with the batched `ItemsSource` assign, WPF only generates `CFTreeViewItem` containers for items currently in the viewport. Large trees stay responsive even when many items remain expanded, and starting large trees collapsed via `CollapseAllThresholdItemCount` further reduces initial container-generation cost.

## Runtime Updates

`NodesSource` is an `ObservableCollection<TreeViewSourceEntry>`.

Supported operations at runtime:

- add entries
- remove entries
- replace entries (item-by-item, or `Clear` + repopulate)
- replace the whole collection instance via the DP

When the collection changes, `CFTreeView` rebuilds the visible tree via the pipeline described above.

Current limitation:

- Mutating properties on an existing `TreeViewSourceEntry` instance (changing `Value` or `Type`) does not automatically refresh the tree. Replace the entry, or trigger a `Clear` + repopulate, to update.

## Recommended Usage Pattern

Use `TreeViewSourceEntry` in the view model as the source of truth.

Recommended responsibilities:

- View model owns:
  - `ObservableCollection<TreeViewSourceEntry>` (the flat source)
  - `ObservableCollection<string>` for selected paths

- `CFTreeView` owns:
  - hierarchy building (path splitting, folder-node synthesis)
  - icon resolution via `TreeViewIconMap`
  - click, keyboard, and multi-selection behavior (including `Enter` / `Ctrl+Enter` commit and swallowing bubble-out of navigation keys)
  - rendering custom `CFTreeViewItem` containers
  - rebuild scheduling, threading, and coalescing
  - collapse-on-threshold decision at build time
  - optional lazy child materialization for very large sources
  - convenience helpers such as `SelectFirstItemAndFocus`, `CollapseAll`, and `CollapseAllExceptSelectedItemParents`

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

Multi selection (requires `IsMultiSelect="True"`):

```csharp
PreviewSelectedTreeViewValues.Clear();
PreviewSelectedTreeViewValues.Add("palette/primary");
PreviewSelectedTreeViewValues.Add("palette/accent");
```
