---
title: Radar Chart
description: A few series of values across a handful of axes, each series a closed shape, for an item's profile at a glance.
order: 61
---

# Radar Chart

`MokaRadarChart` draws a few series across a handful of axes, each series a closed shape around the center, such as
two laptops' review scores or where three regions sit on uptime, latency and cost. It shows a profile at a glance:
which item is lopsided, which is all-round.

A shape is hard to read exactly. Each value is a length along a spoke at its own angle, and a shape's area grows with
the square of its values, so the axes beside a value change how big it looks. To read values, rank items or tell close
values apart, use bars: a horizontal [Bar Chart](bar-chart.md) with a series per item, or a [Dot Plot](dot-plot.md).
Reach for a radar when the shape is the point, with three to twelve axes and up to three series. Past three series the
shapes cover each other: draw one small radar per item in a `MokaGrid` instead, with the same `Max` so they compare.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Axes` | `IReadOnlyList<MokaRadarAxis>?` | -- | The axes, clockwise from twelve o'clock in this order. Fewer than three draw nothing, and the chart shows its empty state |
| `Series` | `IReadOnlyList<MokaRadarSeries>?` | -- | The shapes, each with one value per axis |
| `Scale` | `MokaRadarScale` | `Shared` | `Shared`: one scale for every axis, written up the top axis. `PerAxis`: each axis on its own scale, for measures in different units |
| `Min` | `double?` | -- | The value at the center on a shared scale. Null starts at zero, or lower for a value below zero |
| `Max` | `double?` | -- | The value at the edge on a shared scale, such as 10 for scores out of 10. Null takes a round number at or above the largest value |
| `Rings` | `int` | `4` | About how many rings mark the scale, at most 10. On a shared scale they fall on round values |
| `GridShape` | `MokaRadarGridShape` | `Polygon` | Rings as straight lines between the axes, or `Circle` |
| `Grid` | `MokaChartGrid?` | the look's | `Baseline` draws the outer ring alone and `None` none; the spokes are the axes and always show |
| `Points` | `MokaChartPoints?` | the look's | A dot on every value (`Always`, the Standard look), only on the axis pointed at (`Hover`), or `None` |
| `Area` | `MokaChartArea?` | filled | A light tint inside each shape. `None` draws outlines only, which reads better past two series |
| `Formatter` | `Func<double, string>?` | -- | Writes values in the tooltip, the table and the scale |
| `Height` | `string?` | -- | A fixed height. Without it the chart keeps a 4 by 3 box, no taller than its `Size` allows |

It also takes the parameters every chart shares (`Title`, `Subtitle`, `Label`, `Look`, `Colors`, `Texture`,
`Highlight`, `HiddenSeries`, `Tooltip`, `Keyboard`, `Controls` and the rest; see [Charts](charts.md)).

### MokaRadarAxis

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | What the axis measures, written at its end |
| `Min`, `Max` | `double?` | The axis's own ends under `Scale = PerAxis`. Ignored on a shared scale |
| `Reversed` | `bool` | Less is better on this axis, so its highest value sits at the center and further out always reads as better |
| `Formatter` | `Func<double, string>?` | Writes this axis's values, when its unit differs from the others'. Null takes the chart's `Formatter` |

### MokaRadarSeries

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | The series' name, in the legend and the tooltip |
| `Values` | `IReadOnlyList<double>` | One value per axis. `double.NaN` is a value nobody has: the outline breaks there and the shape isn't filled, so a gap never reads as zero |
| `Key`, `Slot`, `Color`, `ColorValue` | | Identity and color, as on every chart's series |

## Scores out of 10

```blazor-preview
@code {
    MokaRadarAxis[] _axes =
    [
        new("Performance"), new("Battery"), new("Display"), new("Build"), new("Keyboard")
    ];

    MokaRadarSeries[] _laptops =
    [
        new("Laptop A", [8.5, 6, 9, 7.5, 8]),
        new("Laptop B", [7, 9, 7.5, 8, 6.5])
    ];
}

<MokaRadarChart Title="Laptops compared" Axes="_axes" Series="_laptops" Max="10" Look="MokaChartLook.Quiet" />
```

## Measures in different units

`Scale = MokaRadarScale.PerAxis` puts each axis on its own scale, from its `Min` to its `Max`, so uptime, latency and
cost can share a chart. The rings then mark quarters of each axis rather than values, and the tooltip and the table
give each value in its axis's own `Formatter`. `Reversed` turns an axis where less is better, so further out reads as
better everywhere.

```razor
@code {
    MokaRadarAxis[] _axes =
    [
        new("Uptime") { Min = 99, Max = 100, Formatter = Percent },
        new("Latency") { Reversed = true, Max = 200, Formatter = Ms },
        new("Throughput") { Formatter = Gbps },
        new("Cost") { Reversed = true, Formatter = Dollars }
    ];

    MokaRadarSeries[] _regions =
    [
        new("Frankfurt", [99.98, 38, 9.2, 410]),
        new("Virginia", [99.95, 95, 12.5, 330])
    ];

    static string Percent(double v) => $"{v:0.##}%";
    static string Ms(double v) => $"{v:0} ms";
    static string Gbps(double v) => $"{v:0.#} Gbps";
    static string Dollars(double v) => $"${v:#,0}";
}

<MokaRadarChart Title="Regions" Subtitle="Further out is better" Scale="MokaRadarScale.PerAxis"
                GridShape="MokaRadarGridShape.Circle" Area="MokaChartArea.None" Axes="_axes" Series="_regions" />
```

Keep the formatters fields or methods: a lambda that captures a local is new every render, and the chart then draws
again each time.

## Reading it

- Pointing at an axis, anywhere in the wedge around its spoke or on its name, reads every series' value on that axis.
  With `Keyboard` (on in every look but Standard) the chart is one tab stop, and Left and Right move between axes.
  `OnPointClick` and `ActiveChanged` carry the first series' point on the axis; the tooltip context
  (`TooltipTemplate`) lists every series.
- The legend hides, isolates and highlights series as on every chart, and a hidden series takes no part in the shared
  scale. A series keeps its color when another is hidden.
- The table view lists a row per axis and a column per series, and CSV export saves the same values.
- With `Texture`, each shape is hatched at its own angle, so the series tell apart without color, on screen, in print
  and under forced colors.
- The spokes, rings and shapes are one small drawing that scales with the box, so strokes and dots keep their size in
  pixels. The axis names sit outside it as text: the drawing leaves room beside it for the longest side name, never more
  than about a third of the width, and longer names wrap.

## Instead of a radar

| The question | Use |
|---|---|
| Which item scores highest on each measure? | A horizontal [Bar Chart](bar-chart.md) with a series per item |
| How do many items rank on one measure? | A horizontal [Bar Chart](bar-chart.md), largest first, or a [Dot Plot](dot-plot.md) |
| How did each measure change between two moments? | A [Dot Plot](dot-plot.md) with `Before` |
| One item against a target on each measure? | A list of [Meters](meter.md) with `Target` |
