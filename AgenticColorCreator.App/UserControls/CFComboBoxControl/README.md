# CFComboBox

`AgenticColorCreator.App.UserControls.CFComboBoxControl.CFComboBox`

## Overview

A combo box that adds a mixed state on top of the shared `CF.ComboBox` chrome. It wraps a standard WPF
`ComboBox` (styled with `CF.ComboBox` / `CF.ComboBoxTemplate` / `CF.ComboPopupItemStyle`), so the
visuals match the themed combo box used elsewhere.

The mixed state uses the same visual pattern as `CFColor`: a translucent overlay with a `– Mixed –`
label is drawn over the selection display. The overlay is hit-test-transparent, so the drop-down can
still be opened underneath it; picking an item commits a real value and clears the mixed state.

## Files

- `CFComboBox.xaml` / `CFComboBox.xaml.cs` — the control (wraps a styled `ComboBox`).

## Dependency Properties

- `ItemsSource` (`IEnumerable`) — the items to display; forwarded to the inner `ComboBox`.
- `SelectedItem` (`object`, two-way) — the committed selected item.
- `SelectedIndex` (`int`, two-way, default `-1`) — the committed selected index.
- `DisplayMemberPath` (`string`) — forwarded to the inner `ComboBox`.
- `IsMixedState` (`bool`, two-way, default `false`) — when `true`, draws the `– Mixed –` overlay over
  the selection display.

`SelectedItem` and `SelectedIndex` are kept in sync with the inner `ComboBox`; bind whichever one
suits the host.

## Behavior

- The inner `ComboBox`'s selection is synchronized with the public `SelectedItem` / `SelectedIndex`
  through an internal `_isSyncingSelection` guard, so host-driven and user-driven changes are
  distinguished.
- A **user** selection (picking an item from the drop-down) commits the value and clears
  `IsMixedState`.
- A **host** selection (setting the bound `SelectedItem` / `SelectedIndex` programmatically) updates
  the display but does **not** clear `IsMixedState`, so an initially-mixed control stays mixed until
  the user actually picks something.
- While mixed the overlay covers the value text but is `IsHitTestVisible="False"`, so the drop-down
  still opens.

## Mixed State

Host-controlled. Set `IsMixedState = true` when the bound values disagree across a multi-selection;
the control clears it on the next user pick.

## Example

```xaml
<comboBoxControls:CFComboBox
    ItemsSource="{Binding States}"
    SelectedItem="{Binding State, Mode=TwoWay}"
    IsMixedState="{Binding StateIsMixed, Mode=TwoWay}" />
```
