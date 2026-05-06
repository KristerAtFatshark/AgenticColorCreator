# colors.md

## Purpose

This file explains how an agent should use `Color/agentic_colors.md` to apply the correct colors to a UI.

## Primary Rule

Always treat `Color/agentic_colors.md` as the source of truth for UI colors.

## How To Find A Color

1. Start with the top-level category.
2. Then check the interaction-state subcategory.
3. Then use the color name and description to choose the best match for the specific UI part.

## Category First

Use the category name as the first filter.

Examples:
- window backgrounds: `Window`
- panel/card/container backgrounds: `Panel`
- layout surface backgrounds: `Grid`
- text and foreground colors: `Text`
- input field surfaces: `Text Box`
- outlines and separators: `Border`
- button visuals: `Button`

## Interaction State Second

Each color belongs to one of these fixed interaction states:
- `Default`
- `Selected`
- `Pressed`
- `Hovered`
- `Disabled`

After finding the correct category, choose the color from the matching state.

`Hovered` means the control is currently under the mouse pointer right now.
It is the same as `Mouse Over`.
It does not mean a control that was hovered earlier or after the hover has ended.
Whenever UI code uses a mouse-over visual state, map it to `Hovered`.

## Fallback Rule

If a UI element needs a state-specific color and that state is not defined in `agentic_colors.md`, use the `Default` color from the same category and same semantic color name.

Examples:
- if a button needs a `Pressed` border color but only `Button Border Default` exists, use `Button Border Default`
- if text needs a `Hovered` color but only `Text Color Default` exists, use `Text Color Default`
- if an input field uses a focused or active border treatment but there is no dedicated `Pressed` or `Selected` border color, use the same category's `Default` border color unless a better explicit state exists

## Name And Description Matching

Within the correct category and state:

1. Prefer exact name matches.
2. If there is no exact match, use the description to find the closest semantic use.
3. Do not choose a color from another category unless the current category has no reasonable match.

## Practical Selection Order

Use this order when selecting a color:

1. Match category.
2. Match interaction state.
3. Match color name.
4. Match description.
5. Fall back to `Default` in the same category if the requested state does not exist.

When `agentic_colors.md` changes, reevaluate both changed colors and newly added colors.
Newly added colors may provide a more specific semantic match than the previously used mapping and should replace the older mapping when appropriate.
If new top-level categories are introduced, reevaluate any older mappings that previously relied on a broader category because the new category may now be the correct semantic target.

## Examples

Example: button background while hovered

1. Category: `Button`
2. State: `Hovered`
3. Best name match: `Button Background`
4. Result: `Button Background Hovered`

Example: combo box dropdown row while the mouse is currently over it

1. Category: `ComboBox Item`
2. State: `Hovered`
3. Use the hovered row background, text, and border colors
4. Result: `ComboBox Item / Background Hovered`, `ComboBox Item / Text Hovered`, and `ComboBox Item / Border Hovered`

Example: text input field background while disabled, but no disabled entry exists

1. Category: `Text Box`
2. State requested: `Disabled`
3. No disabled match exists
4. Fall back to `Default`
5. Result: use the default text field background color from that category

## Control Versus Popup Parts

For controls with popup content, map the closed control and the popup items separately.

Examples:
- closed selected surface of a combo box: use `ComboBox`
- dropdown rows inside the combo box popup: use `ComboBox Item`
- app root background behind the main layout: use `Application`
- top-level window surface: use `Window`
- grouped content containers and cards: use `Panel`
- layout regions that act as generic content surfaces: use `Grid`

If a category used by the current UI mapping is removed from `agentic_colors.md`, reevaluate the mapping immediately and remap the UI part to the next most specific remaining category.

This means the visible selected item area of a closed combo box should not use `ComboBox Item` colors. Those are only for the popup list entries.

## Focused Inputs

For text-entry controls, focused or active editing visuals should still follow the same state lookup logic.

Examples:
- hover border on a text box: use `Text Box / Border Hovered` if present
- focused or active text box border with no dedicated focused/pressed entry: fall back to `Text Box / Border Default`

## App-Level Theming Rule

If the application already has a dedicated category for a control type, prefer that control category over older generic hardcoded theme colors.

Examples:
- use `ComboBox` colors for the combo box control shell, text, border, and icon
- use `ComboBox Item` colors for popup rows
- use `Text Box` colors for text input controls
- use `Button` colors for button surfaces and text

## Agent Guidance

- Do not invent new color meanings if an existing category, state, name, or description already fits.
- When updating UI code, preserve the semantic mapping between control purpose and color choice.
- If `agentic_colors.md` is expanded later, prefer the newer explicit entry over any earlier fallback.
- If the color-file timestamp changed, do not only compare old mapped values. Also inspect newly added entries because they may introduce a better semantic match for an existing UI part.
- If new top-level categories appear, prefer the newer, more specific category over an older broad category that was only used as an approximation.
- If a previously used category is removed, replace that mapping with the most specific remaining category instead of keeping a stale category assumption.
