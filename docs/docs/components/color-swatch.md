---
title: Color Swatch
description: Grid of color swatches for color selection with circle/square shapes, custom color input, and two-way binding.
order: 13
---

# Color Swatch

`MokaColorSwatch` renders a configurable grid of color swatches. Each swatch is a clickable button that sets `SelectedColor`. The component supports circle and square shapes, a configurable grid width, custom swatch sizing, and an optional "Add custom color" input for free-form hex entry.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Colors` | `IEnumerable<string>` | `[]` | Hex color values to display |
| `SelectedColor` | `string?` | — | Currently selected color (two-way bindable) |
| `SelectedColorChanged` | `EventCallback<string?>` | — | Notified when selection changes |
| `Columns` | `int` | `8` | Number of swatch columns |
| `SwatchSize` | `string` | `"24px"` | CSS size of each swatch |
| `Shape` | `MokaSwatchShape` | `Circle` | `Circle` or `Square` |
| `AllowCustom` | `bool` | `false` | Shows a `+` button to enter a custom hex color |
| `Class` | `string?` | — | Additional CSS classes |
| `Style` | `string?` | — | Additional inline styles |

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

When `AllowCustom` is enabled a `+` swatch appears at the end of the grid. Clicking it reveals an inline hex color input.

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
