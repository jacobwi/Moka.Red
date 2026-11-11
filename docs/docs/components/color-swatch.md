---
title: Color Swatch
description: A radio group of color swatches, with seven looks, six marks for the chosen color, groups under headings, a no color option, custom colors and two-way binding.
order: 13
---

# Color Swatch

`MokaColorSwatch` renders a grid of color swatches to pick one color from. It is a radio group: each swatch holds a native radio, so Tab reaches the group once, the arrow keys move the choice, and a screen reader hears each color's name and whether it's checked. The component supports circle and square shapes, sizes, colors under headings, a no color swatch, and an optional custom color input.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Colors` | `IEnumerable<string>` | `[]` | Colors to display: hex, a keyword or a color function such as `rgb()` or `var()`. A value that is not a CSS color gets an empty swatch |
| `Groups` | `IReadOnlyList<MokaSwatchGroup>?` | - | Colors under headings, in place of `Colors`. All of them are one radio group |
| `SelectedColor` | `string?` | - | Currently selected color (two-way bindable) |
| `SelectedColorChanged` | `EventCallback<string?>` | - | Notified when selection changes |
| `Columns` | `int` | `8` | The most swatches in a row. A narrower container wraps them. The custom entry and `EndContent` don't count, and `Chips` and `Strip` ignore it |
| `SwatchSize` | `string?` | - | CSS size of each swatch. Wins over `SizeValue` and `Size` |
| `Size` | `MokaSize` | `Md` | 16, 20, 24 or 32 px swatches from `Xs` to `Lg` in the default look; every other look steps from a size of its own |
| `Shape` | `MokaSwatchShape` | `Circle` | `Circle` or `Square` |
| `Look` | `MokaSwatchLook` | `Standard` | How the swatches are drawn: `Standard`, `Ring`, `Tiles`, `Chips`, `Strip`, `Menu` or `Terminal` |
| `Mark` | `MokaSwatchMark` | `Standard` | The mark on the chosen color: the look's own, `Check`, `Ring`, `RingAndCheck`, `ColorRing`, `Dot` or `Badge`. `Chips` and `Menu` ignore it |
| `AllowCustom` | `bool` | `false` | Offers a color the palette doesn't have, which then gets a swatch of its own |
| `CustomLook` | `MokaSwatchCustomLook` | `Standard` | How `AllowCustom` asks for it: a dashed `+`, a `Rainbow` `+`, a Custom `Pill`, or a `Hex` field |
| `AllowClear` | `bool` | `false` | A click on the chosen swatch, or Delete or Backspace on it, clears the selection |
| `ShowNone` | `bool` | `false` | Adds a first swatch, slashed through, that chooses no color |
| `NoneLook` | `MokaSwatchNoneLook` | `Standard` | How `ShowNone` draws it: a slash, a None `Chip`, an `Empty` dashed outline or a `Checkerboard` |
| `EndContent` | `RenderFragment?` | - | Content after the swatches, in the last group's row, such as a button of the app's own |
| `ShowPreview` | `bool` | `true` | Shows the chosen color's name under the swatches |
| `AriaLabel` | `string?` | - | Names the radio group, such as "Folder color" |
| `Text` | `MokaColorSwatchText` | English | Every word the swatch writes: the no color swatch, the custom color controls and the hex field |
| `Disabled` | `bool` | `false` | Every swatch and the `+` button are disabled |
| `ColorLabel` | `Func<string, string?>?` | - | Names a color, such as "Blue" for `#42a5f5`. Unnamed colors are named by their value |
| `ColorDisabled` | `Func<string, bool>?` | - | Says which colors are drawn but can't be picked |
| `Labels` | `IReadOnlyList<MokaLabelColor>?` | - | Label colors to offer in place of `Colors`, such as `MokaLabelColors.Standard`. `AllowCustom` does nothing while it's set |
| `SelectedLabel` | `MokaLabelColor?` | - | The chosen label color while `Labels` is set (two-way bindable) |
| `SelectedLabelChanged` | `EventCallback<MokaLabelColor?>` | - | Notified with the chosen label color, or null when `AllowClear` clears it |
| `LabelName` | `Func<MokaLabelColor, string?>?` | - | Names a label color in the app's words. Null or blank gives the English name, such as "Teal" |
| `LabelDisabled` | `Func<MokaLabelColor, bool>?` | - | Says which label colors are drawn but can't be picked |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

## Basic Usage

```blazor-preview
@code {
    string? _color;
    string[] _palette = [
        "#f44336", "#e91e63", "#9c27b0", "#673ab7",
        "#3f51b5", "#2196f3", "#03a9f4", "#00bcd4",
        "#009688", "#4caf50", "#8bc34a", "#cddc39",
        "#ffeb3b", "#ffc107", "#ff9800", "#ff5722",
        "#795548", "#9e9e9e", "#607d8b", "#000000",
    ];
}

<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_color" />
@if (_color is not null)
{
    <MokaCaption>Selected: @_color</MokaCaption>
}
```

## Looks

`Look` draws the same radio group another way. The keyboard, the names, `Labels`, `Groups`, `ShowNone`, `AllowCustom` and `Disabled` work the same in every look. `Standard`, the default, is the look above: 24 px circles 4 px apart, and the chosen one grows a little with a border in the text color and a check.

| Look | What it draws | For |
|------|---------------|-----|
| `Ring` | 26 px circles 8 px apart. The chosen one gets a ring in the text color with a gap of page between, and keeps its check. Nothing grows, so the row never shifts | Most places. It is the look `MokaColorInput`'s Palette field uses |
| `Tiles` | 30 px rounded squares 6 px apart. A hover raises a thin outline; the chosen tile gets an inner gap and an outline | A settings page, a touch screen |
| `Chips` | Each color a pill with its dot and its name. The chosen pill gets a border in the text color | When two blues are hard to tell apart, or the names are what the app stores |
| `Strip` | The colors joined into one bar. The chosen one lifts out of it with a short mark under it | A toolbar or a table cell |
| `Menu` | One color per row with a dot, its name and a check at the end, in a menu panel. `ShowNone` is separated from the colors by a line | A "Color" item in a context menu |
| `Terminal` | 22 px squares with small corners. The chosen one gets an accent outline with a glow, and the line under the swatches reads like a prompt | The Matrix colors |

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_folder" Look="MokaSwatchLook.Ring"
                 ColorLabel="NameOfColor" AriaLabel="Folder color" />

<MokaColorSwatch Labels="MokaLabelColors.Standard" @bind-SelectedLabel="_tag" Look="MokaSwatchLook.Menu"
                 ShowNone AriaLabel="Tag color" />
```

`Chips` and `Menu` show each name, so a screen reader and an eye read the same word and no tooltip repeats it. The other looks show only the color, and name each swatch through its radio's `aria-label` and a tooltip under it, as the default look does.

Each look has its own size at every `Size`: `Ring` 18, 22, 26 and 34 px from `Xs` to `Lg`, `Tiles` and `Strip` 22 to 38, `Terminal` 14 to 30, and the dots of `Chips` (14 to 22) and `Menu` (10 to 18). `SwatchSize` and `SizeValue` still set any size. `Chips` and `Strip` ignore `Columns`: chips wrap where their container ends, and a strip stays one bar until its container wraps it.

## The Mark on the Chosen Color

`Mark` picks the mark on the chosen color. `Standard`, the default, is the look's own. The others work on every look but `Chips` and `Menu`, which draw their own.

| Mark | What it draws |
|------|---------------|
| `Check` | Just the check, black or white by the color, and a slight grow |
| `Ring` | A ring in the text color with a gap, and the color left whole |
| `RingAndCheck` | The ring and the check together, two signals for people who can't tell the colors apart. `Ring`'s own mark |
| `ColorRing` | The ring takes the chosen color. A pale or grey ring is faint on some pages |
| `Dot` | A dot in the middle, black or white by the color, like a radio button's own dot. It reads at 16 px where a check gets cramped |
| `Badge` | A small accent badge with a check at the bottom right, outside the color |

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_color" Look="MokaSwatchLook.Ring" Mark="MokaSwatchMark.Dot" />
```

The no color swatch takes the same mark in the text color. In the default look it is marked by its border alone, as before.

## Custom Color Entries

`CustomLook` decides how `AllowCustom` asks for a color. Each adds the color as a swatch of its own, checked, after the palette, and a color the palette already has picks that swatch instead.

| CustomLook | What it draws |
|------------|---------------|
| `Standard` | A dashed circle with a `+`. It turns into the browser's color button with a check and a cross, and focus goes in and comes back |
| `Rainbow` | A swatch-sized circle in every hue with a `+`, named "Custom color" in a tooltip. It opens a small panel |
| `Pill` | A dashed pill with a rainbow dot and the word Custom, opening the same panel |
| `Hex` | A small mono field after the swatches. Six hex digits, with or without `#`, and Enter add the color |

The panel opens in the top layer against its button, so a card that hides its overflow can't cut it off. It holds the browser's color button and a hex field that follow each other, with Cancel and Add. The hex field opens on the chosen color, selected, so typing replaces it; Enter adds, Escape or Cancel close with no change, and focus goes back to the button. A hex field that holds no color says so, through `aria-invalid` and "Six hex digits" for screen readers, and adds nothing. Enter in either hex field never submits a form around the swatch.

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_highlight" AllowCustom
                 CustomLook="MokaSwatchCustomLook.Pill" Look="MokaSwatchLook.Ring" AriaLabel="Highlight" />

<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_highlight" AllowCustom
                 CustomLook="MokaSwatchCustomLook.Hex" />
```

## No Color Looks

`NoneLook` decides how `ShowNone` draws the swatch that chooses no color. It is a radio like the others, first in the group.

| NoneLook | What it draws |
|----------|---------------|
| `Standard` | A red slash through the swatch, as office apps draw "no fill" |
| `Chip` | A small pill that says None, filled in the text color while chosen. Its word is its name, so it has no tooltip |
| `Empty` | A dashed outline where a color would be |
| `Checkerboard` | The checkerboard design tools use for transparent, for a fill or a highlight |

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_tag" ShowNone NoneLook="MokaSwatchNoneLook.Chip"
                 Look="MokaSwatchLook.Ring" AriaLabel="Tag color" />
```

In `Chips` and `Menu` the no color swatch is a named chip or row whose dot is drawn the same way.

## An End Slot

`EndContent` puts content after the swatches, in the last group's row, where it flows and wraps with them. `MokaColorInput`'s Palette look puts its Custom button there.

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_accent" Look="MokaSwatchLook.Ring" Mark="MokaSwatchMark.Ring">
    <EndContent>
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Outlined" OnClick="Reset">Reset</MokaButton>
    </EndContent>
</MokaColorSwatch>
```

## The Ring's Gap and the Focus Glow

The gap between a swatch and its ring is the surface's color, so it reads as page on a card. Where swatches sit on another color, such as a popup or a panel, set `--moka-swatch-ring-gap` to that color on the swatch or an element around it. `--moka-swatch-halo` retints the soft ring outside the focus ring, `--moka-color-primary-ring` unless set.

```css
.my-popup {
    --moka-swatch-ring-gap: var(--moka-popup-bg, var(--moka-color-surface));
}
```

## Square Shape

```blazor-preview
@code {
    string? _color;
    string[] _colors = ["#d32f2f","#1976d2","#388e3c","#f57c00","#7b1fa2","#0288d1","#455a64","#212121"];
}

<MokaColorSwatch Colors="_colors"
                 @bind-SelectedColor="_color"
                 Shape="MokaSwatchShape.Square"
                 SwatchSize="32px"
                 Columns="4" />
```

## Swatch Size

```blazor-preview
@code {
    string? _color;
    string[] _muted = ["#ef9a9a","#f48fb1","#ce93d8","#9fa8da","#81d4fa","#80cbc4","#a5d6a7","#fff59d"];
}

<div style="display:flex;flex-direction:column;gap:16px">
    <div>
        <MokaCaption>Small (16px)</MokaCaption>
        <MokaColorSwatch Colors="_muted" @bind-SelectedColor="_color" SwatchSize="16px" />
    </div>
    <div>
        <MokaCaption>Default (24px)</MokaCaption>
        <MokaColorSwatch Colors="_muted" @bind-SelectedColor="_color" />
    </div>
    <div>
        <MokaCaption>Large (40px)</MokaCaption>
        <MokaColorSwatch Colors="_muted" @bind-SelectedColor="_color" SwatchSize="40px" />
    </div>
</div>
```

## Column Count

```blazor-preview
@code {
    string? _color;
    string[] _spectrum = [
        "#ff0000","#ff4000","#ff8000","#ffbf00",
        "#ffff00","#80ff00","#00ff00","#00ff80",
        "#00ffff","#0080ff","#0000ff","#8000ff",
    ];
}

<MokaColorSwatch Colors="_spectrum" @bind-SelectedColor="_color" Columns="4" SwatchSize="36px" Shape="MokaSwatchShape.Square" />
```

## Allow Custom Color

When `AllowCustom` is enabled a `+` swatch appears at the end of the grid (`CustomLook` draws it another way, see [Custom Color Entries](#custom-color-entries)). Clicking it opens an inline color input, and focus moves into it. "Use this color" picks the color, Cancel or Escape closes the input, and either way focus goes back to the `+`.

A color picked there gets a swatch of its own after the palette, checked, so the group always shows what's chosen. So does a `SelectedColor` the palette doesn't have, such as one the app saved earlier. A custom color the palette already has picks that swatch instead.

Every swatch shows only its colour, so it is named by its value, through the radio's `aria-label` and a `MokaTooltip` below it. The `+` swatch and the color input are "Custom color", and the inline buttons "Use this color" and "Cancel".

```blazor-preview
@code {
    string? _color = "#d32f2f";
    string[] _base = [
        "#d32f2f","#1976d2","#388e3c","#f57c00",
        "#7b1fa2","#0288d1","#455a64","#212121",
    ];
}

<MokaColorSwatch Colors="_base"
                 @bind-SelectedColor="_color"
                 AllowCustom
                 SwatchSize="28px" />
<MokaCaption>Current: <strong>@_color</strong></MokaCaption>
```

## Named Colors

A screen reader reads a swatch named by its value as "#42a5f5". `ColorLabel` gives it a name, which is also its tooltip
and the text beside the chosen color. A color it returns `null` for keeps its value as its name.

```blazor-preview
@code {
    string? _accent = "#ef5350";
    Dictionary<string, string> _names = new()
    {
        ["#ef5350"] = "Red",
        ["#42a5f5"] = "Blue",
        ["#66bb6a"] = "Green",
        ["#ab47bc"] = "Violet",
    };
}

<MokaColorSwatch Colors="_names.Keys"
                 @bind-SelectedColor="_accent"
                 ColorLabel="@(color => _names.GetValueOrDefault(color))"
                 SwatchSize="28px" />
```

## Label Colors

`Labels` offers [label colors](/getting-started/label-colors) by name, for a "Color" item in a menu where people color their tags or folders. Each swatch is the label's solid color, so it follows light and dark and the theme's label tone, and is named by its hue for screen readers, its tooltip and the text beside the chosen color. The pick comes back through `SelectedLabel` as a `MokaLabelColor`, a name to store rather than a color value. `SelectedColor` and `SelectedColorChanged` aren't used while `Labels` is set.

```razor
<MokaColorSwatch Labels="MokaLabelColors.Standard"
                 @bind-SelectedLabel="_folderColor"
                 AllowClear
                 Columns="7"
                 SwatchSize="26px"
                 AriaLabel="Folder color" />

@code {
    private MokaLabelColor? _folderColor = MokaLabelColor.Teal;
}
```

`LabelName` gives the hues the app's own words, and `LabelDisabled` draws some that can't be picked:

```razor
<MokaColorSwatch Labels="MokaLabelColors.All"
                 @bind-SelectedLabel="_color"
                 LabelName="@(color => Localizer[$"Color{color}"])"
                 LabelDisabled="@(color => _used.Contains(color))" />
```

## Keyboard and Screen Readers

The swatches are one radio group, the [WAI-ARIA radio group pattern](https://www.w3.org/WAI/ARIA/apg/patterns/radio/) built from native radios:

- Tab reaches the chosen swatch, or the first one while nothing is chosen, and Tab again leaves the group.
- The arrow keys move the choice and skip disabled colors.
- With `AllowClear`, Delete or Backspace on the chosen swatch clears it. Space doesn't, because Chromium sends no click for Space on a radio that's already checked.

Name the group with `AriaLabel`, or point `aria-labelledby` at a visible label:

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_color" AriaLabel="Folder color" />
```

The check on the chosen swatch is black or white by the color's lightness, so it reads on greys, white and black as well as on bright colors, and every swatch has a faint inner edge, so a white swatch still shows on a white card.

## No Color

`ShowNone` adds a first swatch with a slash through it, "No color", checked while nothing is chosen. Picking it reports `null`. It is the keyboard friendly way to offer "none", and it works with `Labels` too.

```razor
<MokaColorSwatch Labels="MokaLabelColors.Standard"
                 @bind-SelectedLabel="_folderColor"
                 ShowNone
                 AriaLabel="Folder color" />
```

## Groups

`Groups` draws colors under headings, such as theme colors, a standard palette and the ones used lately. Every group is part of one radio group, so Tab still reaches one swatch and the arrows run on from one group into the next. A color can appear in more than one group: the swatch you pick is the one that shows checked. `ShowNone` goes first in the first group, and custom colors and the `+` last in the last.

```razor
<MokaColorSwatch Groups="_groups" @bind-SelectedColor="_highlight" ShowNone AllowCustom AriaLabel="Highlight" />

@code {
    private string? _highlight;
    private static readonly string[] _recent = ["#42a5f5", "#123456"];

    private readonly MokaSwatchGroup[] _groups =
    [
        new("Theme", ["var(--moka-color-primary)", "var(--moka-color-on-surface)"]),
        new("Standard", ["#ef5350", "#ffb547", "#66bb6a", "#42a5f5", "#7e57c2"]),
        new("Recent", _recent)
    ];
}
```

The app keeps the Recent list, for example by adding to it in `SelectedColorChanged`. A group with an empty title draws its colors with no heading.

## Sizes and Narrow Containers

`Size` gives 16, 20, 24 or 32 px swatches from `Xs` to `Lg`, and `SizeValue` or `SwatchSize` any CSS length (a bare number is read as pixels). `Columns` is the most swatches in a row: in a container narrower than that, they wrap onto more rows.

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_color" Size="MokaSize.Lg" Shape="MokaSwatchShape.Square" />
```

## Hiding the Chosen Color's Name

`ShowPreview="false"` leaves out the line under the swatches, for a swatch in a menu or beside a field that already shows the color.

```razor
<MokaColorSwatch Colors="_palette" @bind-SelectedColor="_color" ShowPreview="false" />
```

## Translating

`Text` holds every word the swatch writes: `None`, `Custom`, `Confirm` and `Cancel`, and for the looks `NoneChip` ("None"), `CustomPill` ("Custom"), `Add`, `PickColor`, `Hex`, `HexField`, `HexPlaceholder` and `HexInvalid`.

```razor
<MokaColorSwatch Colors="_palette"
                 @bind-SelectedColor="_color"
                 ShowNone
                 AllowCustom
                 Text="@(new MokaColorSwatchText { None = "Aucune", Custom = "Autre couleur", Confirm = "Choisir", Cancel = "Annuler" })" />
```

## Inside a ColorPicker Integration

`MokaColorSwatch` pairs naturally with `MokaColorPicker` to offer a preset palette alongside the full picker.

```blazor-preview
@code {
    string _color = "#2196f3";
    string[] _presets = [
        "#f44336","#e91e63","#9c27b0","#3f51b5",
        "#2196f3","#009688","#4caf50","#ff9800",
    ];

    void OnSwatchSelected(string? c) { if (c is not null) _color = c; }
}

<div style="display:flex;flex-direction:column;gap:12px;max-width:280px">
    <MokaLabel>Color</MokaLabel>
    <MokaColorSwatch Colors="_presets"
                     SelectedColor="_color"
                     SelectedColorChanged="OnSwatchSelected"
                     SwatchSize="28px" />
    <MokaColorPicker @bind-Value="_color" />
</div>
```

## Two-Way Binding

`SelectedColor` supports `@bind-SelectedColor` for standard Blazor two-way binding.

```blazor-preview
@code {
    string? _pick;
    string[] _colors = ["#e53935","#43a047","#1e88e5","#8e24aa","#fb8c00"];
}

<MokaColorSwatch Colors="_colors" @bind-SelectedColor="_pick" SwatchSize="32px" Shape="MokaSwatchShape.Square" />

@if (_pick is not null)
{
    <div style="width:80px;height:40px;background:@_pick;border-radius:4px;margin-top:8px" />
}
```
