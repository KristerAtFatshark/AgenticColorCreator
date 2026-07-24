# CFInt

`AgenticColorCreator.App.UserControls.CFIntControl.CFInt`

## Overview

An integer editor: a `CFTextBox` (in `NumberOnly` validation mode) plus up/down spinner buttons. It
reuses `CFTextBox` for the editable surface, delayed commit, validation, and mixed state, and adds
integer parsing, range clamping, and stepping on top.

## Files

- `CFInt.xaml` / `CFInt.xaml.cs` — the control (composes `CFTextBox`).

## Dependency Properties

- `Value` (`int`, two-way) — the committed integer, always clamped to `[Minimum, Maximum]`.
- `Minimum` (`int`, default `int.MinValue`).
- `Maximum` (`int`, default `int.MaxValue`).
- `Step` (`int`, default `1`) — spinner / arrow-key increment.
- `IsMixedState` (`bool`, default `false`) — forwarded to the inner `CFTextBox`.
- `TextValue` (`string`) — internal bridge between the numeric value and the text surface.

## Behavior

- The up/down spinner buttons and the `Up`/`Down` arrow keys add/subtract `Step`, clamp to
  `[Minimum, Maximum]`, and clear mixed state.
- Manual text entry is parsed on commit (via the inner `CFTextBox` commit rules — `Enter`, focus loss,
  or idle timer). Invalid or unparseable text reverts to the last valid value.

## Mixed State

Host-controlled `IsMixedState` is passed straight through to the inner `CFTextBox`. Any real user
change (typing a valid number and committing, or using the spinner/arrows) clears it.

## Example

```xaml
<intControls:CFInt
    Value="{Binding Count, Mode=TwoWay}"
    Minimum="0"
    Maximum="100"
    Step="1"
    IsMixedState="{Binding CountIsMixed, Mode=TwoWay}" />
```
