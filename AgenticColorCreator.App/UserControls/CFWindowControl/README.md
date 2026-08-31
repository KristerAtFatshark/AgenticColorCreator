# CFWindow

`AgenticColorCreator.App.UserControls.CFWindowControl.CFWindow`

## Overview

A WPF `Window` subclass with fully themed client-drawn chrome. It replaces the standard title bar with
the `CF.CustomWindow` template while retaining window dragging, native edge resizing, caption commands,
multi-monitor maximization, and DPI-aware minimum sizing.

## Files

- `CFWindow.cs` - the window subclass and its dependency properties.
- `CFWindowExtensions.cs` - routed caption-command setup and the native maximize/work-area hook.
- `CFDarkStyles.xaml` - contains `CF.CustomWindow`, `CF.WindowCaptionButton`, and the window color
  resources.
- `EditorIconGlyphs.xaml` - supplies the title-bar and caption-button glyph strings. This is vendored
  generated content and must not be edited in this repository.

## Dependency Properties

- `TitleBarIconGlyph` (`string`, default `null`) - optional icon-font glyph rendered at the far left of
  the title bar. Set it from an `EditorIconGlyphs.xaml` resource such as
  `{StaticResource icon-app_level_editor_icon}`. The glyph element is collapsed when the value is null
  or empty.
- `OwnerWindow` (`Window`, default `null`) - optional owner input usable from code or XAML binding. A
  null value resolves to `Application.Current.MainWindow` during initialization. `CFWindow` avoids
  assigning itself as its own owner, which allows the application main window to use the class.

The inherited WPF `Window.Icon` property remains available for an optional image icon. When both
`TitleBarIconGlyph` and `Icon` are set, the glyph appears first.

## Title Bar

The `CF.CustomWindow` template contains:

- Optional glyph icon, optional image icon, and `Window.Title` on the left.
- Minimize, maximize/restore, and close buttons on the right.
- `icon-minimize_window`, `icon-maximize_window`, `icon-restore_window`, and `icon-close_window` from
  `EditorIconGlyphs.xaml`.
- Automatic maximize/restore visibility switching based on `WindowState`.
- Caption-button availability based on `ResizeMode`.

The template uses WPF `WindowChrome` with a 36-pixel caption region and a 6-pixel resize border.
Caption buttons set `WindowChrome.IsHitTestVisibleInChrome` so they receive clicks instead of dragging
the window.

## Window Setup

The `CFWindow` constructor calls `ConfigureCFWindowBehavior()` automatically. The public extension is
idempotent, so calling it again does not add duplicate hooks or command bindings.

`ConfigureCFWindowBehavior()`:

- Registers handlers for `SystemCommands.CloseWindowCommand`, `MinimizeWindowCommand`,
  `MaximizeWindowCommand`, and `RestoreWindowCommand`.
- Installs an `HwndSource` hook when the native window source is initialized.
- Handles `WM_GETMINMAXINFO` so maximized windows use the nearest monitor's work area rather than
  covering its taskbar or using primary-monitor bounds.
- Converts `MinWidth` and `MinHeight` to device pixels before supplying native minimum tracking sizes.
- Removes the native hook and setup tracking when the window closes.

### What The Native Hook Does

An `HwndSource` hook is a callback inserted into the native Windows message loop for this window. It
does not continuously resize or poll the window. It waits for Windows to send `WM_GETMINMAXINFO`, which
happens when Windows needs the allowed minimum, maximum, and maximized bounds before a move, resize, or
maximize operation.

When that message arrives, the hook finds the monitor nearest the window, reads that monitor's work
area (the usable area excluding the taskbar), and writes the correct maximum position, maximum size,
and DPI-scaled minimum size into the message's `MINMAXINFO` structure. The message is then marked as
handled so Windows uses those corrected values. This is needed because custom WPF chrome can otherwise
maximize using primary-monitor or full-monitor bounds, causing the window to extend behind the taskbar
or fit incorrectly on a secondary monitor.

## Theming

The title bar uses the Window entries from `Color/agentic_colors.md` through these resources:

- `CF.Window.Default.Background`
- `CF.Window.Default.Foreground`
- `CF.Window.Default.Icon`
- `CF.Window.MouseOver.Icon`

Caption glyphs use `CF.IconTextBlock` and the embedded `fs-editor-icons` font.

## XAML Example

```xaml
<windowControls:CFWindow x:Class="Example.EditorWindow"
		xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
		xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
		xmlns:windowControls="clr-namespace:AgenticColorCreator.App.UserControls.CFWindowControl"
		Title="Editor"
		TitleBarIconGlyph="{StaticResource icon-app_level_editor_icon}"
		OwnerWindow="{Binding EditorOwner}"
		Style="{StaticResource CF.CustomWindow}"
		Width="800"
		Height="600">
	<Grid>
		<!-- Window content -->
	</Grid>
</windowControls:CFWindow>
```

Omit `OwnerWindow` to use `Application.Current.MainWindow`. Omit `TitleBarIconGlyph` to hide the
font-glyph icon.

## Code Example

```csharp
var window = new EditorWindow
{
	OwnerWindow = this,
	TitleBarIconGlyph = (string)FindResource("icon-app_level_editor_icon"),
};

window.ShowDialog();
```

Derived windows should inherit from `CFWindow` in both XAML and code-behind and apply
`Style="{StaticResource CF.CustomWindow}"`.
