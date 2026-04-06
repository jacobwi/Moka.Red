---
title: Number Ticker
description: Animated rolling number counter that transitions between values.
order: 46
---

# Number Ticker

`MokaNumberTicker` displays a number that animates smoothly when its value changes, rolling each digit into place. Use it for dashboards, stats, or any place where a changing number deserves visual emphasis.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | The number to display |
| `Format` | `string` | `"N0"` | .NET numeric format string (e.g. `"N0"`, `"C2"`, `"F1"`) |
| `Duration` | `int` | `1000` | Animation duration in milliseconds |
| `Size` | `MokaSize` | `Md` | Text size: `Xs`, `Sm`, `Md`, `Lg` |
| `Color` | `MokaColor?` | -- | Optional color theme |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Counter

```blazor-preview
<MokaNumberTicker Value="1234" />
```

## Formatted Currency

```blazor-preview
<MokaNumberTicker Value="9999.99" Format="C2" Size="MokaSize.Lg" />
```

## Fast Animation

```blazor-preview
<MokaNumberTicker Value="500" Duration="300" />
```

## Colored

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Lg">
    <MokaNumberTicker Value="42" Color="MokaColor.Primary" Size="MokaSize.Lg" />
    <MokaNumberTicker Value="87" Color="MokaColor.Success" Size="MokaSize.Lg" />
    <MokaNumberTicker Value="13" Color="MokaColor.Error" Size="MokaSize.Lg" />
</MokaFlexbox>
```

## Interactive Demo

```blazor-preview
<MokaFlexbox Align="MokaAlign.Center" Gap="MokaSpacingScale.Md">
    <MokaButton OnClick="() => _tickerValue += 100" Variant="MokaVariant.Outlined" Size="MokaSize.Sm">+100</MokaButton>
    <MokaNumberTicker Value="_tickerValue" Size="MokaSize.Lg" Color="MokaColor.Primary" />
    <MokaButton OnClick="() => _tickerValue = 0" Variant="MokaVariant.Text" Size="MokaSize.Sm">Reset</MokaButton>
</MokaFlexbox>

@code {
    private double _tickerValue = 0;
}
```
