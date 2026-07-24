# CFColor

`AgenticColorCreator.App.UserControls.CFColorControl.CFColor`

## Overview

An SDR color-well control: a swatch button that opens the interactive color picker, an inline editable
hex field, and a transparency checker rendered behind semi-transparent colors.

## Files

- `CFColor.xaml` / `CFColor.xaml.cs` — the control.
- `CFColorValueTypes.cs` — `CFColorRgb` and `CFColorHsv` value types.
- `CFColorConverters.cs` — XAML converters wrapping the static conversion helpers.
- `TransparentCheckerBrushFactory.cs` — builds the checker brush shown under transparency.

## Dependency Properties

- `Value` (`string`, two-way) — the color as an `#AARRGGBB` hex string (default `#FF000000`).
- `IsMixedState` (`bool`, default `false`) — hides the hex field behind a `- Mixed -` overlay while
  keeping the swatch/picker interactive.

## Events

- `ValueChanged` (routed, bubbling) — raised when `Value` changes.

## Conversion Helpers

Static methods (also available to bindings via `CFColorConverters.cs`):

- `TryConvertHexToRgb(hex, out CFColorRgb)`
- `TryConvertHexToHsv(hex, out CFColorHsv)`
- `ConvertRgbToHex(CFColorRgb)`
- `ConvertHsvToHex(CFColorHsv)`

Value types:

- `CFColorRgb(byte Red, byte Green, byte Blue, byte Alpha)`
- `CFColorHsv(double Hue, double Saturation, double Value, byte Alpha)`

## Mixed State

Host-controlled. While mixed the hex field is hidden and the picker starts from opaque black
(`#FF000000`). The control only propagates a value and clears `IsMixedState` if the user picks a color
different from that default black — so simply opening and closing the picker on the default does not
resolve the mixed state.

## Example

```xaml
<colorControls:CFColor
    Value="{Binding Fill, Mode=TwoWay}"
    IsMixedState="{Binding FillIsMixed, Mode=TwoWay}" />
```
