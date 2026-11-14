---
title: Color Input
description: Simple hex color input with preview swatch.
order: 53
---

# Color Input

`MokaColorInput` provides a text field for entering hex color values, paired with a small preview swatch. Optionally exposes the browser's native color picker for quick selection.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | `""` | Current hex color value (bindable via `@bind-Value`) |
| `ValueChanged` | `EventCallback<string>` | -- | Fires when the value changes |
| `Label` | `string?` | `null` | Field label |
| `HelperText` | `string?` | `null` | Helper text below the input |
| `Actions` | `RenderFragment?` | - | Content beside the box, in its row, such as a button. See [A button beside the box](/components/forms#a-button-beside-the-box) |
| `Placeholder` | `string` | `"#000000"` | Placeholder text |
| `ShowPreview` | `bool` | `true` | Show a color swatch preview next to the input |
| `ShowNativeInput` | `bool` | `true` | Show the browser's native `<input type="color">` picker button |
| `Size` | `MokaSize` | `Md` | Input size: `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables the input |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |
| `Look` | `MokaColorInputLook` | `Standard` | How the field is drawn: `Standard`, `SwatchButton`, `Split`, `Chip`, `Palette`, `Terminal`. See [Looks](#looks) |
| `PickerLayout` | `MokaColorPickerLayout` | `Compact` | The panel the popup opens (every look but `Standard` and `Terminal`) |
| `Presets` | `IReadOnlyList<string>?` | eight colors | The swatches `Palette` shows, as hex, and the palette of a panel that has one |
| `ColorLabel` | `Func<string, string?>?` | `null` | Names a preset swatch. Without it the eight defaults are named Red to Gray, and others by their value |
| `PickerLabel` | `string` | `"Choose a color"` | The popup's name |
| `CustomText` | `string` | `"Custom"` | What `Palette`'s Custom button says |
| `InvalidText` | `string` | `"Six hex digits"` | The error when typed text is no color, in every look but `Standard` |
| `ShowContrast`, `ContrastBackground`, `ShowFormats`, `Format`, `FormatChanged`, `ShowThemeColors`, `ShowAlpha`, `ShowAlphaPreview`, `LabelledEyeDropper`, `ClearableRecentColors` | | | Passed to the popup's panel. See `MokaColorPicker` in [Forms](forms.md) |
| `RecentColors`, `RecentColorsChanged` | `IReadOnlyList<string>?` | `null` | The recent colors, newest first. The field keeps them across openings of the popup; bind them to keep them longer |

`Id` and unmatched attributes go on the text field, and the label's `for` follows `Id`. `Class`, `Style`, `Padding` and `Rounded` go on the bordered box around the swatch and the text field, and `Margin` on the outer element, around the label. Up to 0.1.12 the input ignored `Id`.

The swatch shows the value only while it is a hex colour (`#rgb`, `#rgba`, `#rrggbb` or `#rrggbbaa`) or a theme token such as `var(--moka-color-primary)`, and stays empty for anything else, so typed text can never add CSS of its own. Up to 0.1.12 it also showed CSS colour names.

## Basic

```blazor-preview
<MokaColorInput @bind-Value="_color1" />

@code {
    private string _color1 = "#d32f2f";
}
```

## With Label

```blazor-preview
<MokaColorInput @bind-Value="_color2" Label="Brand Color" HelperText="Enter a hex color code" />

@code {
    private string _color2 = "#1976d2";
}
```

## Disabled

```blazor-preview
<MokaColorInput Value="#388e3c" Label="Locked Color" Disabled="true" />
```

## Without Native Picker

```blazor-preview
<MokaColorInput @bind-Value="_color3" ShowNativeInput="false" Label="Manual Entry Only" />

@code {
    private string _color3 = "#ff9800";
}
```

## Sizes

```blazor-preview
<MokaFlexbox Direction="MokaDirection.Column" Gap="MokaSpacingScale.Sm" Style="max-width: 300px;">
    <MokaColorInput @bind-Value="_colorSm" Size="MokaSize.Sm" Label="Small" />
    <MokaColorInput @bind-Value="_colorMd" Size="MokaSize.Md" Label="Medium" />
    <MokaColorInput @bind-Value="_colorLg" Size="MokaSize.Lg" Label="Large" />
</MokaFlexbox>

@code {
    private string _colorSm = "#e91e63";
    private string _colorMd = "#9c27b0";
    private string _colorLg = "#673ab7";
}
```

## Looks

`Look` draws the field another way. `Standard` is the default and stays exactly as above. Every other look takes only a hex color or a theme token, shows "Six hex digits" (`InvalidText`) for anything else, and in an `EditForm` that text is the validation message. All but `Terminal` open a `MokaColorPicker` panel in a popup in the top layer, against the field: Escape closes it and puts focus back on what opened it, and a click outside closes it. `PickerLayout` picks the panel, `Compact` by default.

The looks with no text box (`SwatchButton`, `Chip`, `Palette`) post their value from a hidden input on a static page, and the label names their button through `aria-labelledby` rather than `for`.

### Swatch button

A round swatch with the hex beside it in mono. Clicking it opens the picker underneath. Quiet enough for a long settings list. A theme token shows by its name, such as "Primary".

```razor
<MokaColorInput Look="MokaColorInputLook.SwatchButton" Label="Color" @bind-Value="_color" />
```

### Split field

Swatch, hex and an eyedropper in one bordered field. Type the hex, open the picker from the swatch, or pick a color off the screen. The eyedropper shows only where the browser has one (Edge and Chrome). A typed color takes effect once it has six digits, or any valid length when you leave the field.

```razor
<MokaColorInput Look="MokaColorInputLook.Split" Label="Color" HelperText="Hex, like #EF5350" @bind-Value="_color" />
```

### Inline chip

A small pill with a dot and the hex, for toolbars and table cells where a whole field is too big. It has no label above it: `Label` becomes part of the button's name for screen readers. `Margin`, `Padding` and `Style` go on the chip.

```razor
<MokaColorInput Look="MokaColorInputLook.Chip" Label="Fill color" @bind-Value="_fill" />
```

### Palette field

Preset swatches and a Custom button that opens the picker, for tag and label colors, where most people take a preset. The eight defaults are red, orange, amber, green, teal, blue, violet and gray. Custom shows the chosen color when it isn't one of them.

```razor
<MokaColorInput Look="MokaColorInputLook.Palette"
                Label="Label color"
                Presets="@_tagColors"
                ColorLabel="@(c => _names.GetValueOrDefault(c))"
                @bind-Value="_color" />
```

### Terminal

Mono hex in capitals beside a small square that glows in the color, square corners, and an accent ring with an inner glow on focus. The tag at the end reads `HEX`, `VAR` for a theme token, or `INVALID`.

```razor
<MokaColorInput Look="MokaColorInputLook.Terminal" Label="Brand color" @bind-Value="_brand" />
```

### The popup's panel

The panel's extras pass through, and the field keeps the recent colors between openings. A color chosen in the popup goes to the front of the recent colors when the popup closes.

```razor
<MokaColorInput Look="MokaColorInputLook.SwatchButton"
                PickerLayout="MokaColorPickerLayout.Tabbed"
                ShowThemeColors="true"
                ShowContrast="true"
                ClearableRecentColors="true"
                @bind-RecentColors="_recent"
                @bind-Value="_color" />
```
