# CFRadioButton

`AgenticColorCreator.App.UserControls.CFRadioButtonControl.CFRadioButton`

## Overview

A radio button that adds a mixed (indeterminate) state on top of the shared `CF.RadioButton` style. It
wraps a standard WPF `RadioButton`, so the visuals match the themed radio button used elsewhere, and it
supports grouping via `GroupName`.

Because a plain `RadioButton` has no built-in visual for the indeterminate value, the `CF.RadioButton`
style renders mixed state as a **hollow orange ring in the center** (instead of the solid filled dot
used for the checked state).

## Files

- `CFRadioButton.xaml` / `CFRadioButton.xaml.cs` — the control (wraps a styled `RadioButton`).

The mixed-state ring visual lives in the `CF.RadioButton` control template in
`AgenticColorCreator.App/CFStyles/CFDarkStyles.xaml` (a `mixedMark` ellipse shown only when the inner
`IsChecked` is `null`).

## Dependency Properties

- `IsChecked` (`bool`, two-way) — the committed boolean value.
- `IsMixedState` (`bool`, two-way, default `false`) — when `true`, the inner radio button shows the
  native indeterminate value (`IsChecked = null`), which the `CF.RadioButton` style renders as the
  hollow orange ring.
- `Label` (`object`) — the caption shown next to the button. Named `Label` (not `Content`) to avoid
  hiding `ContentControl.Content`.
- `GroupName` (`string`) — forwarded to the inner `RadioButton` so multiple `CFRadioButton`s with the
  same group name behave as one mutually-exclusive group.

## Behavior

- The inner radio button is `IsThreeState="False"`, so a user click on an indeterminate button resolves
  to `true`.
- Any user selection commits a real boolean to `IsChecked` and clears `IsMixedState`.
- Programmatic state pushes (host setting `IsChecked` / `IsMixedState`) are guarded internally so they
  are not mistaken for user selections.

## Mixed State

Host-controlled. Set `IsMixedState = true` when the bound values disagree; the control clears it on the
next user selection and commits the chosen boolean. In mixed state the center shows an orange ring
rather than a filled dot.

## Example

```xaml
<radioButtonControls:CFRadioButton
    Label="Option A"
    GroupName="Options"
    IsChecked="{Binding OptionAChecked, Mode=TwoWay}"
    IsMixedState="{Binding OptionsMixed, Mode=TwoWay}" />
<radioButtonControls:CFRadioButton
    Label="Option B"
    GroupName="Options"
    IsChecked="{Binding OptionBChecked, Mode=TwoWay}"
    IsMixedState="{Binding OptionsMixed, Mode=TwoWay}" />
```
