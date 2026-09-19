---
title: Theme Switcher
description: Dropdown picker for switching between multiple application themes.
order: 78
---

# Theme Switcher

`MokaThemeSwitcher` renders a dropdown button that lets users pick from a list of named themes. Optionally shows a color preview swatch next to each theme name.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Themes` | `IReadOnlyList<MokaThemeSwitcherItem>` | `[]` | Available themes (required) |
| `SelectedTheme` | `MokaTheme?` | -- | Currently selected theme (two-way bindable) |
| `SelectedThemeChanged` | `EventCallback<MokaTheme?>` | -- | Callback when selection changes |
| `ShowPreview` | `bool` | `true` | Show primary, secondary, success and error swatches for each theme. A theme value that is not a CSS color gets an empty swatch |
| `Size` | `MokaSize` | `Md` | Button size |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the button. Replaces the padding `Size` gives it |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the button |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the switcher |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The padding and radius go on the button, the box the switcher draws, and the margin, `Class` and `Style` on the element around it. Up to 0.1.12 the switcher ignored all three.

### MokaThemeSwitcherItem

A positional record: `new MokaThemeSwitcherItem(name, theme, description)`.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name |
| `Theme` | `MokaTheme` | Theme reported through `SelectedThemeChanged` when the item is picked |
| `Description` | `string?` | Optional second line under the name |

`MokaTheme` is a record, so the selected item is found by value. `MokaTheme.Dark` returns a new instance on every call and still matches an item built from `MokaTheme.Dark`.

The switcher only reports the choice. Apply it by passing the bound theme to your `MokaThemeProvider`.

## Basic with Themes

```blazor-preview
<MokaThemeSwitcher Themes="_themes" @bind-SelectedTheme="_selected" />

@code {
    private static readonly MokaThemeSwitcherItem[] _themes =
    [
        new("Light", MokaTheme.Light, "Default light palette"),
        new("Dark", MokaTheme.Dark, "Near-black with a red accent"),
        new("Ocean", MokaTheme.Light.WithPrimary("#0277bd"), "Light with a blue primary")
    ];

    private MokaTheme? _selected = MokaTheme.Light;
}
```

## Without Preview

```blazor-preview
<MokaThemeSwitcher Themes="_themes" @bind-SelectedTheme="_selected" ShowPreview="false" />

@code {
    private static readonly MokaThemeSwitcherItem[] _themes =
    [
        new("Light", MokaTheme.Light),
        new("Dark", MokaTheme.Dark),
        new("Ocean", MokaTheme.Light.WithPrimary("#0277bd"))
    ];

    private MokaTheme? _selected = MokaTheme.Dark;
}
```

## Keyboard

The switcher follows the WAI-ARIA menu button pattern. The button opens a menu of the themes with one of them checked, and focus moves into the menu onto the checked theme.

| Key | Action |
|-----|--------|
| Enter / Space / Down / Up | On the button: open the menu |
| Down / Up | Move to the next or previous theme, wrapping at the ends |
| Home / End | Move to the first or last theme |
| Enter / Space | Pick the focused theme and close the menu |
| Escape | Close the menu without picking |
| Tab | Close the menu and move on |

Picking a theme, with the keys or the mouse, returns focus to the button. Picking the theme that is already checked closes the menu without raising `SelectedThemeChanged`.

A pick sticks even when `SelectedTheme` isn't bound, and a re-render of the parent doesn't undo it. The switcher follows `SelectedTheme` again when the parent passes a different theme. Use `@bind-SelectedTheme` to keep both in step.

Up to 0.1.12 the themes in the open list could only be picked with the mouse.

## Accessibility

- The button has `aria-haspopup="menu"`, reports `aria-expanded` as `"true"` or `"false"`, and points at the open menu with `aria-controls`. Its name says what it is for and which theme is picked, for example "Theme: Dark".
- The menu has `role="menu"` and takes its name from the button. Each theme is a `menuitemradio` named by `Name`, with `aria-checked` as `"true"` or `"false"`. `Description` is read after the name.
- The button is `type="button"`, so it no longer submits a surrounding form.
