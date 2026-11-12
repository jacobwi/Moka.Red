---
title: Line Chart
description: Lines over a value axis, stacked areas, bands, steps, slopes, small multiples and indexed growth, from ten points to a million.
order: 56
---

# Line Chart

`MokaLineChart` draws one or more named series as lines over a common value axis. The plot fills the width of its
box, while the labels, the dots and the line widths keep their size and shape, and the axis labels thin out as the
chart narrows. It stacks series into areas, draws a band around a line, holds values as steps, names lines at their
ends, splits into one small chart per series, indexes series to a common start, and draws a live window that holds
still while someone reads it. Series of a million points draw the lowest and highest value of each pixel, so every
peak stays.

## Two designs

A chart with today's parameters and the Standard look draws exactly what it always has. Any of the parameters below
under "New", a chart look other than Standard (`Look`, or `MokaTheme.WithCharts`), or a title, toolbar or legend
option, moves it onto the shared chart frame: a header and toolbar, a legend that hides and isolates series, the
loading, empty and error states, a table view and export ([Chart Frame](chart-frame.md)); axes that stagger, rotate
and thin their labels, time and log axes, thresholds, bands, events and windows ([Chart Axes](chart-axes.md),
[Chart Annotations](chart-annotations.md)); and tooltips, the keyboard, pins, sync groups and zoom
([Chart Interaction](chart-interaction.md)).

On the original design (`MokaTheme.Dark` and `Light`) a look always resolves to Standard, so those apps keep their
charts, while a feature such as `Stacked` still works there in the original colors.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Series` | `IReadOnlyList<MokaLineSeries>?` | -- | The series to draw. A `double.NaN` is a gap |
| `Labels` | `IReadOnlyList<string>?` | -- | Category names along the x-axis, one per value |
| `Times` | `IReadOnlyList<DateTimeOffset>?` | -- | One time per value. The x-axis becomes a time axis and x is proportional to time |
| `ShowPoints` | `bool` | `true` | Whether dots are drawn. Off wins over `Points` |
| `ShowArea` | `bool` | `false` | Whether the area under each line is filled |
| `YTicks` | `int` | `4` | Number of y-axis divisions to draw grid lines for |
| `Min`, `Max` | `double?` | -- | A fixed floor and ceiling for the y-axis |
| `ShowLegend` | `bool` | `true` | Whether the series are listed in a legend |
| `ShowXLabels` | `bool` | `true` | Whether the x-axis labels are drawn |
| `TickFormat` | `string` | `"0.##"` | .NET numeric format for the y-axis labels on the Standard look |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Size` | `MokaSize` | `Md` | The tallest the plot grows to. Below that it keeps the look's proportion |
| `Height` | `string?` | -- | A fixed height for the plot, such as `"230px"` or a `calc()`, `var()`, `min()`, `max()` or `clamp()` of lengths, whatever the width |
| `Formatter` | `Func<double, string>?` | -- | Writes a value for the ticks, tooltips and legend. Keep it a field or a method |
| `TooltipLabel` | `Func<int, string>?` | -- | Writes the heading of a point's tooltip from its index |
| `ShowCurrent` | `bool` | `false` | Whether the legend shows each series' latest value |
| `ShowPeak` | `bool` | `false` | Whether the highest value any series reaches is written under the chart |
| `PeakLabel` | `string` | `"Peak"` | The word before the peak |
| `AutoScale` | `bool` | `false` | Rounds the ends of the y-axis to round steps |
| `ScaleUnits` | `IReadOnlyList<double>?` | -- | With `AutoScale` and a `Formatter`, the units the steps are rounded in, such as 1, 1024 and 1048576 |
| `SuggestedMax` | `double?` | -- | The least the top of the y-axis reaches. Higher values still raise it |
| `Smooth` | `bool` | `false` | Smooth curves that never bulge past a peak. The same as `Curve="MokaLineCurve.Smooth"` |
| `ShowLastPoint` | `bool` | `false` | A ringed dot on each series' latest value |
| `Live` | `bool` | `false` | Slides the lines one step left when the window moves, instead of jumping |
| `LiveInterval` | `TimeSpan` | 1 s | How long the slide takes |
| `EmptyText` | `string?` | -- | What the plot says while there are no values |

New:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Stacked` | `bool` | `false` | Stacks the series: the top edge is the total, each band one part. The tooltip adds the total |
| `Curve` | `MokaLineCurve?` | the look's | `Straight`, `Smooth`, or `Step`, which holds each value until the next |
| `Points` | `MokaChartPoints?` | the look's | `None`, `Hover` (a dot where you point and at each line's end) or `Always`. Past 400 drawn values a line behaves as `Hover` |
| `Area` | `MokaChartArea?` | the look's when `ShowArea` | `None`, `Flat` (a tint) or `Fade` (a tint that fades to nothing at the baseline) |
| `DirectLabels` | `MokaDirectLabels?` | the look's for up to four lines | `Names`, `Values` or `Both`, written beside each line's end |
| `Highlight` | `string?` | -- | One series' key in its color, the rest muted and drawn under it, named at its end |
| `HighlightSeries` | `IReadOnlyCollection<string>?` | -- | Several series to keep in color |
| `Split` | `bool` | `false` | One small chart per series, all on one scale, with one crosshair through them all |
| `SplitColumns` | `int?` | -- | The most panels side by side. None is narrower than about 13em |
| `IndexTo` | `double?` | -- | Divides each series by its first value and multiplies by this, usually 100 |
| `BreakGapsOver` | `TimeSpan?` | -- | With `Times`, two samples further apart than this break the line |
| `PauseWhilePointing` | `bool` | `false` | With `Live`, holds still while pointed at or focused, counting what waits |
| `Downsample` | `MokaDownsample` | `Auto` | How a long series is cut to the plot's width: `MinMax` (the default), `Lttb` or `None` |
| `DataVersion` | `long?` | -- | Compare the data by reference and this version instead of copying it. Use it past about 100,000 values |
| `Trend` | `MokaChartTrend` | `None` | A thin dashed trend over each series: `MovingAverage` or `Linear`. Listed in the tooltip and the table |
| `TrendWindow` | `int` | `7` | How many values each mean of a moving average covers, the value itself included |

Every chart also takes `Title`, `Subtitle`, `Look`, `Legend`, `LegendActions`, `HiddenSeries`, `Tooltip`, `Keyboard`,
`Pinnable`, `SyncGroup`, `Controls`, `View`, `State` and the rest of [Chart Frame](chart-frame.md) and
[Chart Interaction](chart-interaction.md), and every cartesian chart `XAxis`, `YAxis`, `Unit`, `Grid`, `TickLabels`,
`ValueSide`, `Annotations`, `TargetLow`, `TargetHigh`, `Zoom`, `Range` and `RangePresets`
([Chart Axes](chart-axes.md), [Chart Annotations](chart-annotations.md)).

### MokaLineSeries

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | Shown in the legend, the tooltips and at the line's end |
| `Values` | `IReadOnlyList<double>` | One per category or time. `double.NaN` is a gap |
| `Color` | `MokaColor?` | A color by meaning. Null takes the palette |
| `Key` | `string?` | What identifies the series for hiding, highlighting and colors. Null uses `Name` |
| `Slot` | `int?` | The palette slot, counting from 1, so the series keeps its color across charts |
| `ColorValue` | `string?` | Any CSS color or token, such as `var(--moka-chart-3)` |
| `Low`, `High` | `IReadOnlyList<double>?` | A band around the line, one pair per value, such as the 5th and 95th percentile |
| `Curve` | `MokaLineCurve?` | This line's own curve. Null takes the chart's |
| `CompareTo` | `string?` | The key of the series this one is a comparison for, such as last week beside this week. Drawn faint and dashed in that series' color |

## Basic

```blazor-preview
<MokaLineChart Labels="@(new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })"
               Series="@(new[] { new MokaLineSeries { Name = "Revenue", Values = new[] { 12d, 18, 15, 22, 28 } } })" />
```

## A look

`Look` picks how the chart is drawn: `Quiet` has hairline grids, round ticks, the legend on top, names at the line ends,
a crosshair tooltip and the keyboard. See [Chart Looks](chart-looks.md) for the others.

```blazor-preview
<MokaLineChart Title="Traffic" Look="MokaChartLook.Quiet" Unit="MokaChartUnit.Of("Mbps")"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "Download", Values = new[] { 40d, 35, 80, 120, 140, 90 } },
                   new MokaLineSeries { Name = "Upload", Values = new[] { 12d, 10, 30, 42, 38, 25 } }
               })" />
```

## Stacked area

`Stacked` piles the series up, so the top edge is the total and each band is one part, such as bandwidth by zone. The
tooltip lists every series, top first, and the total. A gap is a gap in its own band and nothing for the bands above.

```blazor-preview
<MokaLineChart Title="Bandwidth by zone" Look="MokaChartLook.Quiet" Stacked="true"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "LAN", Values = new[] { 120d, 110, 140, 200, 180, 130 } },
                   new MokaLineSeries { Name = "Wi-Fi", Values = new[] { 90d, 85, 100, 110, 140, 120 } },
                   new MokaLineSeries { Name = "IoT", Values = new[] { 12d, 12, 14, 15, 13, 12 } }
               })" />
```

## Range band

`Low` and `High` on a series shade a band around its line, such as the median inside the 5th to 95th percentile. The
tooltip adds the spread, and the value axis reaches the band.

```blazor-preview
<MokaLineChart Title="Response time" Look="MokaChartLook.Quiet" Unit="MokaChartUnit.Milliseconds"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries
                   {
                       Name = "Median", Values = new[] { 92d, 95, 120, 136, 118, 100 },
                       Low = new[] { 48d, 50, 62, 70, 60, 52 }, High = new[] { 170d, 175, 230, 330, 240, 180 }
                   }
               })" />
```

## Step line

`Curve="MokaLineCurve.Step"` holds each value until the next, for counts that jump rather than drift: instances
running, devices online. A sloped line between 2 and 5 would claim there were 3.5 instances for a while.

```blazor-preview
<MokaLineChart Title="Instances running" Look="MokaChartLook.Quiet" Curve="MokaLineCurve.Step"
               DirectLabels="MokaDirectLabels.Values"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[] { new MokaLineSeries { Name = "Instances", Values = new[] { 2d, 3, 6, 8, 5, 3 } } })" />
```

## Slope chart

Two labels make a before and after. `HighlightSeries` keeps the ones that got worse in color, and `DirectLabels`
names every line at its end, pushing labels apart where values sit close.

```blazor-preview
<MokaLineChart Title="Error rate around release 2.4" Look="MokaChartLook.Quiet"
               Labels="@(new[] { "Before", "After" })" HighlightSeries="@(new[] { "Search" })"
               DirectLabels="MokaDirectLabels.Both" Points="MokaChartPoints.Always"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "Sync", Values = new[] { 3.1, 1.8 } },
                   new MokaLineSeries { Name = "Orders", Values = new[] { 2.6, 1.0 } },
                   new MokaLineSeries { Name = "Search", Values = new[] { 0.9, 1.4 } },
                   new MokaLineSeries { Name = "Files", Values = new[] { 0.4, 0.3 } }
               })" />
```

## Emphasis

`Highlight` draws one series in its color, mutes the rest and draws them underneath, and names it at its end. When the
story is one service, six colors bury it.

```blazor-preview
<MokaLineChart Title="p95 latency by service" Look="MokaChartLook.Quiet" Highlight="Search"
               Unit="MokaChartUnit.Milliseconds"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "Search", Values = new[] { 150d, 160, 180, 410, 220, 160 } },
                   new MokaLineSeries { Name = "Auth", Values = new[] { 70d, 90, 100, 85, 80, 72 } },
                   new MokaLineSeries { Name = "Files", Values = new[] { 120d, 130, 150, 170, 140, 125 } }
               })" />
```

## Small multiples

`Split` draws one small chart per series, all on one scale so their heights compare. Pointing at one panel moves the
crosshair through all of them, and each panel's header says its value there, or its latest. `SplitColumns` caps how
many sit side by side; a phone shows one column. Hiding a series in the legend removes its panel.

```blazor-preview
<MokaLineChart Title="Bandwidth by zone" Look="MokaChartLook.Quiet" Split="true" SplitColumns="2"
               Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "LAN", Values = new[] { 120d, 110, 140, 200, 180, 130 } },
                   new MokaLineSeries { Name = "Wi-Fi", Values = new[] { 90d, 85, 100, 110, 140, 120 } },
                   new MokaLineSeries { Name = "Guest", Values = new[] { 30d, 28, 40, 45, 60, 42 } },
                   new MokaLineSeries { Name = "IoT", Values = new[] { 12d, 12, 14, 15, 13, 12 } }
               })" />
```

## Indexed to 100

`IndexTo="100"` divides each series by its first value and multiplies by 100, so measures of very different size,
such as users and sessions, compare by growth on one axis. The axis counts in plain numbers from just under the lowest
value, a line's end says how much it grew ("+73%"), and the tooltip still says each raw value. Use it, or two charts
in one `SyncGroup`, instead of a second y-axis.

```blazor-preview
<MokaLineChart Title="Users and sessions" Look="MokaChartLook.Quiet" IndexTo="100"
               DirectLabels="MokaDirectLabels.Both"
               Labels="@(new[] { "1 Oct", "8 Oct", "15 Oct", "22 Oct", "29 Oct" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "Sessions", Values = new[] { 18400d, 20500, 23900, 27800, 31832 } },
                   new MokaLineSeries { Name = "Users", Values = new[] { 1240d, 1330, 1420, 1515, 1612 } }
               })" />
```

## Compared with another period

A series with `CompareTo` set to another series' key (its `Key`, else its `Name`) is a comparison for it, such as last
week's values beside this week's on the same times. It is drawn faint and dashed in that series' color, unless it
sets a color of its own, with no dots, area or name at its end, and under the series so the current values stay on
top. The tooltip lists it as a row of its own and says, under the series it belongs to, how far that series is from
it: "+12% vs Last week" (`MokaChartText.Versus`), or the plain difference where the comparison is zero. The legend
lists it with a dashed key, the table view gives it a column, and a tooltip template reads it as
`MokaChartPoint.Comparison`.

A comparison counts in the value axis's range like any series, so neither line is ever cut off. It hides with the
series it belongs to, stays lit while that series is highlighted, sits in the same panel of small multiples, and is
never stacked. A key that names no series, or one that is itself a comparison, leaves it an ordinary series.

```razor
<MokaLineChart Times="_times" Look="MokaChartLook.Quiet" Series="@(new MokaLineSeries[]
{
    new() { Name = "This week", Values = _thisWeek },
    new() { Name = "Last week", Values = _lastWeek, CompareTo = "This week" }
})" />
```

## Trends

`Trend` draws a thin dashed line over each series in its color: `MovingAverage`, the mean of each value and the ones
before it over `TrendWindow` values (a gap doesn't count, and the line starts once the window has filled), or
`Linear`, the straight line nearest to the values by least squares, fitted against the times on a time axis. Each is
worked out from all of the series, so zooming doesn't move it, and from the values as drawn, so it follows `IndexTo`.
The tooltip and the table list it after its series ("LAN, 7-point average", "LAN trend": `MokaChartText.AverageOf`
and `TrendOf`), and a tooltip template reads it as `MokaChartPoint.Trend`. A stacked chart draws none, since its bands
sit on each other, and a comparison gets none of its own. The math is `MokaChartStats.MovingAverage` and `LinearFit`
in Moka.Red.Core.

```razor
<MokaLineChart Times="_times" Series="_series" Trend="MokaChartTrend.MovingAverage" TrendWindow="24"
               Look="MokaChartLook.Quiet" />
```

## Names at the line ends

`DirectLabels` writes names, latest values or both beside each line's end, in text colors, never the series color.
Labels whose ends crowd push apart and keep a short leader back to their line. The stylesheet places them in the plot's
own height, so they stay apart however the chart is resized, with no script. When the plot is too short for all of
them, or the chart narrower than about 24em, they hide together and the legend names the lines. At most eight lines
are named; a look names its lines on its own only up to four.

## Points and areas

`Points` puts dots nowhere (`None`, not even where you point), where you point and at each line's end (`Hover`), or
on every value (`Always`, a dot ringed in the surface's color so it reads where lines cross). `Area` fills under each
line with a flat tint or a tint that fades to nothing at the baseline. A line with a band takes the band as its fill.

```blazor-preview
<MokaLineChart Look="MokaChartLook.Quiet" Points="MokaChartPoints.Always" Area="MokaChartArea.Fade"
               Labels="@(new[] { "Mon", "Tue", "Wed", "Thu", "Fri" })"
               Series="@(new[] { new MokaLineSeries { Name = "Revenue", Values = new[] { 12d, 18, 15, 22, 28 } } })" />
```

## Gaps and windows

`double.NaN` in a series marks a value that's missing: the line and the area break there. With `Times`,
`BreakGapsOver` breaks the line where samples stopped coming, rather than drawing straight across the hole. A
`MokaChartWindow` annotation hatches a span, such as maintenance, and the values inside it are gaps.

```razor
<MokaLineChart Times="_times" Series="_series" BreakGapsOver="TimeSpan.FromMinutes(20)"
               Annotations="@(new MokaChartAnnotation[] { new MokaChartWindow(_from, _to) { Label = "Maintenance" } })"
               Look="MokaChartLook.Quiet" />
```

## Live

For a rolling window, such as a transfer rate sampled once a second, pass the whole window each time, padded at the
start with `double.NaN` until it fills. With `Live`, when the values change and the count stays the same, the lines
start one step to the right and slide home over `LiveInterval`, so the chart scrolls rather than jumps. The slide is a
CSS animation, so it costs no renders, and it stands still for people who asked for reduced motion.

`PauseWhilePointing` holds the chart still while the pointer or the keyboard focus is on it, so the value being read
doesn't slide away. A chip says "Paused · 3 new", and the chart catches up when it is let go.

```razor
<MokaLineChart Series="_series" Labels="_labels" Live="true" PauseWhilePointing="true"
               Look="MokaChartLook.Quiet" Points="MokaChartPoints.Hover" EmptyText="Waiting for traffic" />
```

### Streaming into a long window

A live chart keeps a copy of what it last drew and an index over each series' values, a lowest and highest value per
block of 32 samples (`MokaMinMaxIndex` in Moka.Red.Core). When the data comes back grown at its end, or slid along
(dropped from its start, added at its end), every series and the times by the same amount, the chart finds that out by
comparing with its copy bit for bit, reads only what arrived, and picks the points to draw from the index, so a long
window costs about what its new samples cost. Anything else, a value changed in the middle, a series that came or
went, a series cut by `Lttb`, starts over and reads everything, as before. Either way the picks are exactly the ones a
full pass would make, so the chart draws the same markup for the same data.

It works with or without `DataVersion`, and with a list the app changes in place and hands back with a new version,
which is the cheapest way to feed it:

```razor
<MokaLineChart Live="true" Times="_times" Series="_series" DataVersion="_version" Look="MokaChartLook.Quiet"/>

@code {
    private readonly List<DateTimeOffset> _times = [];
    private readonly List<double> _values = [];
    private IReadOnlyList<MokaLineSeries> _series = [];
    private long _version;

    protected override void OnInitialized() => _series = [new() { Name = "Samples", Values = _values }];

    // From a timer or a socket, so it hops onto the renderer's thread first.
    private Task ArrivedAsync(DateTimeOffset at, double value) => InvokeAsync(() =>
    {
        _times.Add(at);
        _values.Add(value);
        _version++;
        StateHasChanged();
    });
}
```

Measured on Blazor Server in Release, one sample a second, the window sliding (the series' rebuild, then the server's
whole build and render of the page around it):

| Window | Rebuild before | Rebuild now | Build and render before | Now |
|---|---|---|---|---|
| 10,000 | 0.64 ms | 0.54 ms | 5.6 ms | 4.6 ms |
| 100,000 | 1.5 ms | 0.54 ms | 5.9 ms | 3.6 ms |
| 1,000,000 | 11.9 ms | 2.6 ms | 22.7 ms | 5.8 ms |

A window that only grows measured 1.98 ms against 11.7 ms at a million. What reaches the browser doesn't change: each
tick sends the new line, one path of about twice the plot's width in points, about 16 KB of path data, since Blazor
sends a changed attribute whole.

## Large data

A line never draws more points than about twice the plot's width. Past that, each pixel's lowest and highest value
is kept (`Downsample="MokaDownsample.Lttb"` keeps the points that hold the shape instead), so every peak and dip
survives, and tooltips still read real samples. The chart asks the browser for the plot's width once a series is
longer than 1,600 values. Zooming in (a brush, the wheel, the range strip or a preset, see
[Chart Interaction](chart-interaction.md)) cuts the data to the window first, so a minute out of a million points a
day draws raw samples.

Measured on Blazor Server in a browser, two series of a day's times, from changing the data to the new lines on the
page, round trip included:

| Points a series | Drawn | Change to page | Sent to the browser |
|---|---|---|---|
| 10 | all | about 20 ms | about 8 KB |
| 1,000 | all | about 25 ms | about 56 KB |
| 100,000 | about 1,800 | about 55 ms | about 73 KB |
| 1,000,000 | about 1,800 | about 250 ms, most of it making the data | about 77 KB |

The chart's own work is a small part of that: rendered alone, a million points take about 37 ms the first time and
about 25 ms a change with `DataVersion`. What reaches the browser stops growing past the plot's width, since each line
is one path of about twice the plot's width in points. Once the pointer has fetched the points' positions, each change
of the point under it sends about 1.2 KB. A parent that renders
every second with the same data costs the chart no render at all, since it compares what it is given with what it
drew. Past about 100,000 values pass a `DataVersion` and bump it when the data changes, so the chart compares the
version rather than copying and comparing every value. Without labels or times, a series that long counts its values
along a number axis rather than laying out a label for each.

## Category labels

The labels along the x-axis thin out with the chart's own width, with no script. Labels that don't fit stagger, turn,
wrap or cut; see [Chart Axes](chart-axes.md). A blank label takes no place on the axis, so you can label only the
marks you want:

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

Short axis labels often say too little in a tooltip. `TooltipLabel` writes a point's heading from its index instead,
so the axis can read "12:05" while the tooltip reads "Sun 8 Nov 12:05:30". On a time axis, `XAxis.TooltipTimeFormat`
and `XAxis.Hour12` do the same without code (see [Chart Axes](chart-axes.md)).

## Width and height

The plot stretches to fill its box, so a chart in a wide card has no empty bands beside it. What mustn't stretch is
kept out of the stretch: the tick labels are text beside and under the plot, the lines and grid keep their width,
and the dots stay round.

- Without `Height` the plot keeps the look's proportion (600 by 260 on Standard), capped by `Size` (Sm 160, Md 240,
  Lg 320 px on the classic chart; Sm 9, Md 14, Lg 19 rem for the plot alone on the modern one).
- With `Height` the plot is that tall whatever its width. Use it for a dashboard card with a set height. It takes a
  length or an expression, so `Height="calc(8rem * var(--app-text-scale, 1))"` grows with an app's text size.

```razor
<MokaCard Title="Throughput">
    <MokaLineChart Series="_series" Labels="_labels" Height="180px" />
</MokaCard>
```

On the classic chart, with `ShowCurrent` the accessible name carries the latest values too, such as "Throughput. Down
1.2 MiB/s, Up 300 KiB/s, Peak 3.9 MiB/s", so a screen reader hears what the legend shows. The modern chart says them
in its legend, its table view and the keyboard's cells.
