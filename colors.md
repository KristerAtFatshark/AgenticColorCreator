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
- window and panel backgrounds: `Surface`
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

## Examples

Example: button background while hovered

1. Category: `Button`
2. State: `Hovered`
3. Best name match: `Button Background`
4. Result: `Button Background Hovered`

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
