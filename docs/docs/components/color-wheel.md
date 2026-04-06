---
title: Color Wheel
description: Circular color picker for intuitive hue and saturation selection.
order: 41
---

# Color Wheel

`MokaColorWheel` renders a circular color picker that lets users select colors by interacting with a hue ring and saturation/brightness area. It supports two-way hex value binding, an optional hex input field, and a color preview swatch.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | `"#ff0000"` | Selected color as a hex string. Supports two-way binding via `@bind-Value`. |
| `ValueChanged` | `EventCallback<string>` | -- | Callback when the selected color changes |
| `ShowHexInput` | `bool` | `true` | Shows a text input for entering hex values directly |
| `ShowPreview` | `bool` | `true` | Shows a preview swatch of the selected color |
| `Size` | `MokaSize` | `Md` | Size of the color wheel: `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables interaction |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Color Wheel

```blazor-preview
<MokaColorWheel @bind-Value="color" />
<MokaText Style="margin-top:8px">Selected: @color</MokaText>

@code {
    string color = "#3b82f6";
}
```

## Without Hex Input

Hide the hex input field for a more compact picker.

```blazor-preview
<MokaColorWheel @bind-Value="noInputColor" ShowHexInput="false" />

@code {
    string noInputColor = "#10b981";
}
```

## Sizes

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Lg" Align="MokaAlign.Start">
    <div>
        <MokaCaption>Small</MokaCaption>
        <MokaColorWheel @bind-Value="smColor" Size="MokaSize.Sm" />
    </div>
    <div>
        <MokaCaption>Medium</MokaCaption>
        <MokaColorWheel @bind-Value="mdColor" Size="MokaSize.Md" />
    </div>
    <div>
        <MokaCaption>Large</MokaCaption>
        <MokaColorWheel @bind-Value="lgColor" Size="MokaSize.Lg" />
    </div>
</MokaFlexbox>

@code {
    string smColor = "#ef4444";
    string mdColor = "#8b5cf6";
    string lgColor = "#f59e0b";
}
```

## Without Preview

Hide the color preview swatch.

```blazor-preview
<MokaColorWheel @bind-Value="noPrevColor" ShowPreview="false" />

@code {
    string noPrevColor = "#06b6d4";
}
```
