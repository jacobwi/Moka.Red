---
title: Heatmap
description: A calendar heatmap, the contribution-graph view - one square per day colored by how much happened.
order: 59
---

# Heatmap

`MokaHeatmap` lays a calendar out weeks across and days of the week down, coloring each day by
how much happened then - the contribution-graph view. The scale is the theme's primary mixed
toward transparent, so the busiest day is always the deepest.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Data` | `IReadOnlyDictionary<DateTime, int>?` | -- | How many days each date had. Matched by day; time is ignored |
| `Weeks` | `int` | `53` | Number of week columns to show |
| `EndDate` | `DateTime?` | -- | The last day shown. Defaults to today. The grid still ends on that week's Sunday |
| `MaxValue` | `int?` | -- | The count a day must reach for the deepest color. Null = the largest count in `Data` |
| `ShowMonthLabels` | `bool` | `true` | Whether month names run along the top |
| `ShowWeekdayLabels` | `bool` | `true` | Whether weekday names run down the side |
| `Label` | `string?` | -- | What the heatmap shows, for assistive technology |
| `Unknown` | `IReadOnlyCollection<DateTime>?` | -- | Days with no data, which aren't the same as days with nothing in them. Drawn hollow and hatched. Matched by day |
| `UnknownText` | `string` | `"No data"` | What an unknown day's tooltip says before its date |
| `Layout` | `MokaHeatmapLayout` | `Calendar` | `Grid` draws rows by columns from `GridValues`, such as hour by weekday |
| `GridValues` | `IReadOnlyList<IReadOnlyList<double>>?` | -- | The grid's rows, each holding its columns. `double.NaN` is a cell nobody knows about |
| `RowLabels`, `ColumnLabels` | `IReadOnlyList<string>?` | -- | A name per row and per column. Column names thin by container query, evenly spaced |
| `CellText` | `Func<int, int, double, string>?` | -- | A grid cell's tooltip from its row, column and value. Null writes "Tue 04: 412" |
| `MaxGridValue` | `double?` | -- | The value a grid cell must reach for the deepest step. Null = the largest value |
| `Ramp` | `MokaHeatmapRamp?` | -- | `Accent` (four transparency steps of the accent, today's) or `Steps` (five solid shades from the chart tokens). Null: `Steps` for the grid and for any chart look but Standard |
| `ShowScale` | `bool` | `false` | A key from `LessText` to `MoreText` under the cells |
| `LessText`, `MoreText` | `string` | `"Less"`, `"More"` | The key's ends |
| `Look` | `MokaChartLook?` | -- | The chart look, which picks the ramp. The original design always draws Standard |

`MokaHeatmap.HourByWeekday(times, zone)` counts times into seven rows, Monday first, of 24 hours; `WeekdayNames(culture)`
and `HourNames` label them.

## Basic

```blazor-preview
@code {
    Dictionary<DateTime, int> BuildData()
    {
        var random = new Random(7);
        var data = new Dictionary<DateTime, int>();
        for (DateTime day = DateTime.Today.AddDays(-364); day <= DateTime.Today; day = day.AddDays(1))
        {
            if (random.Next(100) < 65)
                data[day] = random.Next(1, 14);
        }
        return data;
    }
}

<MokaHeatmap Data="BuildData()" Weeks="26" />
```

## A fixed scale

Left alone, the busiest day in the data is the deepest. `MaxValue` pins the scale instead, so two
heatmaps beside each other read on the same one:

```razor
<MokaHeatmap Data="_lastWeek" Weeks="8" MaxValue="20" />
```

The grid runs Monday to Sunday and always includes the whole week the end day sits in. Days past
the end are drawn faint and carry no count, and every cell carries a native tooltip with its
count and date. Month and weekday names follow the current culture.

## Days without data

A day missing from `Data` is drawn as zero. When you know a day wasn't measured at all, such as before tracking
began or while a collector was down, list it in `Unknown`. Those days are drawn as hollow, hatched squares in the
theme's colors, so they never read as a quiet day, and their tooltip says "No data on" and the date. `UnknownText`
changes those words. A date that is in both `Data` and `Unknown` counts as known, and an unknown day past the end
is drawn as a future day.

```razor
<MokaHeatmap Data="_backups" Unknown="_collectorDown" UnknownText="Not measured" Weeks="12" />
```

## Hour by weekday

`Layout="MokaHeatmapLayout.Grid"` draws any rows by columns, which is how to show when things happen: blocked
connections, logins, backups. `HourByWeekday` counts a list of times into seven rows of 24 hours in the zone you give
(the local zone if none, which on Blazor Server is the server's). One hue, more is stronger, in five steps.

```blazor-preview
@code {
    IReadOnlyList<IReadOnlyList<double>> _blocked = MokaHeatmap.HourByWeekday(
        Enumerable.Range(0, 2000).Select(i => new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)
            .AddMinutes(i * 37 % 10080)), TimeZoneInfo.Utc);
}

<MokaHeatmap Layout="MokaHeatmapLayout.Grid" GridValues="_blocked" RowLabels="MokaHeatmap.WeekdayNames()"
             ColumnLabels="MokaHeatmap.HourNames" ShowScale Label="Blocked connections by hour" />
```

Cells stay square between 10 and 18 pixels. The column names keep every third hour, evenly spaced, and fewer as the
grid narrows. Below the width 24 columns need, a grid wider than it is tall turns on its side: the days run across,
the hours down, every third one named, which is how it reads on a phone. Each cell keeps a `title` tooltip, which a
grid of a few hundred cells can afford; a grid of thousands of cells is the wrong chart.

## The five-step ramp

`Ramp="MokaHeatmapRamp.Steps"` gives the calendar the grid's five solid shades (`--moka-chart-seq-1` to `-5`, with an
empty day in `--moka-chart-zero`), which read more evenly on any surface than the accent's transparency steps. A chart
look other than Standard picks it too. Without either, the calendar draws exactly what it always has.

```razor
<MokaHeatmap Data="_commits" Ramp="MokaHeatmapRamp.Steps" ShowScale />
```
