---
title: Gauge
description: Semicircular arc gauge for displaying values within a range.
order: 47
---

# Gauge

`MokaGauge` renders a speedometer-style semicircular arc that fills proportionally to the current value within a min/max range. Use it for CPU usage, performance scores, temperature, or any bounded metric.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value |
| `Min` | `double` | `0` | Minimum value |
| `Max` | `double` | `100` | Maximum value |
| `Label` | `string?` | -- | Text label below the value |
| `ShowValue` | `bool` | `true` | Displays the numeric value in the center |
| `Format` | `string` | `"N0"` | .NET numeric format string for the displayed value |
| `Color` | `MokaColor` | `Primary` | Arc color theme |
| `Size` | `MokaSize` | `Md` | Gauge size: `Sm`, `Md`, `Lg` |
| `StartAngle` | `double` | `-90` | Arc start angle in degrees |
| `EndAngle` | `double` | `90` | Arc end angle in degrees |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Percentage

```blazor-preview
<MokaGauge Value="72" />
```

## With Label

```blazor-preview
<MokaGauge Value="72" Label="CPU Usage" Color="MokaColor.Primary" />
```

## Custom Range (0-200 RPM)

```blazor-preview
<MokaGauge Value="145" Min="0" Max="200" Label="RPM" Format="N0" Color="MokaColor.Warning" />
```

## Colors and Sizes

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Xl" Align="MokaAlign.End">
    <MokaGauge Value="25" Label="Low" Color="MokaColor.Success" Size="MokaSize.Sm" />
    <MokaGauge Value="60" Label="Medium" Color="MokaColor.Warning" Size="MokaSize.Md" />
    <MokaGauge Value="92" Label="High" Color="MokaColor.Error" Size="MokaSize.Lg" />
</MokaFlexbox>
```

## Without Value Display

```blazor-preview
<MokaGauge Value="50" ShowValue="false" Label="Progress" Color="MokaColor.Secondary" />
```
