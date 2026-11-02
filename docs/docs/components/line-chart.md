---
title: Line Chart
description: A category line chart with grid lines, tick labels and a legend, in pure SVG.
order: 56
---

# Line Chart

`MokaLineChart` draws one or more named series as polylines over a common y-axis. Pure SVG, so it
themes and scales with the page; series without an explicit color take the palette in rotation.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Series` | `IReadOnlyList<MokaLineSeries>?` | -- | The series to draw. Each has `Name`, `Values` and an optional `Color` |
| `Labels` | `IReadOnlyList<string>?` | -- | Category names along the x-axis, one per value |
| `ShowPoints` | `bool` | `true` | Whether a dot marks every value |
| `ShowArea` | `bool` | `false` | Whether the area between each line and the x-axis is filled |
| `YTicks` | `int` | `4` | Number of y-axis divisions to draw grid lines for |
| `Min` | `double?` | -- | Fixed floor of the y-axis. Null = computed from the values |
| `Max` | `double?` | -- | Fixed ceiling of the y-axis. Null = computed from the values |
| `ShowLegend` | `bool` | `true` | Whether the series names are listed under the chart |
| `ShowXLabels` | `bool` | `true` | Whether category names are drawn along the x-axis |
| `TickFormat` | `string` | `"0.##"` | .NET numeric format for the y-axis tick labels |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Size` | `MokaSize` | `Md` | The height the chart scales into |

## Basic

```blazor-preview
<MokaLineChart Labels="@(new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })"
               Series="@(new[] { new MokaLineSeries { Name = "Revenue", Values = new[] { 12d, 18, 15, 22, 28 } } })" />
```

## Two series with an area

```blazor-preview
@code {
    MokaLineSeries[] _series =
    [
        new() { Name = "Revenue", Values = [12d, 18, 15, 22, 28], Color = MokaColor.Primary },
        new() { Name = "Costs", Values = [8d, 9, 11, 10, 13], Color = MokaColor.Warning }
    ];
}

<MokaLineChart Labels="@(new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })" Series="_series" ShowArea="true" />
```

Every point carries a native tooltip naming its category, series and value. The y-axis starts at
zero when every value is positive and ends at zero when every value is negative; `Min` and `Max`
pin it when the data should not decide.

Series take the palette in rotation - primary, secondary, success, info, warning, error, surface -
and the same names as `MokaColor` pick a color outright.
