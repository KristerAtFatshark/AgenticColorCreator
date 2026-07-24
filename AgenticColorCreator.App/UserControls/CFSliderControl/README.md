# CFSlider

`AgenticColorCreator.App.UserControls.CFSliderControl.CFSlider`

## Overview

A horizontal slider paired with an embedded `CFFloat` numeric editor. Dragging the slider and editing
the number stay in sync, and the control supports ticks, snapping, and mixed state.

## Files

- `CFSlider.xaml` / `CFSlider.xaml.cs` — the control (hosts a WPF `Slider` + an embedded `CFFloat`).

## Dependency Properties

- `Value` (`float`, two-way) — clamped to `[Minimum, Maximum]`.
- `Minimum` (`float`, default `0`).
- `Maximum` (`float`, default `100`).
- `Step` (`float`, default `1`) — drives both the slider's small/large change and the numeric editor
  increment. The control deliberately exposes a single increment concept instead of separate
  small/large change values.
- `Decimals` (`int`, default `2`) — precision of the embedded `CFFloat`.
- `TickFrequency` (`double`, default `0`).
- `TickPlacement` (`System.Windows.Controls.Primitives.TickPlacement`, default `None`).
- `Ticks` (`DoubleCollection`) — explicit tick positions.
- `IsSnapToTickEnabled` (`bool`, default `false`) — snaps the thumb (and the numeric value) to ticks.
- `IsMixedState` (`bool`, default `false`) — collapses the slider behind a `- Mixed -` overlay and
  forwards mixed state to the embedded `CFFloat`.

## Behavior

- The embedded `CFFloat`'s width is computed deterministically from the widest formatted range bound
  at the current `Decimals` (plus spinner padding), so the layout does not shift as the value changes.
  It is recalculated on load and whenever the range or `Decimals` change.
- When `IsMixedState` is true, the actual `Slider` is collapsed under the overlay (matching the
  `CFColor` mixed-state pattern) while the embedded `CFFloat` inherits the mixed state.
- The value is clamped to `[Minimum, Maximum]` whenever the value or the range changes.

## Mixed State

Host-controlled. A real user change (dragging the slider or committing a value in the embedded
`CFFloat`) clears it via the inner editor's commit path.

## Example

```xaml
<sliderControls:CFSlider
    Value="{Binding Weight, Mode=TwoWay}"
    Minimum="0"
    Maximum="10"
    Step="0.5"
    Decimals="1"
    TickFrequency="1"
    TickPlacement="BottomRight"
    IsSnapToTickEnabled="True"
    IsMixedState="{Binding WeightIsMixed, Mode=TwoWay}" />
```
