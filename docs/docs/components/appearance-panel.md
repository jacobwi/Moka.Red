---
title: Appearance Panel
description: A ready-made settings panel where the person using an app picks how it looks.
order: 156
---

# Appearance Panel

`MokaAppearancePanel` is the settings screen for how an app looks: color mode and palette as picture cards, accent as swatches, then plain rows for density and the other style choices. Every pick applies at once, with no Save button.

It behaves like an input. It takes a `MokaAppearance` and gives one back, and storing it is your app's job, per device or per account.

## Setup

```razor
@using Moka.Red.ThemeGen.Appearance
```

## Use it

Hold the value and the themes it makes. Work the themes out when the value changes, not while rendering: a new theme redraws everything under its provider.

```razor
<MokaThemeProvider AutoDetectColorScheme
                   Theme="@_themes.Theme"
                   DarkTheme="@_themes.DarkTheme"
                   LightTheme="@_themes.LightTheme">
    <MokaAppearancePanel Value="_appearance" ValueChanged="SetAppearance"/>
</MokaThemeProvider>

@code {
    private MokaAppearance _appearance = new();
    private MokaAppearanceThemes _themes = new MokaAppearance().ToThemes();

    private async Task SetAppearance(MokaAppearance appearance)
    {
        _appearance = appearance;
        _themes = appearance.ToThemes();
        await Storage.SaveAsync(appearance);
    }
}
```

Keep `AutoDetectColorScheme` on for every mode. The provider starts watching the device on its first render only, so turning it on when someone picks System would do nothing. For Light and Dark, `ToThemes` hands over the same theme for both, so the device has no say.

For a scheme you already know, `ToTheme(isDark)` gives one theme.

## Parameters

### MokaAppearancePanel

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `MokaAppearance` | `new()` | The current choices. Two-way bindable |
| `ValueChanged` | `EventCallback<MokaAppearance>` | - | Raised with the new value on every pick |
| `Rows` | `MokaAppearanceRows` | `Default` | Which rows show. See [Rows](#rows) |
| `Palettes` | `IReadOnlyList<MokaAppearancePalette>` | `Presets` | The palettes to pick from. See [Your own palettes](#your-own-palettes) |
| `Accents` | `IReadOnlyList<MokaAccent>` | `MokaAccent.Presets` | The accents to pick from |
| `Text` | `MokaAppearanceText` | `new()` | Every word it writes. See [Words](#words) |
| `ChildContent` | `RenderFragment?` | - | Your own rows, after the built-in ones |
| `Nonce` | `string?` | - | A content security policy nonce for the previews. Pass the one you give your own provider |
| `Size` | `MokaSize` | `Sm` | The size of the segmented controls |
| `Disabled` | `bool` | `false` | Makes the whole panel inert, your own rows included |

### MokaAppearanceRow

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Label` | `string?` | - | The row's label |
| `Hint` | `string?` | - | A line under the label |
| `ChildContent` | `RenderFragment?` | - | The control. Name it too, since a screen reader doesn't hear the label from it |

### MokaAppearance

A record of ids and plain choices, so it serializes as it is. A choice left null keeps what the palette has, so a stored value holds only what the person changed.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Mode` | `MokaColorMode` | `Dark` | `Light`, `Dark` or `System` |
| `Palette` | `string?` | `null` | A palette's `Id`. Null, or an id no longer offered, is the first palette |
| `Accent` | `string?` | `null` | An accent's `Id`. Null, or an id no longer offered, is the palette's own |
| `Density` | `MokaDensity?` | `null` | Comfortable or compact |
| `TextSize` | `MokaTextSize?` | `null` | Normal or small |
| `Corners` | `MokaCorners?` | `null` | Soft, medium or tight |
| `MainButton` | `MokaMainButton?` | `null` | Solid, tinted or outline |
| `LetterCase` | `MokaLetterCase?` | `null` | Sentence or uppercase |
| `TitleFont` | `MokaTitleFont?` | `null` | Space Grotesk, Inter or JetBrains Mono |
| `Selection` | `MokaSelection?` | `null` | Ring, flat or glow |
| `CurrentPlace` | `MokaCurrentPlace?` | `null` | Ring or solid |
| `StatusBar` | `MokaStatusBarLook?` | `null` | How status bars are drawn |
| `Descriptions` | `bool?` | `null` | Whether text saying what a thing is for shows |

| Method | Returns | Description |
|--------|---------|-------------|
| `ToTheme(isDark, palettes, accents)` | `MokaTheme` | The theme for a known scheme |
| `ToThemes(palettes, accents)` | `MokaAppearanceThemes` | `Theme`, `DarkTheme` and `LightTheme` for `MokaThemeProvider` |

If you passed your own `Palettes` or `Accents` to the panel, pass the same lists to these.

### MokaAppearancePalette

| Name | Type | Description |
|------|------|-------------|
| `Id` | `string` | What a stored value holds, so keep it stable |
| `Name` | `string` | The name on the card |
| `Dark` | `MokaTheme` | The theme in dark mode |
| `Light` | `MokaTheme` | The theme in light mode |
| `Hint` | `string?` | A line under the name |
| `TakesStyleChoices` | `bool` | Whether density and the other style choices do anything on it |

`MokaAppearancePalette.Presets` is `Red`, `Gate`, `Matrix` and `Original`. `Crisp` exists too but isn't in the list, since as a picture it looks just like Gate.

## Rows

`Rows` is a set of flags. The default is `ColorMode`, `Palette`, `Accent`, `Density` and `TextSize`.

```razor
<MokaAppearancePanel @bind-Value="_appearance"
                     Rows="MokaAppearanceRows.Default | MokaAppearanceRows.Corners | MokaAppearanceRows.StatusBar"/>
```

`MokaAppearanceRows.All` shows everything, and `MokaAppearanceRows.Style` is every row from density down.

A row with nothing to choose hides itself: the palette row with one palette, the accent row with no accents.

## Style on the original design

`MokaTheme.Dark` and `MokaTheme.Light` are the original design, and it ignores density, text size and every other style choice. While a palette built on it is selected, those rows hide and one line says why. An accent still works on it.

The person's choices stay in the value, so they come back with another palette.

## Your own palettes

Each palette is a name with a dark theme and a light theme. Anything `MokaTheme` can do goes in.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Palettes="Brand"/>

@code {
    private static readonly IReadOnlyList<MokaAppearancePalette> Brand =
    [
        new("sea", "Sea", MokaTheme.Gate.WithAccent(MokaAccent.Cyan), MokaTheme.GateLight.WithAccent(MokaAccent.Cyan))
        {
            Hint = "Blue-grey with a cyan accent"
        },
        MokaAppearancePalette.Red
    ];
}
```

The panel shows each palette in a small window drawn with that palette's own theme.

## Your own rows

`ChildContent` goes after the built-in rows. `MokaAppearanceRow` matches them and stacks with them when the panel is narrow.

```razor
<MokaAppearancePanel @bind-Value="_appearance">
    <MokaAppearanceRow Label="Side menu" Hint="Shows only icons on a wide window">
        <MokaSwitch @bind-Value="_narrowMenu" aria-label="Side menu"/>
    </MokaAppearanceRow>
</MokaAppearancePanel>
```

## Words

Every word comes from `MokaAppearanceText`. Set the ones to change.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Text="Swedish"/>

@code {
    private static readonly MokaAppearanceText Swedish = new()
    {
        ColorMode = "Färgläge",
        ColorModeLight = "Ljust",
        ColorModeDark = "Mörkt"
    };
}
```

The names of palettes and accents are on those records, so rename them in the lists you pass.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Palettes="Named"/>

@code {
    private static readonly IReadOnlyList<MokaAppearancePalette> Named =
    [
        MokaAppearancePalette.Red with { Name = "Röd" },
        MokaAppearancePalette.Gate with { Name = "Grind" }
    ];
}
```

## Narrow spaces

The cards wrap to fit, narrower ones below 30rem of the panel's own width so a phone gets two across. Below that same width each row puts its control under its label, so it works in a side panel or on a phone without a media query.
