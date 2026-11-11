---
title: Bar Chart
description: Bars in columns or rows, grouped, stacked, as shares of a whole or either side of zero.
order: 57
---

# Bar Chart

`MokaBarChart` draws one or more named series as bars over a common value axis: side by side or stacked, in columns
or, with `Horizontal`, in rows. A category can be a whole split into shares (`Percent`), and bars can grow either side
of a zero line in the up and down colors (`Diverging`).

Left to its defaults on the Standard look it draws exactly what it always has: an svg of bars with native tooltips.
Any of the newer parameters, a chart look other than Standard, or the shared chart chrome (a title, the legend's
actions, a tooltip, the keyboard) draws it in HTML instead. HTML bars get exact caps and gaps from the look (24 px wide
at most and 2 px apart on Quiet), round only the end the data reaches, print values only where they fit, take hatching,
and lift with a band behind their column when pointed at or reached with the keyboard.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Series` | `IReadOnlyList<MokaBarSeries>?` | -- | The series to draw. Each has `Name`, `Values`, and optionally `Color`, `ColorValue`, `Colors` (one per value), `Key` and `Slot` |
| `Labels` | `IReadOnlyList<string>?` | -- | Category names, one per value |
| `Times` | `IReadOnlyList<DateTimeOffset>?` | -- | One time per value: each bar stands at its time, under the time axis's ticks |
| `Stacked` | `bool` | `false` | Whether the series pile onto one bar per category |
| `Horizontal` | `bool` | `false` | One row per category, the name whole beside its bar, or above it in a narrow chart |
| `Percent` | `bool` | `false` | Each category is one whole split into its series' shares. Stacks the series |
| `Diverging` | `bool` | `false` | Bars grow either side of a zero line in the up and down colors, by sign rather than series |
| `UpIsGood` | `bool` | `true` | With `Diverging`, whether above zero is the good direction. False draws it in the down color |
| `PositiveText`, `NegativeText` | `string?` | "Above zero", "Below zero" | With `Diverging`, the legend's names for the two directions |
| `Sort` | `MokaChartSort?` | -- | Order by total. Null sorts largest first when `Horizontal`, else keeps the order given |
| `ValueLabels` | `MokaValueLabels?` | -- | Where values print: `Tip`, `Inside`, `Largest` (the busiest bar only) or `None`. Null picks one |
| `BarEnds` | `MokaBarEnds?` | -- | `Rounded` at the data end, or `Square`. Null takes the look's |
| `MaxColumns` | `int` | `400` | The most columns drawn. More are merged by `ColumnAggregate` |
| `ColumnAggregate` | `MokaChartAggregate` | `Sum` | How merged columns combine: `Sum`, `Mean`, `Min`, `Max`, `First`, `Last` |
| `ShareText` | `Func<string, string, string>?` | -- | Writes a bar's share of the whole in the tooltip from the share and the total. Defaults to "38% of 104" |
| `YTicks` | `int` | `4` | Value axis divisions |
| `Min`, `Max` | `double?` | -- | Fixed ends of the value axis |
| `ShowLegend` | `bool` | `true` | Whether the series are listed |
| `ShowXLabels` | `bool` | `true` | Whether category names are drawn |
| `TickFormat` | `string` | `"0.##"` | .NET numeric format for the ticks on the Standard look |
| `Height` | `string?` | -- | A fixed height for the plot, such as `"160px"` or `"calc(8rem * var(--app-text-scale, 1))"`, whatever the width |
| `MaxHeight` | `string?` | -- | With `Horizontal`, the height the rows scroll in |
| `Size` | `MokaSize` | `Md` | The tallest the plot grows to: Sm 160, Md 240, Lg 320 px |

It also takes everything every cartesian chart shares: `Title`, `Subtitle`, `Look`, `Colors`, `Texture`, `Legend`,
`LegendActions`, `HiddenSeries`, `Highlight`, `Tooltip`, `Pinnable`, `Keyboard`, `Controls` (table view and export),
`State`, `Unit`, `Formatter`, `YAxis`, `XAxis`, `LabelFit`, `Grid`, `ValueSide`, `TargetLow`, `TargetHigh` and
`Annotations`. See [Chart axes](/components/chart-axes), [Chart frame](/components/chart-frame) and
[Chart interaction](/components/chart-interaction).

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
        new() { Name = "This week", Values = [412_000, 128_000, 506_000, 233_000, 97_000] },
        new() { Name = "Last week", Values = [388_000, 141_000, 470_000, 219_000, 76_000] }
    ];
}

<MokaBarChart Labels="@(new[] { "EU West", "EU North", "US East", "US West", "Asia" })" Series="_series"
              Look="MokaChartLook.Quiet" ValueLabels="MokaValueLabels.Largest" Title="Requests by region" />
```

On Quiet each bar is at most 24 px wide with 2 px between neighbours, rounded only at its top. Pointing anywhere in a
category's column lifts its bars and shows one tooltip for the group: every series, value first, with its share of the
column. `Largest` prints the busiest bar's value and nothing else.

## Horizontal bars

```blazor-preview
@code {
    string[] _domains = ["telemetry.microsoft.com", "i.ytimg.com", "graph.facebook.com", "api.github.com"];
    MokaBarSeries[] _queries = [new() { Name = "Queries", Values = [18240, 12905, 9412, 7830] }];
}

<MokaBarChart Horizontal Labels="_domains" Series="_queries" Look="MokaChartLook.Quiet" Title="Top domains" />
```

Rows sort largest first unless `Sort` says otherwise. Each value prints just past its bar, or inside the bar's end
when the plot has no room past it; long names stay whole beside the bars and move above them in a narrow chart. Past
200 rows an interactive chart draws only the rows in view, at one exact height, so 10,000 rows cost about forty. Give
it `MaxHeight` to scroll them in a box under a value axis that stays put.

## 100% stacked

```blazor-preview
@code {
    MokaBarSeries[] _protocols =
    [
        new() { Name = "HTTPS", Values = [62, 48, 21, 71] },
        new() { Name = "QUIC", Values = [24, 38, 2, 9] },
        new() { Name = "DNS", Values = [4, 6, 31, 12] },
        new() { Name = "Other", Values = [10, 8, 46, 8], ColorValue = "var(--moka-chart-muted)" }
    ];
}

<MokaBarChart Horizontal Percent Sort="MokaChartSort.None" Labels="@(new[] { "LAN", "Guest", "IoT", "VPN" })"
              Series="_protocols" Look="MokaChartLook.Quiet" />
```

Each share prints inside its segment where it fits and lives in the tooltip where it doesn't. The values stay as given:
the tooltip and the table show them beside their shares, written in `Unit` or by `Formatter`.

## Diverging

```blazor-preview
@code {
    MokaBarSeries[] _change = [new() { Name = "Change", Values = [71, 38, 12, -9, -21, -64] }];
}

<MokaBarChart Horizontal Diverging UpIsGood="false" PositiveText="Slower" NegativeText="Faster"
              Labels="@(new[] { "/api/sync", "/api/login", "/api/orders", "/api/thumbs", "/api/files", "/api/search" })"
              Series="_change" Unit="MokaChartUnit.Milliseconds" Look="MokaChartLook.Quiet" />
```

The axis is the same either side of zero, so a gray zero line runs through the middle. The legend names the two
directions, and hiding one hides those bars.

## Target band and emphasis

`TargetLow` and `TargetHigh` shade a target range behind the bars; on a look that prints values, the bars outside it
are the ones labelled. `Highlight` names one series to keep its color while the rest turn gray.

```razor
<MokaBarChart Series="_backups" Labels="_nights" TargetLow="20" TargetHigh="40" Look="MokaChartLook.Quiet" />
<MokaBarChart Series="_services" Labels="_hours" Stacked Highlight="Search" Look="MokaChartLook.Quiet" />
```

## A color per bar

`Colors` on a series gives each value's bar its own color, by position, such as the three busiest hours in the warning
color. An entry that is null or not a CSS color keeps the series' color. The bars stay the series' in every other way:
the legend shows the series' color, the hatching under a texture keeps the series' angle, hiding and highlighting act
on the series, and a highlight elsewhere still mutes them. Merged columns and `Diverging` bars draw in their usual
colors.

```razor
<MokaBarChart Series="_hours" Times="_times" Look="MokaChartLook.Quiet" />

@code {
    private MokaBarSeries[] _hours = [];

    protected override void OnParametersSet()
    {
        HashSet<int> busiest = [.. _values.Select((v, i) => (v, i)).OrderByDescending(x => x.v).Take(3).Select(x => x.i)];
        _hours = [new() { Name = "Traffic", Values = _values,
            Colors = [.. _values.Select((_, i) => busiest.Contains(i) ? "var(--moka-color-warning)" : null)] }];
    }
}
```

## Bars at their times

With `Times` the bars stand on a time axis at their times, lined up with its ticks, each as wide as the usual step
between times and never wider than the gap to a neighbour, so a missing hour leaves a gap. The axis reaches half a bar
past the first and last, unless `XAxis` pins its ends (`MinTime`, `MaxTime`). The tooltip heading writes each bar's time
as the axis's tooltips do (`Hour12`, `TooltipTimeFormat`), the table lists the times, and `MokaChartPoint.Time` and `X`
carry it. A sorted chart keeps its bars in even slots.

```razor
<MokaBarChart Series="_series" Times="_hours" XAxis="@(new MokaChartAxis { Hour12 = true })" />
```

## Too many categories

Columns can't be virtualized, so a long list is merged instead: past `MaxColumns`, or past one column for every three
pixels of the plot (which an interactive chart measures), neighbouring categories merge into one column by
`ColumnAggregate`. The axis labels a merged column by its first category, and its tooltip names the span, "1 Jan to 25
Jan". To keep every category, set `LabelFit="MokaAxisLabelFit.Scroll"`: the categories scroll sideways under a value axis
that stays put, at one element per bar. For a long list of names, `Horizontal` is usually the better chart.

| Categories | Columns | Rows (`Horizontal`) |
|---|---|---|
| 10 | 10 columns | 10 rows |
| 100 | 100 columns; labels thin, stagger or turn | 100 rows |
| 10,000 | merged to the plot's width, at most `MaxColumns`; or every one with `Scroll` | the rows in view, past 200 |

## The classic chart

Without any of the above, the chart is today's svg. Bars round the end they point to and keep a plain edge against the
axis, every bar carries a native tooltip naming its category, series and value, and stacked columns size the axis by
their totals.

Without `Height` the box keeps a 600 by 260 proportion, capped by `Size`; with `Height` it is that tall whatever its
width. The labels thin out as the chart narrows, keeping every 2nd, 4th or 8th label by the length of each, and always
the last; a blank label takes no place. The axis is a CSS size container, so this follows the chart's own width without
script.

```razor
<MokaBarChart Labels="_hours" Series="_series" Height="140px" />
```

## Performance

Bars are HTML written inline by the chart, never a component per bar, and the marks are redrawn only when the data or
the axis changes; a parent that renders with the same values draws nothing. Hovering costs no call to .NET until the
pointer moves to another column. Pass `DataVersion` with a long list so the chart compares a version rather than copying
the values.
