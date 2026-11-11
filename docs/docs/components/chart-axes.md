---
title: Chart Axes
description: How every cartesian chart draws its axes, labels that don't fit, time and log axes, units, the grid, and charts drawn in rows.
order: 57
---

# Chart Axes

Every cartesian chart (`MokaLineChart`, `MokaBarChart`, `MokaHistogram`, `MokaCandlestickChart`, `MokaScatterChart`, and the
charts drawn in rows: `MokaDotPlot`, `MokaBoxPlot`, `MokaGanttChart`, `MokaFunnel` and `MokaBarChart` with `Horizontal`) draws
its axes the same way. The value axis rounds, writes units and thins its ticks with the plot's height; the axis along the
bottom keeps every label it can and decides what to do with the rest from its own width. None of it runs script: each
label carries the axis width it needs as a class, and the stylesheet answers with container queries.

`MokaLineChart` and `MokaBarChart`, which came before the chart engine, draw these only once one of the new parameters
below, or a look other than Standard, asks for the new drawing; with none of them they draw exactly as before.

## Parameters

On every cartesian chart:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Labels` | `IReadOnlyList<string>?` | -- | Category names along the x axis, one per value |
| `Times` | `IReadOnlyList<DateTimeOffset>?` | -- | One time per value. With them, x is proportional to time and the axis steps in clock and calendar units |
| `XAxis`, `YAxis` | `MokaChartAxis?` | -- | Everything about one axis, below. On a chart drawn in rows the values run along `XAxis` and the names down `YAxis` |
| `Unit` | `MokaChartUnit?` | -- | The units the value axis is rounded and written in. A shortcut for the value axis's `Unit` |
| `LabelFit` | `MokaAxisLabelFit?` | `Auto` | What category labels that don't fit do. A shortcut for the x axis's `Fit` |
| `Grid` | `MokaChartGrid?` | the look's | The lines behind the data |
| `TickLabels` | `MokaTickPlacement?` | the look's | Where the value axis's numbers sit: `Outside`, `Inside` the plot just above their line, or `Auto`, inside under 20 em |
| `ValueSide` | `MokaAxisSide?` | the look's | `Start` (left) or `End` (right, next to the newest values) |
| `TimeZone` | `TimeZoneInfo?` | -- | The zone a time axis is aligned and written in. Null uses a registered `TimeProvider`'s zone, else the server's or browser's |
| `YTicks` | `int` | `4` | How many divisions the value axis aims for when `MokaChartAxis.TickCount` isn't set |
| `Min`, `Max` | `double?` | -- | Fixed ends of the value axis |
| `ShowXLabels` | `bool` | `true` | Whether the labels along the bottom are drawn |
| `TickFormat` | `string` | `"0.##"` | The value axis's .NET format on the Standard look without a unit |
| `Range` / `RangeChanged` | `MokaChartRange?` | -- | The span the chart is zoomed into, two-way. A brush, the wheel, the range strip or a preset changes it |
| `Virtualize` | `bool?` | -- | Rows only: draw rows as they scroll into view. Null turns it on past 200 rows |
| `RowHeight` | `double?` | the look's | Rows only: each row's height in pixels |
| `MaxHeight` | `string?` | -- | Rows only: the height the rows scroll in, under a value axis that stays put |

`MokaChartAxis` (every property optional; one left null takes the look's):

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Visible` | `bool` | `true` | Whether the axis is drawn |
| `Title` | `string?` | -- | A title over the value axis, or under the x axis |
| `Scale` | `MokaAxisScale` | `Auto` | `Linear`, `Log` (each step ten times the last), `Time` or `Category` |
| `Side` | `MokaAxisSide?` | -- | Which side the value axis is on |
| `Min`, `Max` | `double?` | -- | Fixed ends; a fixed end still gets a tick |
| `SuggestedMin`, `SuggestedMax` | `double?` | -- | The axis reaches at least this far; data beyond still moves it |
| `TickCount` | `int?` | -- | How many divisions to aim for |
| `TickValues` | `IReadOnlyList<double>?` | -- | Ticks to draw instead of working them out |
| `Nice` | `bool?` | -- | Round the ends and the step to 1, 2, 2.5, 4 or 5 times a power of ten, in the unit the top reaches. Every tick then takes the step's decimals, so 1.080 sits between 1.076 and 1.084 |
| `IncludeZero` | `bool?` | -- | Whether the axis reaches zero. Null takes the chart's: a value axis reaches zero unless the chart's values sit far from it (candlesticks), an x axis spans only its data |
| `Unit` | `MokaChartUnit?` | -- | `Bytes`, `BytesBinary`, `BitsPerSecond`, `Milliseconds`, `Seconds`, `Percent`, `Compact`, or `MokaChartUnit.Of("req/s")` |
| `ShowUnit` | `bool` | `true` | False writes bare numbers and moves the unit into the title: "Written (GB)" |
| `Compact` | `bool?` | -- | Numbers from ten thousand up as 12K |
| `Format` | `string?` | -- | A .NET format string for the ticks |
| `Formatter` | `Func<double, string>?` | -- | Writes each tick; wins over `Format` and `Unit`. Keep it a field or a method |
| `TickLabels` | `MokaTickPlacement?` | -- | As the chart's `TickLabels`, for this axis |
| `Grid` | `MokaChartGrid?` | -- | As the chart's `Grid`, for this axis |
| `Fit` | `MokaAxisLabelFit?` | -- | What labels that don't fit do |
| `WrapLines` | `int` | `2` | How many lines a wrapped label may take |
| `MaxLabelEm` | `double?` | -- | Labels longer than this, in em, are cut, with the whole text in a tooltip |
| `MinCategoryEm` | `double?` | 2.75 | With `Fit = Scroll`, the least width each category gets |
| `TickTemplate` | `RenderFragment<MokaAxisTick>?` | -- | Draws each tick. Its `Text` still decides the room it takes. Keep it a field |
| `TimeFormat` | `string?` | -- | A .NET date format for a time axis's ticks; day and month boundaries keep theirs |
| `Reverse` | `bool` | `false` | Runs the axis the other way |

## Labels that don't fit

Every label's width is estimated from its text, and the axis works out, for each strategy, the narrowest width at which
every label still shows. As the chart narrows past each of those widths the axis moves to the next stage, and the last
stage thins labels by rank: the last always shows, then the first, then those at positions divisible by the largest
power of two, so an evenly filled axis halves as it narrows.

| `MokaAxisLabelFit` | As the axis narrows |
|---|---|
| `Auto` | flat, then every other label on a second row, then at 45 degrees, then fewer at 45 degrees. Short labels, such as hours or month names, skip the turn and thin flat; only longer names are turned |
| `Thin` | flat, then fewer |
| `Stagger` | flat, then on two rows, then fewer, flat |
| `Rotate` | flat, then at 45 degrees, then fewer |
| `Wrap` | flat, then each label on up to `WrapLines` lines as wide as its slot, then fewer, flat |
| `Truncate` | flat, then each label cut to its slot. Every label always shows; the whole text is in its tooltip |
| `Scroll` | the categories scroll sideways under a value axis that stays put, each at least `MinCategoryEm` wide |

Labels at 45 degrees are cut at 12 em, so a long name can't make the axis taller than the plot. A cut label keeps its
whole text, in the page and in a tooltip. For a few dozen long names, a chart drawn in rows reads better than any of
these: names stay whole down the side.

```blazor-preview
@code {
    string[] _domains = ["telemetry.microsoft.com", "i.ytimg.com", "graph.facebook.com", "api.github.com",
        "time.cloudflare.com", "s3.eu-central-1.amazonaws.com", "updates.nas.local", "dns.google"];
    MokaBarSeries[] _series = [new() { Name = "Queries", Values = [18240, 12905, 9412, 7830, 6115, 4960, 2284, 980] }];
}

<MokaBarChart Labels="@_domains" Series="@_series" LabelFit="MokaAxisLabelFit.Auto" Look="MokaChartLook.Quiet" />
```

## Time axis

Give the chart times rather than strings. The axis picks its step from the span and the width (5 seconds to a year),
writes hours as the culture does ("14:00" or "2 PM"), marks each day's start in bold with a stronger line down the plot,
and writes dates for a month. Ticks of one step appear together as the chart widens, so the axis never shows 2:00 and
6:00 without 4:00.

```blazor-preview
@code {
    static DateTimeOffset _now = new(2026, 11, 11, 18, 0, 0, TimeSpan.Zero);
    DateTimeOffset[] _times = [.. Enumerable.Range(0, 97).Select(i => _now.AddMinutes(-30 * (96 - i)))];
    MokaLineSeries[] _series = [new() { Name = "Download", Values = [.. Enumerable.Range(0, 97).Select(i => 300 + 200 * Math.Sin(i / 8d))] }];
}

<MokaLineChart Times="@_times" Series="@_series" Unit="MokaChartUnit.Of("Mbps")" Look="MokaChartLook.Quiet" TimeZone="TimeZoneInfo.Utc" />
```

On Blazor Server, `TimeZoneInfo.Local` is the server's zone. Register a `TimeProvider` whose `LocalTimeZone` is the
user's, or pass `TimeZone`.

## Log axis and units

```blazor-preview
<MokaLineChart Labels="@(new[] { "00:00", "06:00", "12:00", "18:00" })"
               Series="@(new[] { new MokaLineSeries { Name = "Backup", Values = new[] { 62000d, 90000, 58000, 83000 } },
                                 new MokaLineSeries { Name = "DNS sync", Values = new[] { 40d, 62, 41, 70 } } })"
               YAxis="@(new MokaChartAxis { Scale = MokaAxisScale.Log, Unit = MokaChartUnit.Milliseconds })" />
```

A log axis writes each power of ten in the largest unit it is a whole number of: 10 ms, 100 ms, 1 s, 10 s, 100 s. Over
three decades or fewer it adds ticks at 2 and 5, which show only where the plot is tall enough.

## Value axis and grid

| `MokaChartGrid` | Draws |
|---|---|
| `Lines` | a line at every tick, the baseline as faint as the rest (what charts always drew) |
| `Hairlines` | lines one step off the surface and a slightly stronger baseline |
| `Baseline` | only the baseline: zero when the axis crosses it, else the bottom |
| `Dotted` | dotted lines and a solid baseline |
| `None` | nothing |

On a plot under 8 em tall every other tick of four divisions hides, so numbers never crowd. With `TickLabels = Inside`
the numbers sit over the plot, just above their line, and the plot takes the whole width.

## Rows

Charts drawn in rows have names down the side and the value axis on top, which stays put while the rows scroll in a
`MaxHeight`. Each name is one line, whole: the name column is as wide as the longest name, up to 40% of the chart, and
a name cut there keeps its whole text in a tooltip. On a chart too narrow to keep the names whole beside their rows (and
always under 24 em) the names move above their rows.

Past 200 rows they are drawn through Blazor's `Virtualize`, so ten thousand rows cost about twenty in the page. Every
row is exactly `RowHeight` pixels, the size `Virtualize` is told, so scrolling lands on the right rows. A static render
has no script for `Virtualize`, so there every row is drawn.

## Performance

The axes are worked out once per data change and cached, so a chart that renders again for its title or its state does
no axis work. The grid and the marks sit behind a version gate and aren't diffed again until the data changes.

## Accessibility

Tick labels are hidden from screen readers, which read the chart's label, the keyboard grid's cells and the table view
instead. Names in rows and cut labels keep their whole text in the page, and in a tooltip for the pointer.
