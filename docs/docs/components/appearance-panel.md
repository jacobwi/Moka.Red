---
title: Appearance Panel
description: A ready-made settings panel where the person using an app picks how it looks.
order: 156
---

# Appearance Panel

`MokaAppearancePanel` is the settings screen for how an app looks, in up to three groups. Colors holds color mode and palette as picture cards and accent as swatches. Style holds the status bar as picture cards, then rows for density and the other choices. Components, when you ask for it, holds the looks of toasts, notifications, popups, lists, dialogs and buttons. Every pick applies at once, with no Save button.

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
| `Accents` | `IReadOnlyList<MokaAccent>` | `MokaAccent.Presets` | The accents to pick from. See [Your own accents](#your-own-accents) |
| `OptionState` | `Func<MokaAppearanceOption, MokaAppearanceOptionState>?` | - | Hides or greys out single options. See [Hiding or disabling an option](#hiding-or-disabling-an-option) |
| `Text` | `MokaAppearanceText` | `new()` | Every word it writes. See [Words](#words) |
| `ChildContent` | `RenderFragment?` | - | Your own rows, after the built-in ones, in the last group shown |
| `ModeLook` | `MokaThemeToggleLook?` | `null` | Draws the color mode row as a `MokaThemeToggle` instead of cards. See [The color mode row](#the-color-mode-row) |
| `Compact` | `bool` | `false` | The form for a popover or a side sheet. See [Compact](#compact) |
| `ShowReset` | `bool` | `true` | Whether a button puts every choice back while any differs from `Defaults` |
| `Defaults` | `MokaAppearance?` | `null` | What that button goes back to. Null is a new `MokaAppearance` |
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
| `Density` | `MokaDensity?` | `null` | Comfortable or compact, every value of `MokaDensity` |
| `TextSize` | `MokaTextSize?` | `null` | Normal or small |
| `Corners` | `MokaCorners?` | `null` | Soft, medium or tight |
| `MainButton` | `MokaMainButton?` | `null` | Solid, tinted or outline |
| `LetterCase` | `MokaLetterCase?` | `null` | Sentence or uppercase |
| `TitleFont` | `MokaTitleFont?` | `null` | Space Grotesk, Inter or JetBrains Mono |
| `Selection` | `MokaSelection?` | `null` | Ring, flat or glow |
| `CurrentPlace` | `MokaCurrentPlace?` | `null` | Ring or solid |
| `StatusBar` | `MokaStatusBarLook?` | `null` | How status bars are drawn |
| `Descriptions` | `bool?` | `null` | Whether text saying what a thing is for shows |
| `Toasts` | `MokaToastLook?` | `null` | How toasts are drawn |
| `ToastStacking` | `MokaToastStacking?` | `null` | List or pile |
| `Notifications` | `MokaNotificationLook?` | `null` | How notification lists are drawn |
| `Popups` | `MokaPopupLook?` | `null` | How menus, popovers, hover cards and other floating panels are drawn |
| `Lists` | `MokaListLook?` | `null` | How the rows of lists that open from a field are drawn |
| `Dialogs` | `MokaDialogLook?` | `null` | How dialogs are drawn |
| `Buttons` | `MokaButtonLook?` | `null` | How buttons are drawn |

| Method | Returns | Description |
|--------|---------|-------------|
| `ToTheme(isDark, palettes, accents)` | `MokaTheme` | The theme for a known scheme |
| `ToThemes(palettes, accents)` | `MokaAppearanceThemes` | `Theme`, `DarkTheme` and `LightTheme` for `MokaThemeProvider` |
| `WithComponentStyle(style)` | `MokaAppearance` | This value with toasts, notifications, popups, lists, dialogs and buttons set the way `MokaTheme.WithComponentStyle` sets them. The toast stack stays as it is |

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

`Rows` is a set of flags. The default is `ColorMode`, `Palette`, `Accent`, `StatusBar`, `Density` and `TextSize`.

```razor
<MokaAppearancePanel @bind-Value="_appearance"
                     Rows="MokaAppearanceRows.Default | MokaAppearanceRows.Corners | MokaAppearanceRows.StatusBar"/>
```

`MokaAppearanceRows.All` shows everything, and `MokaAppearanceRows.Style` is every style row. `MokaAppearanceRows.Components` is the seven component look rows and `MokaAppearanceRows.ComponentStyle` the row that sets six of them at once (see [Component looks](#component-looks)). Neither is in `Default` or `Style`, so a panel that asks for those looks as it did. To leave the status bar out of the default set, use `MokaAppearanceRows.Default & ~MokaAppearanceRows.StatusBar`.

The status bar row shows each look on a real `MokaStatusBar`. Density, corners and main button draw each option in the theme it would give, since a word alone doesn't show the difference. The headings appear when more than one group has something in them.

A row with nothing to choose hides itself: the palette row with one palette, the accent row with no accents, and any row left with only the option already picked.

## Component looks

Seven rows pick how components are drawn, under a Components heading after the style rows. Each sets one of the theme's component looks, the same ones `MokaTheme.WithToasts` and the other methods in [Theming](../getting-started/theming.md#style) set.

| Row | Flag | Value | Looks |
|-----|------|-------|-------|
| Toasts | `Toasts` | `MokaToastLook` | Standard, Tone edge, Tinted, Icon tile, Snackbar, Pill, Terminal |
| Toast stack | `ToastStacking` | `MokaToastStacking` | List, Pile |
| Notifications | `Notifications` | `MokaNotificationLook` | Standard, Inbox, Cards, Compact, Grouped, Timeline, Terminal |
| Popups | `Popups` | `MokaPopupLook` | Standard, Flat, Elevated, Glass, Accent tint, Contrast, Terminal |
| Lists | `Lists` | `MokaListLook` | Standard, Check, Solid, Attached, Radio, Compact, Terminal |
| Dialogs | `Dialogs` | `MokaDialogLook` | Standard, Bands, Icon lead, Centered, Terminal |
| Buttons | `Buttons` | `MokaButtonLook` | Standard, Raised, Soft, Pill, Glow, Offset, Gradient, Ghost |

Each look is a small picture card drawn in the theme around the panel: a toast, a notification list, a menu, a select list, a dialog or a row of buttons. The popup pictures carry the same `moka-popups-*` classes the real panels read, so they show what a menu will look like. With `Compact` the rows are plain segments.

The Component style row sets toasts, notifications, popups, lists, dialogs and buttons in one pick, through `MokaTheme.WithComponentStyle`: Standard, Soft or Terminal. It writes those six fields into the value, so the rows below it show what it picked and can still change any one of them. Its card is current while the six match a style, and none is once someone fine-tunes one away from it. It leaves the toast stack alone.

```razor
<MokaAppearancePanel @bind-Value="_appearance"
                     Rows="MokaAppearanceRows.Default | MokaAppearanceRows.ComponentStyle | MokaAppearanceRows.Components"/>
```

For only the style row, without the seven rows under it:

```razor
<MokaAppearancePanel @bind-Value="_appearance"
                     Rows="MokaAppearanceRows.Default | MokaAppearanceRows.ComponentStyle"/>
```

To start someone on a style from code, use `MokaAppearance.WithComponentStyle`:

```razor
@code {
    private MokaAppearance _appearance = new MokaAppearance().WithComponentStyle(MokaComponentStyle.Soft);
}
```

A component with a `Look` parameter of its own keeps it, whatever the panel picked.

## Hiding or disabling an option

`Rows` takes out whole rows. `OptionState` works on single options, the same way for all of them. It's asked about each option and answers `Available`, `Disabled` (shown greyed out) or `Hidden`. Match on what you care about and leave the rest.

```razor
<MokaAppearancePanel @bind-Value="_appearance" OptionState="StateOf"/>

@code {
    private static MokaAppearanceOptionState StateOf(MokaAppearanceOption option) => option switch
    {
        { Value: MokaColorMode.System } => MokaAppearanceOptionState.Hidden,
        { Value: MokaCorners.Tight } => MokaAppearanceOptionState.Disabled,
        { Row: MokaAppearanceRows.Palette, Value: "original" } => MokaAppearanceOptionState.Disabled,
        _ => MokaAppearanceOptionState.Available
    };
}
```

`MokaAppearanceOption` has two properties:

| Name | Type | Description |
|------|------|-------------|
| `Row` | `MokaAppearanceRows` | The row it's in |
| `Value` | `object` | A `MokaColorMode`, a `MokaDensity` or the matching enum of another style or component look row (`MokaComponentStyle` in the component style row), a palette's or accent's `Id`, or a `bool` in the descriptions row |

Hiding an option changes only what can be picked. A stored value that holds it keeps it, and the row shows nothing as picked until the person picks something else. If that value can't apply in your app, change it where you load it.

## The color mode row

Cards are the default. `ModeLook` puts a `MokaThemeToggle` there in any of its looks, which takes less room.

```razor
<MokaAppearancePanel @bind-Value="_appearance" ModeLook="MokaThemeToggleLook.IconsAndWords"/>
```

If `OptionState` hides `System` the toggle has two states. If it hides light or dark, or disables any mode, the row stays cards, because a toggle has no way to show that.

## Compact

For a popover or a side sheet. It drops the headings, makes the cards smaller, draws the color mode as a toggle, and uses plain segments on the style rows, the status bar and the component look rows.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Compact/>
```

## Going back to the defaults

A button at the end appears once any choice differs from the defaults. If your app starts somewhere other than `new MokaAppearance()`, say so, and set `ShowReset="false"` to leave the button out.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Defaults="new() { Mode = MokaColorMode.System }"/>
```

## Style on the original design

`MokaTheme.Dark` and `MokaTheme.Light` are the original design, and it ignores density, text size, every other style choice and the component looks. While a palette built on it is selected, those rows hide and one line says why. An accent still works on it.

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

## Your own accents

`Accents` takes any color. `MokaAccent.FromColor` works out shades that keep their contrast in both schemes, and `with` gives it the id a stored value will hold and the name on its swatch.

```razor
<MokaAppearancePanel @bind-Value="_appearance" Accents="Brand"/>

@code {
    private static readonly IReadOnlyList<MokaAccent> Brand =
    [
        .. MokaAccent.Presets,
        MokaAccent.FromColor("#12ab34") with { Id = "brand", Name = "Brand green" }
    ];
}
```

Pass the same list to `ToThemes`. To let someone pick a color of their own, put a picker in one of [your own rows](#your-own-rows) and add its color to the list.

The density row offers every value of `MokaDensity`. `MokaTheme.WithDensity(double)` also scales the spacing by a number, which a `MokaAppearance` doesn't hold. Apply it to the themes `ToThemes` gives you, or put it in a palette.

## Your own rows

`ChildContent` goes after the built-in rows, in the last group: under Style, or under Components while the component rows show. `MokaAppearanceRow` matches them and stacks with them when the panel is narrow.

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
