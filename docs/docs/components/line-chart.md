---
title: Line Chart
description: A category line chart with grid lines, tick labels and a legend, in pure SVG.
order: 56
---

# Line Chart

`MokaLineChart` draws one or more named series as polylines over a common y-axis. The plot fills the width of its
box at the box's height, while the labels, the dots and the line widths keep their size and shape, and the category
labels thin out as the chart narrows; series without an explicit color take the palette in rotation. It also
draws a live window of samples, such as a transfer rate fed once a second: see [Live](#live).

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
| `Size` | `MokaSize` | `Md` | The tallest the chart grows to: Sm 160, Md 240, Lg 320 px. Below that it keeps a 600 by 260 proportion |
| `Height` | `string?` | -- | A fixed height for the plot and its labels, such as `"230px"`, whatever the width |
| `Formatter` | `Func<double, string>?` | -- | Writes a value for the ticks, the point tooltips and the legend, such as "1.2 MB/s". Null uses `TickFormat` |
| `TooltipLabel` | `Func<int, string>?` | -- | Writes the start of a point's tooltip for a category index, such as the full time of a sample. An empty string leaves it off. Null uses `Labels` |
| `ShowCurrent` | `bool` | `false` | Whether the legend shows each series' latest value |
| `ShowPeak` | `bool` | `false` | Whether the legend ends with the highest value any series reaches |
| `PeakLabel` | `string` | `"Peak"` | The word before the peak |
| `AutoScale` | `bool` | `false` | Rounds the ends of the y-axis out to a step of 1, 2, 2.5 or 5 times a power of ten, so every tick is round |
| `ScaleUnits` | `IReadOnlyList<double>?` | -- | With `AutoScale`, the units `Formatter` writes in, such as 1, 1024 and 1048576, so the step is round in the unit shown |
| `SuggestedMax` | `double?` | -- | The least the top of the y-axis reaches when `Max` isn't set. Higher values still raise it |
| `Smooth` | `bool` | `false` | Draws smooth curves that never bulge past a peak or a trough |
| `ShowLastPoint` | `bool` | `false` | A ringed dot on each series' latest value |
| `Live` | `bool` | `false` | Slides the lines one step left when the window moves, instead of jumping |
| `LiveInterval` | `TimeSpan` | 1 s | How long the slide takes |
| `EmptyText` | `string?` | -- | Text in the middle of the plot while there are no values |

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

## Gaps

`double.NaN` in a series marks a value that's missing. The line and the area break there and no point is drawn. It doesn't count toward the y-axis, and a series that is all `NaN` draws nothing but keeps its place in the legend.

```razor
<MokaLineChart Labels="@(new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })"
               Series="@(new[] { new MokaLineSeries { Name = "Revenue", Values = new[] { 12d, 18, double.NaN, 22, 28 } } })" />
```

Every point carries a native tooltip naming its category, series and value. The y-axis starts at
zero when every value is positive and ends at zero when every value is negative; `Min` and `Max`
pin it when the data should not decide.

Series take the palette in rotation - primary, secondary, success, info, warning, error, surface -
and the same names as `MokaColor` pick a color outright.

A long tick label, such as one `Formatter` writes, moves the plot right to make room rather than running off
the left edge.

## Category labels

The labels along the x-axis thin out with the chart's own width, with no script. A wide chart shows every label
that fits, and a narrow one keeps every 2nd, 4th or 8th from the first. The last label always shows. Each label needs
room beside the labels that outrank it, judged by the length of each, so one long label only costs the labels next to
it. A long series writes only the labels some width has room for, so a window of 5000 samples is still a few labels in
the page.

A blank label takes no place on the axis, so you can label only the marks you want. Here a two-minute window shows
four marks at round times, and they stay once the chart has room for them:

```razor
<MokaLineChart Series="_series" Labels="_marks"/>

@code {
    private readonly string[] _marks = [.. Enumerable.Range(0, 111).Select(i => i switch
    {
        20 => "-90s",
        50 => "-60s",
        80 => "-30s",
        110 => "now",
        _ => ""
    })];
}
```

The axis is a CSS size container and each label names the narrowest axis it fits on, so the labels change when the
chart is resized, a sidebar opens or the font grows, without a render.

Short axis labels often say too little in a tooltip. `TooltipLabel` writes the start of a point's tooltip from its
index instead, so the axis can read "12:05" while the tooltip reads "Sun 8 Nov 12:05:30 · Down: 1.2 MiB/s":

```razor
<MokaLineChart Series="_series"
               Labels="_minutes"
               TooltipLabel="@(i => _samples[i].Time.ToString("ddd d MMM HH:mm:ss"))" />
```

Without it the tooltip starts with the category's label, as before.

## Live

For a rolling window, such as a transfer rate sampled once a second, pass the whole window each time, padded at
the start with `double.NaN` until it fills. With `Live`, when the values change and the count stays the same, the
lines start one step to the right and slide home over `LiveInterval`, so the chart scrolls rather than jumps. The
slide is a CSS animation, so it costs no renders, and it stands still for people who asked for reduced motion.

```razor
<MokaLineChart Series="_series"
               Labels="_labels"
               Label="Throughput"
               Formatter="Rate"
               AutoScale="true"
               ScaleUnits="@(new[] { 1d, 1024, 1024 * 1024 })"
               SuggestedMax="4096"
               ShowPoints="false"
               ShowLastPoint="true"
               ShowCurrent="true"
               ShowPeak="true"
               ShowArea="true"
               Smooth="true"
               Live="true"
               EmptyText="Waiting for traffic" />

@code {
    private static string Rate(double bytes) => bytes switch
    {
        >= 1024 * 1024 => $"{bytes / (1024 * 1024):0.0} MiB/s",
        >= 1024 => $"{bytes / 1024:0} KiB/s",
        _ => $"{bytes:0} B/s"
    };
}
```

A dashboard card usually wants a set height rather than one that follows the card's width: `Height="230px"` keeps
the chart that tall while the plot fills the card's width.

A parent that renders every second hands the chart its parameters every second. The chart compares them with the
last ones, the values included, and draws again only when something differs, so an unchanged window costs no
render and a list the app changed in place still redraws. Turn `ShowPoints` off for a window of many samples: a dot
per value is an element per value in the page.

## Width and height

The plot stretches to fill its box, so a chart in a wide card has no empty bands beside it. What mustn't stretch
is kept out of the stretch: the tick labels are text beside and under the plot, in a column as wide as the longest
label, the lines and grid keep their width, and the dots stay round.

How tall the box is depends on `Height`:

- Without `Height` the box keeps a 600 by 260 proportion, capped by `Size` (Sm 160, Md 240, Lg 320 px). It grows
  with its width until it reaches the cap, so a narrow chart is short and a wide one stops at the cap.
- With `Height` the box is that tall whatever its width. Use it for a dashboard card with a set height, where a
  chart that grew with the card's width would push the rest of the card around.

```razor
<MokaCard Title="Throughput">
    <MokaLineChart Series="_series" Labels="_labels" Height="180px" />
</MokaCard>
```

With `ShowCurrent`, the chart's accessible name carries the latest values too, such as "Throughput. Down 1.2 MiB/s,
Up 300 KiB/s, Peak 3.9 MiB/s", so a screen reader hears what the legend shows.
