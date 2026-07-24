# CFTextBox

`AgenticColorCreator.App.UserControls.CFTextBoxControl.CFTextBox`

## Overview

A themed text box with delayed ("eventual") commit, input validation, and mixed-state support. It is
the shared text-editing layer that `CFInt` and `CFFloat` are built on, but it can be used directly for
plain text input as well.

## Files

- `CFTextBox.xaml` / `CFTextBox.xaml.cs` — the control.
- `MixedVisualVisibilityConverter.cs` — shows/hides the mixed-state overlay vs. the editable field.
- `TextBoxGrayIfMixedConverter.cs` — grays the text while mixed.

## Dependency Properties

- `Value` (`string`, two-way) — the committed value the host binds to.
- `DisplayedText` (`string`, two-way) — the text currently shown in the field. May differ from
  `Value` while the user is typing and before a commit.
- `Text` (`string`) — inner text-box text, used internally for validation/commit plumbing.
- `IsEditing` (`bool`) — `true` while the user is actively typing; set on key down, cleared on focus
  loss.
- `IsMixedState` (`bool`, default `false`) — mixed-state flag (see below).
- `ValidationMode` (`CFTextBoxValidationMode`, default `AlphaNumericPath`) — `AlphaNumericPath`,
  `NumberOnly`, or `FloatNumber`.
- `DecimalPlaces` (`int`, default `2`) — maximum decimals allowed in `FloatNumber` mode.

## Events

- `ValueCommitted` (routed, bubbling) — raised whenever a valid value is committed.

## Validation

`ValidationMode` selects the rule applied to the current text:

- `AlphaNumericPath` — letters, digits, and `/ . _ -` (path-friendly).
- `NumberOnly` — a valid `int`.
- `FloatNumber` — a valid invariant-culture `float` with at most `DecimalPlaces` decimals.

Invalid text turns the field red and is **not** committed.

## Commit Behavior

A pending edit is committed on any of:

- pressing `Enter`,
- losing keyboard focus (tab-out or click-outside),
- a 1000 ms idle timer elapsing while the control still has focus.

## Mixed State

`IsMixedState` is host-controlled. While mixed the editable text is blanked. Committing a non-empty
valid value from mixed state pushes it to `Value` and sets `IsMixedState = false`; committing an empty
value keeps the control mixed. The control never sets mixed state on its own — only clears it on a real
user commit.

## Example

```xaml
<textControls:CFTextBox
    Value="{Binding Name, Mode=TwoWay}"
    ValidationMode="AlphaNumericPath"
    IsMixedState="{Binding NameIsMixed, Mode=TwoWay}" />
```
