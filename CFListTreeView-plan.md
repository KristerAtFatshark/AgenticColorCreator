# CFListTreeView Implementation Plan

## Goal

Create `CFListTreeView`, a tree-style WPF control backed by a virtualized `ListView`. It should provide
the important behavior expected from a tree while avoiding the cost of nested WPF `TreeViewItem` containers.
Its visuals should match the established tree visual language.

The control separates structural loading from filtering:

- `SourceItems` defines the complete scope and tree structure.
- `MatchedItems` is a subset of `SourceItems` and only masks the existing structure.
- Changing a filter must not rebuild or reparse the structural model.

## External Item Contract

Add or reuse `ICFTreeViewItem` as the structural contract implemented by `ContentBrowserItem`.

Required members:

- `TreeFolderPath`: forward-slash-separated parent folder path with no leading or trailing separator.
  Null or empty places the item at the root.
- `TreeSortKey`: optional string used to order the item among sibling leaves. It is not displayed and
  does not need to be unique. It can include the extension when base names collide.
- `ResourceName`: displayed resource base name without its extension.
- `ResourceType`: resource extension/type without the dot; the control maps it to an editor icon.

The interface should not prescribe leaf visuals. The original source object is passed to a host-supplied
`ItemTemplate` for display.

## Public API

Add these dependency properties:

- `SourceItems` (`IEnumerable`): complete structural scope. Expected production binding:
  `IReadOnlyList<ContentBrowserItem> AllItemsInScope`.
- `MatchedItems` (`IEnumerable`): current filtered subset. Expected production binding:
  `ListCollectionView FilteredItemsView`.
- `SelectedItems` (`IList`): two-way collection of selected source objects.
- `ItemTemplate` (`DataTemplate`): template used to render a leaf's original source object.
- `IsMultiSelect` (`bool`, default `false`): enables Ctrl-based multi-selection.

Add these public methods:

- `CollapseAll()`.
- `CollapseAllExceptSelectedItemParents()`.
- `SelectFirstItemAndFocus()`.

Add a structure-load completion event so hosts and tests can await asynchronous structural loading.
Use a name such as `StructureLoaded` or `StructureLoadCompleted`, finalized during implementation.

## Internal Model

Use a persistent non-visual graph built from `SourceItems`:

- Folder nodes synthesized from `TreeFolderPath` segments.
- Leaf nodes containing the original `ICFTreeViewItem` object.
- Parent reference on every node.
- Ordered child lists on folder nodes.
- Depth and expansion state.
- Stable source-order index for deterministic sort tie-breaking.
- Reference-identity lookup from source object to leaf node.
- Optional folder-path lookup for fast folder reuse while loading.
- Stable row/view-model objects that can be reused across filter and expansion updates.

Synthetic folder rows are not selectable. Leaf rows retain the original source object and are the only
rows represented by `SelectedItems`.

## Source Loading

`SourceItems` defines structure and is the only input that causes a structural rebuild.

Loading pipeline:

1. Enumerate and snapshot `SourceItems` on the UI thread so an arbitrary `IEnumerable` or collection
   view is not accessed concurrently.
2. Validate or ignore entries that do not implement `ICFTreeViewItem`, with final error behavior made
   explicit during implementation.
3. Read `TreeFolderPath` and `TreeSortKey` once per item.
4. Build the folder graph from normalized path segments.
5. Create one leaf node per source item and attach it to its parent folder or the root.
6. Build identity and folder lookup indexes.
7. Sort each sibling set once.
8. Create the initial flat visible-row list from expansion state.
9. Publish the result on the UI thread and raise the completion event.

Perform pure graph construction off the UI thread when profiling shows it is beneficial. Use a monotonic
generation token so a stale load cannot replace a newer `SourceItems` result.

If `SourceItems` implements `INotifyCollectionChanged`, subscribe to it. Replacing the property or
receiving a collection change requests a structural rebuild. Coalesce same-dispatcher-frame mutation
bursts so `Clear` followed by many adds produces one load.

`TreeFolderPath` is treated as immutable while an item remains in the current source. To move an item,
the host replaces `SourceItems` or raises an applicable source collection change that causes a rebuild.
The first version does not subscribe to property changes on every source item.

## Sorting

Ordering rules:

1. Folders appear before leaves within the same parent.
2. Folder siblings sort by normalized folder name/path segment.
3. Leaf siblings sort by `TreeSortKey`.
4. Equal or missing keys retain stable source order.

Precompute normalized comparison data during source loading. Comparisons during sorting should avoid
repeated path parsing and casing work.

The supplied `CreateEntry` and `BuildSortKey` examples are inspiration only. Their exact composite-key
encoding, marker characters, buffer allocation, and normalization algorithm are not required. Choose
the simplest implementation that satisfies the ordering rules, then use profiling to decide whether a
precomputed composite ordinal key is preferable to a dedicated comparer.

## Flattened ListView Presentation

Host a `ListView` configured with `VirtualizingStackPanel` and recycling virtualization.

The ListView receives a flat list of visible row objects. Each row contains enough state to render:

- Folder or leaf row kind.
- Depth/indentation.
- Expansion state and whether children exist.
- Original source item for leaf `ContentPresenter` rendering.
- Folder display name and folder icon for synthetic rows.
- Resource name and mapped resource-type icon for leaf rows.
- Selection and focus state required by the custom interaction model.

Do not create nested `ItemsControl`, `TreeViewItem`, or child `ListView` hierarchies. Expanding,
collapsing, or filtering recalculates the flat visible-row projection while reusing persistent graph and
row objects.

## Filtering And MatchedItems

Filtering is a marking pass over the existing model, not a structural rebuild.

Semantics:

- `MatchedItems == null`: no active filter; show rows according to normal expansion state.
- Non-null, empty `MatchedItems`: active filter with zero results; show no rows.
- Non-null, non-empty `MatchedItems`: show matching leaves and the ancestor folders needed to preserve
  their tree context.

Filtering pipeline:

1. Enumerate the current `MatchedItems` view.
2. Resolve each result through the source-object reference-identity lookup.
3. Mark its leaf node as matched.
4. Follow parent links and mark every ancestor as required for filter visibility.
5. Regenerate only the flat visible-row projection.

Ignore matched objects not present in `SourceItems`. Do not split folder paths, synthesize folders,
resort siblings, or recreate the graph during this pass.

Subscribe when `MatchedItems` implements `INotifyCollectionChanged`. A `ListCollectionView.Refresh()`
normally emits a reset notification; handle that by rerunning only the marking pass. Use generation or
dispatcher coalescing if rapid filter notifications can otherwise apply stale masks.

## Expansion During Filtering

Keep persistent user expansion state separate from filter-forced visibility.

- While a filter is active, temporarily reveal every ancestor required to reach a matched leaf.
- Do not overwrite the user's stored expansion choices while revealing filtered paths.
- When `MatchedItems` returns to null, restore presentation from the stored expansion state.
- Folder expansion changes while filtered should have explicitly tested behavior. Prefer recording the
  user's chosen persistent state while still ensuring required match ancestors remain visible until the
  filter clears.

Expansion changes update only the visible-row projection.

## Selection

- Only leaf rows are selectable; clicking a folder toggles expansion without adding it to selection.
- `SelectedItems` contains original source objects, not internal row wrappers.
- Support single selection and optional multi-selection.
- Ctrl-click and Ctrl+Enter toggle leaf selection in multi-select mode.
- A normal click or Enter replaces the selection.
- External `SelectedItems` changes apply selection in place through the identity lookup.
- User selection writes original source objects back to `SelectedItems`.
- Keep selected source objects selected when a filter temporarily hides them.
- Scroll the first externally selected item into view when it is currently visible.

Use synchronization guards to prevent collection-update loops between the control and the host.

## Keyboard And Mouse Interaction

Match the useful legacy tree interaction behavior:

- Up/Down move through visible rows, skipping non-selectable folders when committing selection but still
  allowing folder focus if needed for expansion navigation.
- Left collapses an expanded folder or moves focus to its parent.
- Right expands a collapsed folder or moves focus to its first visible child.
- Enter selects a leaf or toggles a focused folder.
- Ctrl+Enter toggles a leaf in multi-select mode.
- Home, End, PageUp, and PageDown navigate the virtualized list.
- Navigation keys are handled inside the control so an ancestor `ScrollViewer` does not scroll instead.
- Clicking an expander does not change leaf selection.

Finalize whether folders can receive keyboard focus during implementation; they remain non-selectable
regardless.

## Visual Design

Match the established tree visuals as closely as possible:

- Same background, border, text, selected, hover, icon, and expander colors.
- Same row height, padding, indentation, and expander geometry.
- Same `CF.IconTextBlock` font/icon rendering pipeline.
- Synthetic folders use `icon-folder`.
- Leaf content is rendered through `ItemTemplate` so production can display `ContentBrowserItem` data.
- Leaf icons are mapped from `ResourceType`; callers do not provide presentation resource keys.

Add dedicated styles such as `CF.CFListTreeView` and `CF.CFListTreeViewItem`, but reference the existing
`CF.TreeView...` and `CF.TreeViewItem...` brushes where the visuals are intentionally identical. Avoid
duplicating color values.

## UI Preview

Add a separate `CFListTreeView` card to the `UI Preview` tab.

The preview should include:

- Persistent complete source collection bound to `SourceItems`.
- `ListCollectionView` bound to `MatchedItems`.
- A simple filter input and clear-filter action.
- A query that produces zero results.
- Single and multi-selection demonstrations.
- Collapse all, collapse to selected, and select-first actions.
- Configurable stress-entry count.
- Readouts for selected source objects and visible row count.
- A demonstration that clearing a filter restores the pre-filter expansion state.

## Tests

### Structural Tests

- Root-level items from null/empty `TreeFolderPath`.
- Folder synthesis from one-level and nested paths.
- Shared folders are created once.
- Folders sort before leaves.
- Folder and leaf sibling sorting follows normalized keys and stable source-order tie-breaking.
- Replacing or changing `SourceItems` rebuilds structure once.
- Stale asynchronous loads cannot replace newer source results.
- Non-`ICFTreeViewItem` behavior matches the documented policy.

### Filtering Tests

- Null `MatchedItems` means no active filter.
- Non-null empty `MatchedItems` displays zero rows.
- Matches display with all required ancestors.
- Unmatched branches are hidden.
- Unknown matched objects are ignored.
- `ListCollectionView.Refresh()` updates the mask without rebuilding structure.
- Repeated match changes reuse the same graph and row objects.
- Clearing the filter restores expansion state.

### Selection And Interaction Tests

- Folder rows cannot enter `SelectedItems`.
- Single and multi-selection synchronize both directions.
- Hidden selected leaves remain selected across filters.
- External visible selection scrolls into view.
- Mouse expansion and keyboard navigation follow the specified rules.
- Collapse helpers and `SelectFirstItemAndFocus()` work with the flat model.

### Performance Tests

- Load a representative large `SourceItems` scope and record graph-build plus first-display timing.
- Apply many successive `MatchedItems` refreshes and assert no structural rebuild occurs.
- Measure zero-result, sparse-result, and broad-result masks separately.
- Compare initial load, memory use, and filter latency against the recorded legacy baseline.
- Confirm ListView recycling remains enabled and realized container count stays close to viewport size.

Keep expensive performance tests separately identifiable so normal test runs can exclude them reliably.

## Documentation And Completion

- Add `CFListTreeViewControl/README.md` describing the interface, data lifecycle, filter mask, flat-row
  projection, selection, keyboard behavior, styling, and examples.
- Add XML documentation to non-obvious public APIs and the structural/filter pipeline boundaries.
- Verify CRLF line endings and tab indentation for all touched code, project, and XAML files.
- Build the app, run functional tests, run targeted performance tests, and manually verify the preview.
- Update `status.md` with implementation state, measurements, limitations, and usage notes.

## Suggested Implementation Order

1. Define `ICFTreeViewItem`, internal node/row models, and source loading tests.
2. Implement persistent graph construction, sorting, and identity indexes.
3. Implement flat expansion projection and virtualized ListView presentation.
4. Match the established tree styling and folder visuals.
5. Implement `MatchedItems` masking and expansion-state restoration.
6. Implement selection synchronization and mouse interaction.
7. Implement keyboard navigation and helper methods.
8. Add UI Preview integration.
9. Add load/filter performance coverage and compare with the recorded legacy baseline.
10. Complete README, formatting checks, build, tests, and manual verification.
