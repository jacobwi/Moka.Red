---
title: Charts
description: Which chart answers which question, what every chart shares, how they cope with large and live data, and how they read for keyboards and screen readers.
order: 54
---

# Charts

Every Moka.Red chart is built on one engine. A chart takes its look and series colors from the theme, sits in a frame
with a title, a toolbar, a legend, loading, empty and error states, a table view and export, answers the pointer and
the keyboard through one shared script, and lays out its axis labels by its own width. Each chart's page covers its own
parameters; this page says which chart to pick and what they all share.

## Which chart

| The question | Chart | Notes |
|---|---|---|
| How does a value move over time? | [Line Chart](line-chart.md) | One line or a few. Stacked areas for parts of a total, a band for a range, steps for values that hold |
| How does it move, inside a card or a table cell? | [Sparkline](sparkline.md), or [Stat](stat.md) with `Spark` | No axes, no frame. A stat puts the number first and the line under it |
| How do a few categories compare? | [Bar Chart](bar-chart.md) | Columns for a handful; `Horizontal` for long names or many rows |
| What share of the whole is each part? | [Donut Chart](donut-chart.md) | A ring for up to five parts; `Shape = Bar` for one share bar, `Waffle` for a hundred squares; `Percent` on a bar chart to compare wholes |
| How does a whole split into parts of parts? | [Treemap](treemap.md) | Nested tiles sized by value, such as disk use by folder |
| Where does a flow go? | [Sankey](sankey.md) | Sources to destinations, each band as wide as its volume. Keep it to four or so a side |
| Which side of zero, and by how much? | [Bar Chart](bar-chart.md) with `Diverging` | Gains one way, losses the other, with `UpIsGood` saying which is good |
| What changed between two moments, row by row? | [Dot Plot](dot-plot.md) with `Before` | A dumbbell per row. For two points in time on a line, a slope chart in [Line Chart](line-chart.md) |
| Values that don't start at zero, one per row? | [Dot Plot](dot-plot.md) | Uptime, latency, scores. `Stems` draws a lollipop |
| How are raw values spread? | [Histogram](histogram.md) | Counts per bucket with lines at chosen percentiles |
| How do several groups' spreads compare? | [Box Plot](box-plot.md) | The middle half, the median and whiskers per group |
| How do two measures relate? | [Scatter Chart](scatter-chart.md) | A dot per item, or a bubble whose area is a third measure |
| Open, high, low and close per period? | [Candlestick Chart](candlestick-chart.md) | With volume under the candles |
| When did jobs run, for how long, and which failed? | [Gantt Chart](gantt-chart.md) | One row per job on a shared clock, with a line at now |
| How many made it through each step? | [Funnel](funnel.md) | The share that went on between steps |
| When is it busy? | [Heatmap](heatmap.md) | `Grid` for hour by weekday or any rows by columns; the calendar for one square a day |
| How close is it to a target? | [Meter](meter.md) with `Target` | A bullet chart: bands, a bar and a target tick. [Gauge](gauge.md) for a dial |

## What every chart shares

- [Chart Looks](chart-looks.md): a look (`Standard`, `Quiet`, `Dense`, `Soft`, `Bold`) sets grid, ticks, legend,
  points, tooltip and keyboard together; a palette sets the series colors (`Reference` is checked for color-blind
  readers on light and dark); texture hatches each series; print decides how charts look on paper and under forced
  colors. Set them once on the theme, or per chart with `Look`, `Colors` and `Texture`.
- [Chart Frame](chart-frame.md): the title, a toolbar you can rebuild control by control, loading, empty and error
  states, a table of the values, export as PNG, SVG or CSV (also from code, `ExportAsync`), and, on charts that zoom
  along x, a range strip with presets.
- [Chart Legend](chart-legend.md): names and colors, or toggles that hide, isolate and highlight series. A chart with
  one series has none, since its title names it, unless you set `Legend`. It also stands alone as one row of toggles over
  a dashboard.
- [Chart Interaction](chart-interaction.md): tooltips (crosshair, nearest point, column, readout, or your own
  template), the keyboard, pins that show the change since the pin, sync groups that share a crosshair across charts,
  zoom by brush, wheel and pinch, and a live chart that holds still while someone points at it.
- [Chart Axes](chart-axes.md): labels that stagger, turn, wrap, cut or thin by the axis's own width with no script;
  time, log and unit axes; grid styles; charts drawn in rows.
- [Chart Annotations](chart-annotations.md): thresholds, target bands, event markers and windows.

Every word a chart writes comes from `Text` (`MokaChartText`), English when unset. `Formatter` writes values for ticks,
tooltips and the legend; keep it a field or a method, since a lambda that captures a local is new every render.
Tooltips and the table write each value at its own precision, so 26.4 GB never reads 26 GB because the ticks step by
10 GB. `OnPointClick` and `ActiveChanged` carry a `MokaChartPoint` with the series, the value, and its place along x
(`X`) and size (`Size`) where the chart has them.

```blazor-preview
<MokaLineChart Title="Requests" Subtitle="Last 6 hours" Look="MokaChartLook.Quiet"
               Controls="MokaChartControls.View | MokaChartControls.Export"
               Labels="@(new[] { "10:00", "11:00", "12:00", "13:00", "14:00", "15:00" })"
               Series="@(new[]
               {
                   new MokaLineSeries { Name = "Web", Values = new[] { 420d, 510, 640, 600, 720, 690 } },
                   new MokaLineSeries { Name = "API", Values = new[] { 180d, 210, 260, 300, 280, 330 } }
               })" />
```

## Today's charts keep drawing as they did

`MokaLineChart`, `MokaBarChart`, `MokaDonutChart`, `MokaHeatmap`, `MokaSparkline`, `MokaMeter` and `MokaStat` draw
exactly what they always have until you set one of their newer parameters, a title or toolbar, a tooltip, the keyboard,
or a look other than Standard. On `MokaTheme.Dark` and `MokaTheme.Light` looks and palettes always resolve to Standard,
so those apps keep their charts; a feature such as `Stacked` or `Horizontal` still works there, in the original colors.
On the new design (`Red`, `Gate`, `Crisp`, `Matrix`) the theme's chart choices also default to today's charts, so an app
sees no change until it asks.

## Large and live data

A chart compares what it is given with what it last drew, so a parent that renders every second with the same data
costs the chart no render at all. Past about 100,000 values, pass `DataVersion` and bump it whenever the data changes:
the chart then compares the version instead of copying and comparing every value.

A line never draws more than about twice the plot's width in points. Past that it keeps each pixel's lowest and highest
value (`Downsample="MokaDownsample.Lttb"` keeps the points that hold the shape instead), so every peak survives, and
tooltips still read real samples. Zooming cuts the data to the window first, so a minute out of a day of samples draws
raw values. The pointer never reaches the server while it moves: the browser finds the point under it and asks only when
that point changes.

Measured on Blazor Server in a browser, two series of a day's times, from the change to the new line on the page:

| Points a series | What is drawn | Time | Sent to the browser | Hover |
|---|---|---|---|---|
| 10 | every point, a dot on each | about 20 ms | about 8 KB | the points' positions fetched once, on the first hover |
| 1,000 | every point; past 400 drawn, dots only under the pointer | about 25 ms | about 56 KB | the same |
| 100,000 | each pixel's lowest and highest value, about twice the plot's width in points | about 55 ms | about 73 KB | then about 1.2 KB each time the point changes |
| 1,000,000 | the same | about 250 ms, most of it making the data; pass `DataVersion` | about 77 KB | the same |

The other charts at size:

| Chart | 10 | 1,000 | 100,000 and more |
|---|---|---|---|
| Bar chart, columns | one column each | merged to one column per 3 px of width, or to `MaxColumns` (400), adding up with `ColumnAggregate`; `LabelFit = Scroll` keeps every column and scrolls | 10,000 categories merged to 295 columns at about 900 px |
| Rows (horizontal bars, dot plot, box plot, gantt, funnel) | every row | past 200 rows only the rows in view are drawn, about 31 row elements whatever the count, under a value axis that stays put (`MaxHeight`) | the same |
| Scatter | one shape a series | the same | past 2,000 points a series, one dot per 2 px (100,000 points drew 8,959 dots); past `MaxBubbles` the largest bubbles are kept, with a note |
| Histogram | values counted in one pass | the same | a million values per change |
| Box plot | quantiles from the values | the same | pass a precomputed `Summary` per group; the dots are capped at `MaxPoints` a row |
| Candlestick | one candle each | merged past one candle per 3 px: first open, highest high, lowest low, last close, summed volume | 2,000 candles merged to 143 |
| Treemap, Sankey | every part | past `MaxTiles` (150) or `MaxNodesPerSide` (6) the rest fold into Other | the same |

A long axis of labelled categories is thinned to what can show before its labels are laid out, so a million categories
take about a quarter of a second rather than seconds. Rows scroll in a box with `MaxHeight`; columns don't scroll that
way, so a long list of categories reads better as `Horizontal` rows than as columns.

## Accessibility

- Each chart has an accessible name: set `Label` to say what it shows. Without one a chart names itself by its kind,
  and most add a summary of their data, such as "Dot plot: Router 99.99%, DNS 99.97%". A chart with nothing to operate
  is an image; one with a keyboard grid, a toolbar or legend toggles is a figure.
- With `Keyboard` (on in every look but Standard) the chart is one tab stop. The arrow keys move along a line, between
  lines, bars or rows; Home, End, Page Up and Page Down jump; Enter and Space pin; Escape lets go. Each move is read
  out, such as "Download, 23:30, 41 Mbps". See [Chart Interaction](chart-interaction.md).
- The table view (`Controls` with `MokaChartControls.View`) shows every value in a table, paged, and Escape goes back
  to the chart. CSV export saves the same values.
- The `Reference` palette keeps its colors apart for the common kinds of color blindness, and `Texture` hatches each
  series at its own angle so the series tell apart without color. `MokaChartPrint` adds hatching or ink for print and
  forced colors. See [Chart Looks](chart-looks.md).
- Statuses don't rest on color alone: a gantt bar carries an icon and a word, a stat with `UpIsGood="false"` tells a
  screen reader whether its trend is better or worse, and a meter past `WarnAt` or `CriticalAt` names its state.
- Every word a chart writes comes from `MokaChartText`, so it can be translated. The one exception is the chart's
  kind in the name it gives itself ("Line chart"), so set `Label` in an app that isn't in English.
