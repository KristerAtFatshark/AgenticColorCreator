# CFHdrColor

`AgenticColorCreator.App.UserControls.CFHdrColorControl.CFHdrColor`

## Overview

An HDR-aware variant of `CFColor`. It keeps an SDR hex `Value` plus a separate `Stops` exposure
multiplier and uses HDR/SDR conversion helpers to preview and edit the HDR-adjusted output. It shows
the same swatch + hex field, and adds a stops input.

## Files

- `CFHdrColor.xaml` / `CFHdrColor.xaml.cs` — the control.
- `CFHdrColorValueTypes.cs` — `CFHdrColorRgb` and `CFHdrColorHsv` value types.
- `CFHdrColorConverters.cs` — XAML converters wrapping the static conversion helpers.
- `ColorOperations.cs` — HDR/SDR color conversion (exposure stops applied to a WPF color).

## Dependency Properties

- `Value` (`string`, two-way) — SDR hex color (`#AARRGGBB`, default `#FF000000`).
- `Stops` (`double`, two-way, default `0`) — HDR exposure stops applied on top of the SDR color.
- `IsMixedState` (`bool`, default `false`) — hides both the hex field and the stops field behind a
  `- Mixed -` overlay.

## Events

- `ValueChanged` (routed, bubbling) — raised when either `Value` or `Stops` changes.

## Conversion Helpers

Static methods (also available to bindings via `CFHdrColorConverters.cs`):

- `TryConvertHexToRgb(hex, stops, out CFHdrColorRgb)`
- `TryConvertHexToHsv(hex, stops, out CFHdrColorHsv)`
- `ConvertRgbToHex(CFHdrColorRgb)`
- `ConvertHsvToHex(CFHdrColorHsv)`

Value types:

- `CFHdrColorRgb(byte Red, byte Green, byte Blue, byte Alpha, double Stops)`
- `CFHdrColorHsv(double Hue, double Saturation, double Value, byte Alpha, double Stops)`

## Mixed State

Host-controlled, and mirrors `CFColor`. While mixed both the hex and stops fields are hidden, and the
picker starts from opaque black (`#FF000000`) with `0` stops. The control only commits a value and
clears `IsMixedState` if the user picks something different from that default.

## Example

```xaml
<hdrColorControls:CFHdrColor
    Value="{Binding Emissive, Mode=TwoWay}"
    Stops="{Binding EmissiveStops, Mode=TwoWay}"
    IsMixedState="{Binding EmissiveIsMixed, Mode=TwoWay}" />
```
