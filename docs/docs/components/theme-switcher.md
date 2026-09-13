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
| `ShowPreview` | `bool` | `true` | Show primary, secondary, success and error swatches for each theme |
| `Size` | `MokaSize` | `Md` | Button size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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
