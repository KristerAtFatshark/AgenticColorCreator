# CFFloat

`AgenticColorCreator.App.UserControls.CFFloatControl.CFFloat`

## Overview

A floating-point editor: a `CFTextBox` (in `FloatNumber` validation mode) plus up/down spinner buttons
and configurable decimal precision. It mirrors `CFInt` but works with `float` values.

## Files

- `CFFloat.xaml` / `CFFloat.xaml.cs` — the control (composes `CFTextBox`).

## Dependency Properties

- `Value` (`float`, two-way) — the committed value, clamped to `[Minimum, Maximum]` and rounded to
  `Decimals`.
- `Minimum` (`float`, default `float.MinValue`).
- `Maximum` (`float`, default `float.MaxValue`).
- `Step` (`float`, default `1`) — spinner / arrow-key increment.
- `Decimals` (`int`, default `2`) — rounding precision and the max decimals accepted by validation.
- `IsMixedState` (`bool`, default `false`) — forwarded to the inner `CFTextBox`.
- `TextValue` (`string`) — internal text bridge.

## Behavior

- Values are rounded away-from-zero to `Decimals` and formatted with invariant culture (so `.`
  is always the decimal separator).
- The up/down spinner buttons and the `Up`/`Down` arrow keys add/subtract `Step`, clamp to range, and
  clear mixed state.
- Manual entry is parsed on commit; invalid text reverts to the last valid value.

## Mixed State

Host-controlled `IsMixedState` is forwarded to the inner `CFTextBox`; a real user change clears it.

## Example

```xaml
<floatControls:CFFloat
    Value="{Binding Opacity, Mode=TwoWay}"
    Minimum="0"
    Maximum="1"
    Step="0.05"
    Decimals="2"
    IsMixedState="{Binding OpacityIsMixed, Mode=TwoWay}" />
```
