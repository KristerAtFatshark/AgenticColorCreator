# CFListTreeView

`AgenticColorCreator.App.UserControls.CFListTreeViewControl.CFListTreeView`

## Overview

`CFListTreeView` renders a tree through one recycling WPF `ListView`. It builds a persistent plain CLR
folder/leaf graph from the complete source, then projects expanded or filtered nodes into a flat visible
row list. It avoids the nested `TreeViewItem` container cost of a standard WPF `TreeView`.

## Framework Compatibility

The application project defines `NETCORE` for `net8.0-windows`. CFListTreeView uses that symbol only
for compiler/framework syntax differences:

- `NETCORE`: nullable-reference declarations and other net8-compatible declarations.
- Without `NETCORE`: C# 7.3-compatible declarations with no nullable-reference syntax, file-scoped
  namespaces, target-typed construction, `init` accessors, or newer pattern matching.

The fallback production files plus the generated XAML partial class compile directly with Roslyn
`/langversion:7.3` against .NET Framework 4.7.2 WPF references (`WindowsBase`, `PresentationCore`,
`PresentationFramework`, `System.Xaml`, and Ribbon). No warnings are disabled in CFListTreeViewControl.
The UI preview model `PreviewCFListTreeItem` follows the same `NETCORE`/C# 7.3 namespace and nullable
declaration pattern.

## Files

- `CFListTreeView.xaml` / `CFListTreeView.xaml.cs` - control, projection, interaction, and binding logic.
- `ICFTreeViewItem.cs` - structural contract implemented by source objects.
- `CFListTreeNode.cs` - internal persistent folder/leaf graph node.
- `CFListTreeViewRow.cs` - stable row model consumed by the virtualized ListView.
- `ReferenceEqualityComparer.cs` - reusable generic class-identity comparer; CFListTreeView uses
  `ReferenceEqualityComparer<object>.Default` for filtering and selection lookups.
- `CFListTreeViewIconMap.cs` - maps dotless resource types/extensions to editor glyph resource keys.
- `CFListTreeViewIconImages.cs` - converts mapped font glyphs into cached, frozen vector images for the
  default, mouse-over, and selected states.

## Source Contract

Every source object must implement `ICFTreeViewItem`:

- `TreeFolderPath`: parent folder path separated by `/`. Null or empty places the leaf at root.
- `TreeSortKey`: optional case-insensitive sibling ordering key. It is not displayed or required to be
  unique. Include the dotless extension in this value when resources share a base name, for example
  `cryptic_color_decals.level` and `cryptic_color_decals.unit`.
- `ResourceName`: actual leaf display name without its extension, for example `cryptic_color_decals`.
- `ResourceType`: resource extension/type without the dot, for example `level`.

Leaf visuals are supplied separately through `ItemTemplate`, which receives the original source object.
The control maps `ResourceType` to an editor glyph and renders it before that template. Synthetic folders
always use `icon-folder`.

`MatchedItems` and `SelectedItems` are correlated to `SourceItems` by object reference, not by
`IComparable`, `Equals`, resource name, or sort key. This preserves distinct resources that happen to
have equal names or sort keys. A production `ListCollectionView` must therefore yield the same object
instances that are present in `SourceItems`.

Example item metadata:

```text
TreeFolderPath = "content/debug/cryptic_colors"
TreeSortKey = "cryptic_color_decals.level"
ResourceName = "cryptic_color_decals"
ResourceType = "level"
```

## Dependency Properties

- `SourceItems` (`IEnumerable`) - complete scope and structural source.
- `MatchedItems` (`IEnumerable`) - optional filtered subset of the same object instances.
- `SelectedItems` (`IList`) - selected original source objects; synchronized in both directions.
  The list must support `Clear` and `Add`; arrays and read-only/fixed-size lists are rejected.
- `ItemTemplate` (`DataTemplate`) - leaf presentation.
- `IsMultiSelect` (`bool`, default `false`) - enables Ctrl-click and Ctrl+Enter toggling.
- `CollapseAllThreshold` (`int`, default `100`) - starts all folders collapsed when the number of
  unique valid source assets is greater than this value. Values less than or equal to zero disable
  automatic collapse. Synthesized folders and the current `MatchedItems` filter do not affect the count.
- `ShowResourceType` (`bool`, default `true`) - shows the dotless `ResourceType` as a dark gray file
  extension after the host-provided leaf `ItemTemplate`. Set false to display only the resource name.
  The extension changes to `CF.Text.Default.Foreground` (`#FFC8C8C8`) while its item is selected.
- `VisibleRowCount` (`int`, read-only) - current flattened row count, including folder rows.

Public event:

- `StructureLoadCompleted` - raised after `SourceItems` has been rebuilt into the persistent graph.
  Changing `MatchedItems`, selection, expansion, or display options does not raise this event.

## Structure Loading

Only `SourceItems` builds structure. The control snapshots its entries, ignores objects that do not
implement `ICFTreeViewItem`, synthesizes shared folder nodes, creates leaf nodes retaining their original
objects, builds reference-identity indexes, sorts siblings, and generates the visible row projection.
If the same object reference appears more than once, only its first occurrence is loaded because filtering
and selection identify leaves by reference identity.

Folders sort before leaves. Folders sort case-insensitively by name; leaves sort by `TreeSortKey`; source
order breaks ties. Path and sort metadata are read only during structure loading. Replace or notify the
source collection after changing structural metadata.

## Resource Type Icons

`CFListTreeViewIconMap` resolves the dotless `ResourceType` case-insensitively. Current mappings are:

- `animation` -> `icon-resource_animation`
- `item` -> `icon-resource_item`
- `level` -> `icon-resource_level`
- `material` -> `icon-resource_material`
- `particles` -> `icon-resource_particles`
- `shading_environment` -> `icon-resource_shading_environment`
- `state_machine` -> `icon-resource_state_machine`
- `template_definition` -> `icon-resource_template_definition`
- `texture` -> `icon-resource_texture`
- `unit` -> `icon-resource_unit`
- `wwise_bank` -> `icon-resource_wwise_bank`
- `wwise_event` -> `icon-resource_wwise_event`

Null, empty, and unknown types use `icon-resource_default`. Add new mappings only to
`CFListTreeViewIconMap`; callers should provide resource types, not presentation resource keys.
The mapped font glyph is converted once into shared frozen `DrawingImage` instances for default, hover,
and selected colors. Recycled rows render those cached vector images without repeated text shaping or
font rendering. A benchmarked cached-font alternative was simpler, but its expanded mixed-icon scrolling
times clustered above the DrawingImage median. The ranges overlap, so this is a modest rendering
optimization rather than the fix for the prior stall; that stall came from the malformed extension
binding. The extension now uses direct bindings without `StringFormat`, avoiding an exception for every
realized or recycled leaf row.

Structure construction is synchronous in the first version. The measured 50,000-item benchmark stays
well below the current threshold, and synchronous loading avoids accessing arbitrary UI-bound objects on
a worker thread. The public completion event leaves room for a generation-based asynchronous pipeline if
production profiling later demonstrates a need.

If `SourceItems` implements `INotifyCollectionChanged`, same-dispatcher-frame changes are coalesced into
one rebuild. Queued notifications from a replaced source are discarded. `StructureLoadCompleted` is
raised after each completed structural load.

When the loaded source asset count exceeds `CollapseAllThreshold`, folder rows are created in a collapsed
state before the first visible projection. Lowering the threshold at runtime can collapse the existing
structure without rebuilding it. Raising or disabling the threshold does not automatically expand folders,
which preserves explicit user expansion choices.

## Visible Row Projection

The persistent graph and the ListView rows are separate concerns:

- The graph contains every synthesized folder and source leaf.
- Expansion and filtering produce a flat list containing only rows that should currently be visible.
- Stable row objects are reused when that projection changes.
- A flat node index supports whole-tree operations without repeatedly walking the hierarchy.
- Repeated folder paths are cached while building large sources.
- Sparse filters clear only nodes marked by the previous filter pass.
- Queued notifications from replaced source or match collections are discarded.

This projection model is what allows one recycling ListView to behave like a tree without nested WPF
item containers.

## Filter Mask

`MatchedItems` never rebuilds the graph:

- Null means no active filter; normal expansion state determines visible rows.
- A non-null empty collection means an active filter with zero results.
- Otherwise matching leaves and their ancestor folders are displayed.

Matching uses source-object reference identity, which fits a `ListCollectionView` over the same source
objects. Unknown objects are ignored. Collection-view reset/refresh notifications rerun only the marking
pass and flat projection; queued notifications from a replaced match view are discarded.

Filtering temporarily reveals required ancestor paths without changing stored `IsExpanded` values.
Clearing the filter therefore restores the user's expansion state.

## Selection And Interaction

- Folder rows toggle expansion and never enter `SelectedItems`.
- Leaf clicks replace selection; Ctrl-click toggles when `IsMultiSelect` is enabled.
- Enter commits a leaf or toggles a folder; Ctrl+Enter toggles multi-selection.
- Up, Down, Home, End, PageUp, and PageDown navigate visible rows.
- Left collapses or moves to the parent; Right expands or moves to the first child.
- Selected leaves remain selected when a filter temporarily hides them.
- External selections scroll the first currently visible selected leaf into view.
- Source rebuilds remove selected objects that are no longer present and remove duplicate references.

Public helpers:

- `CollapseAll()`.
- `ExpandAll()`.
- `CollapseAllExceptSelectedItemParents()`.
- `SelectFirstItemAndFocus()`.
- `ForceSelection(object item)` replaces current selection with a source object, expands its ancestors,
  scrolls/focuses it, and returns false when the object is not in `SourceItems`. It does not create a
  persistent lock: the next click or Enter selection replaces it normally.

## Virtualization And Styling

`CF.CFListTreeView` uses a recycling `VirtualizingStackPanel` with logical scrolling. Rows reuse the
existing `CF.TreeView...` and `CF.TreeViewItem...` brushes, expander template, icon font, indentation,
hover colors, and selected colors so the control matches the established tree visual language without duplicating color
values. Synthetic folders use `icon-folder`.

Keyboard focus uses the dedicated `CF.CFListTreeViewFocusVisual`: a 1px dashed
`CF.Text.Default.Foreground` (`#FFC8C8C8`) rectangle with zero inset, sized to the same ListViewItem
bounds as the orange selected `RowBorder`. Selection and keyboard focus therefore remain visually
distinct without the smaller black Windows default adorner.

The loaded scrolling test verifies that approximately one viewport of containers is realized rather than
one container per source item. Current local measurements include 12 realized containers while scrolling
an expanded mixed-icon tree containing 10,061 visible rows. Performance tests also cover a 50,000-item
structure and repeated sparse filter masks.

## Example

```xaml
<listTreeControls:CFListTreeView
	SourceItems="{Binding AllItemsInScope}"
	MatchedItems="{Binding FilteredItemsView}"
	SelectedItems="{Binding SelectedItems}"
	ShowResourceType="True"
	IsMultiSelect="True">
	<listTreeControls:CFListTreeView.ItemTemplate>
		<DataTemplate>
			<TextBlock Text="{Binding ResourceName}" />
		</DataTemplate>
	</listTreeControls:CFListTreeView.ItemTemplate>
</listTreeControls:CFListTreeView>
```

The objects yielded by `FilteredItemsView` must be the same instances present in `AllItemsInScope`.

## UI Preview

The `UI Preview` tab contains a production-shaped example with realistic resource paths, duplicate base
names with different extensions, varied resource-type icons, and generated stress entries. It exposes:

- Text filtering through a `ListCollectionView`, including zero-result queries.
- `Stress` source-item count.
- `Collapse Threshold`.
- `Show Extensions` (`ShowResourceType`).
- Selected-item and visible-row readouts.
- Collapse all, collapse to selected, and select-first actions.
- Expand all and force-select-second actions.
