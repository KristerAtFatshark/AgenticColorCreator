# CFCheckBox

`AgenticColorCreator.App.UserControls.CFCheckBoxControl.CFCheckBox`

## Overview

A check box that adds a mixed (indeterminate) state on top of the shared `CF.CheckBox` style. It wraps
a standard WPF `CheckBox`, so the visuals are identical to the themed check box used elsewhere.

## Files

- `CFCheckBox.xaml` / `CFCheckBox.xaml.cs` — the control (wraps a styled `CheckBox`).

## Dependency Properties

- `IsChecked` (`bool`, two-way) — the committed boolean value.
- `IsMixedState` (`bool`, two-way, default `false`) — when `true`, the inner check box shows the
  native indeterminate glyph (its `IsChecked` is set to `null`, which the `CF.CheckBox` style renders
  via its `indeterminateMark`).
- `Label` (`object`) — the caption shown next to the box. Named `Label` (not `Content`) to avoid
  hiding `ContentControl.Content`.

## Behavior

- The inner check box is `IsThreeState="False"`, so a user click on an indeterminate box resolves to
  `true`.
- Any user click commits a real boolean to `IsChecked` and clears `IsMixedState`.
- Programmatic state pushes (host setting `IsChecked` / `IsMixedState`) are guarded internally so they
  are not mistaken for user toggles.

## Mixed State

Host-controlled. Set `IsMixedState = true` when the bound values disagree; the control clears it on the
next user toggle and commits the chosen boolean.

## Example

```xaml
<checkBoxControls:CFCheckBox
    Label="Visible"
    IsChecked="{Binding IsVisible, Mode=TwoWay}"
    IsMixedState="{Binding IsVisibleMixed, Mode=TwoWay}" />
```
