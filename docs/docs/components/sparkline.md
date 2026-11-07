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
| `Values` | `IReadOnlyList<double>?` | -- | The values to draw. Null or fewer than two renders an empty box. `double.NaN` is a gap |
| `ShowArea` | `bool` | `true` | Whether the area under the line is filled |
| `ShowLastPoint` | `bool` | `true` | Whether a dot marks the last value that isn't a gap |
| `StrokeWidth` | `double` | `2` | Stroke width of the line, in viewBox units |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Color` | `MokaColor?` | -- | Line color. Null = the theme's primary |
| `ColorValue` | `string?` | -- | Line color as a CSS color or a token such as `var(--moka-color-success)`. Wins over `Color`. Anything that isn't a color is ignored |
| `Height` | `string?` | -- | Height of the box as a CSS length such as `24px`. Anything else leaves it to `Size` |
| `Min` | `double?` | -- | The value at the bottom of the box. Null = the lowest value |
| `Max` | `double?` | -- | The value at the top of the box. Null = the highest value |
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

## Height, a token and a fixed scale

`Height` sets the box, `ColorValue` takes any color or token, and `Min` and `Max` fix the scale, so a line of percentages doesn't look full at 3%. A value outside the scale sits on the edge of the box.

```razor
<MokaSparkline Values="@(new[] { 3d, 7, 4, 8 })" Height="24px" ColorValue="var(--moka-color-success)" Min="0" Max="100" />
```

## Gaps

`double.NaN` marks a value that's missing. The line and the area break there and start again at the next value, and the dot moves to the last value there is.

```razor
<MokaSparkline Values="@(new[] { 3d, 7, double.NaN, double.NaN, 8, 5 })" />
```

The last point carries a native tooltip with its value; a flat series draws a line through the
middle instead of collapsing onto an edge.
