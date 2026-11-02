---
title: Sparkline
description: A small inline line chart, the kind that sits beside a number in a dashboard card.
order: 55
---

# Sparkline

`MokaSparkline` draws a polyline over its values with an optional area fill and a dot on the last
point. Pure SVG, so it themes and scales with the page around it.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Values` | `IReadOnlyList<double>?` | -- | The values to draw. Null or fewer than two renders an empty box |
| `ShowArea` | `bool` | `true` | Whether the area under the line is filled |
| `ShowLastPoint` | `bool` | `true` | Whether a dot marks the last value |
| `StrokeWidth` | `double` | `2` | Stroke width of the line, in viewBox units |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Color` | `MokaColor?` | -- | Line color. Null = the theme's primary |
| `Size` | `MokaSize` | `Md` | The box the sparkline scales into |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<MokaSparkline Values="@(new[] { 3d, 7, 4, 8, 5, 9, 6 })" />
```

## Without the area fill

```blazor-preview
<MokaSparkline Values="@(new[] { 9d, 6, 8, 4, 7, 3, 5 })" ShowArea="false" />
```

## Colors and sizes

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Lg" Align="MokaAlign.Center">
    <MokaSparkline Values="@(new[] { 1d, 3, 2, 5 })" Color="MokaColor.Success" Size="MokaSize.Sm" />
    <MokaSparkline Values="@(new[] { 1d, 3, 2, 5, 4, 6 })" Color="MokaColor.Warning" Size="MokaSize.Md" />
    <MokaSparkline Values="@(new[] { 1d, 3, 2, 5, 4, 6, 5, 7 })" Color="MokaColor.Error" Size="MokaSize.Lg" />
</MokaFlexbox>
```

The last point carries a native tooltip with its value; a flat series draws a line through the
middle instead of collapsing onto an edge.
