---
title: Bar Chart
description: A category bar chart, side by side or stacked, in pure SVG.
order: 57
---

# Bar Chart

`MokaBarChart` draws one or more named series as bars over a common y-axis, side by side or
stacked onto one column per category. The plot fills the width of its box at the box's height, the tick
labels are text beside and under it, and the category labels thin out as the chart narrows.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Series` | `IReadOnlyList<MokaBarSeries>?` | -- | The series to draw. Each has `Name`, `Values` and an optional `Color` |
| `Labels` | `IReadOnlyList<string>?` | -- | Category names along the x-axis, one per value |
| `Stacked` | `bool` | `false` | Whether the series pile onto one column per category |
| `YTicks` | `int` | `4` | Number of y-axis divisions to draw grid lines for |
| `Min` | `double?` | -- | Fixed floor of the y-axis. Null = computed from the values |
| `Max` | `double?` | -- | Fixed ceiling of the y-axis. Null = computed from the values |
| `ShowLegend` | `bool` | `true` | Whether the series names are listed under the chart |
| `ShowXLabels` | `bool` | `true` | Whether category names are drawn along the x-axis |
| `TickFormat` | `string` | `"0.##"` | .NET numeric format for the y-axis tick labels |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Size` | `MokaSize` | `Md` | The tallest the chart grows to: Sm 160, Md 240, Lg 320 px. Below that it keeps a 600 by 260 proportion |
| `Height` | `string?` | -- | A fixed height for the plot and its labels, such as `"160px"`, whatever the width |

## Basic

```blazor-preview
<MokaBarChart Labels="@(new[] { "Q1", "Q2", "Q3", "Q4" })"
              Series="@(new[] { new MokaBarSeries { Name = "Sales", Values = new[] { 24d, 31, 28, 39 } } })" />
```

## Grouped series

```blazor-preview
@code {
    MokaBarSeries[] _series =
    [
        new() { Name = "2025", Values = [24d, 31, 28, 39] },
        new() { Name = "2026", Values = [30d, 34, 36, 44], Color = MokaColor.Success }
    ];
}

<MokaBarChart Labels="@(new[] { "Q1", "Q2", "Q3", "Q4" })" Series="_series" />
```

## Stacked

```blazor-preview
@code {
    MokaBarSeries[] _series =
    [
        new() { Name = "Direct", Values = [12d, 14, 11, 16] },
        new() { Name = "Referral", Values = [6d, 5, 8, 7], Color = MokaColor.Info },
        new() { Name = "Social", Values = [3d, 4, 6, 5], Color = MokaColor.Warning }
    ];
}

<MokaBarChart Labels="@(new[] { "Q1", "Q2", "Q3", "Q4" })" Series="_series" Stacked="true" />
```

Bars round the end they point to - the top for positive values, the bottom for negative ones -
and keep a plain edge against the axis. Every bar carries a native tooltip naming its category,
series and value. Stacked columns size the axis by the category totals, so a tall pile never runs
off the top.

## Width and height

The plot stretches to fill its box, so a wide, short panel has no empty bands beside the bars. The tick labels
stay text at their own size: the y labels sit in a column as wide as the longest one, and each category label sits
under the middle of its bars.

How tall the box is depends on `Height`:

- Without `Height` the box keeps a 600 by 260 proportion, capped by `Size` (Sm 160, Md 240, Lg 320 px), the same
  box the chart always had. It grows with its width until it reaches the cap.
- With `Height` the box is that tall whatever its width, for a panel or card with a set height.

```razor
<MokaBarChart Labels="_hours" Series="_series" Height="140px" />
```

Since the labels no longer shrink with the chart, they thin out instead: a narrow chart keeps every 2nd, 4th or
8th label, judged by how long they are, and always the last one. The axis is a CSS size container, so this follows
the chart's own width without script.
