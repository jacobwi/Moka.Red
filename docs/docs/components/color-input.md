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
| `Placeholder` | `string` | `"#000000"` | Placeholder text |
| `ShowPreview` | `bool` | `true` | Show a color swatch preview next to the input |
| `ShowNativeInput` | `bool` | `true` | Show the browser's native `<input type="color">` picker button |
| `Size` | `MokaSize` | `Md` | Input size: `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables the input |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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
