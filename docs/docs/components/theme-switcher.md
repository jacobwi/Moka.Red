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
| `Themes` | `IReadOnlyList<MokaThemeOption>` | `[]` | Available themes |
| `SelectedTheme` | `string?` | -- | Currently selected theme name (two-way bindable) |
| `SelectedThemeChanged` | `EventCallback<string?>` | -- | Callback when selection changes |
| `ShowPreview` | `bool` | `true` | Show color preview swatches next to theme names |
| `Size` | `MokaSize` | `Md` | Button size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaThemeOption

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name |
| `Value` | `string` | Unique value key |
| `PrimaryColor` | `string?` | Primary color hex for the preview swatch |

## Basic with Themes

```blazor-preview
<MokaThemeSwitcher Themes="@_themes" SelectedTheme="light" />

@code {
    private IReadOnlyList<MokaThemeOption> _themes = new[]
    {
        new MokaThemeOption { Name = "Light", Value = "light", PrimaryColor = "#ffffff" },
        new MokaThemeOption { Name = "Dark", Value = "dark", PrimaryColor = "#060608" },
        new MokaThemeOption { Name = "Ocean", Value = "ocean", PrimaryColor = "#0277bd" }
    };
}
```

## Without Preview

```blazor-preview
<MokaThemeSwitcher Themes="@_themes" SelectedTheme="dark" ShowPreview="false" />

@code {
    private IReadOnlyList<MokaThemeOption> _themes = new[]
    {
        new MokaThemeOption { Name = "Light", Value = "light" },
        new MokaThemeOption { Name = "Dark", Value = "dark" },
        new MokaThemeOption { Name = "Ocean", Value = "ocean" }
    };
}
```
