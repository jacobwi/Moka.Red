---
title: Chart Frame
description: What every chart has around its plot - the title, a toolbar you can rebuild, the legend, loading, empty and error states, a table view, export, and a range strip.
order: 160
---

# Chart Frame

Every chart draws the same frame around its plot: a header with the title, the toolbar and your own actions; the legend in its place; loading, empty and error states drawn over the plot at its own size; a table of the values; export to PNG, SVG and CSV; and, on a chart with an x axis, range presets and a range strip. The parameters below are on every chart.

## Setup

```razor
@using Moka.Red.Data.Chart
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | - | The title over the chart |
| `Subtitle` | `string?` | - | A line under the title |
| `TitleContent` | `RenderFragment?` | - | Drawn in place of the title text |
| `Actions` | `RenderFragment?` | - | Your own controls, at the end of the toolbar. They fold with it |
| `Controls` | `MokaChartControls?` | the look's (none) | The toolbar's controls: `View`, `ExportPng`, `ExportSvg`, `ExportCsv` (`Export` is all three), `Ranges`, `ResetZoom`, `Legend`, or `All` |
| `ToolbarControl` | `RenderFragment<MokaChartControlContext>?` | - | Draws each control, in its place. Return `context.Default` to keep one, wrap it, or return something else |
| `Legend` | `MokaChartLegendPosition?` | the look's | `Bottom`, `Top`, `Right`, `TitleRow` or `None` |
| `LegendKey` | `MokaChartLegendKey?` | the look's | `Dot`, `Mark` or `Pill` |
| `LegendActions` | `MokaChartLegendActions?` | the look's | `Highlight`, `Hide`, `Isolate` |
| `HiddenSeries` | `IReadOnlyCollection<string>?` | - | The keys of the hidden series. Two-way bindable |
| `View` | `MokaChartView` | `Chart` | The chart or a table of its values. Two-way bindable |
| `State` | `MokaChartState` | `Auto` | `Auto` (empty without data, else ready), `Ready`, `Loading`, `Empty`, `Error` |
| `EmptyText` | `string?` | "No data" | What the empty state says |
| `EmptyContent` | `RenderFragment?` | - | Drawn in place of the empty state |
| `ErrorText` | `string?` | "Couldn't load the data." | What the error state says |
| `ErrorContent` | `RenderFragment?` | - | Drawn in place of the error state |
| `OnRetry` | `EventCallback` | - | Raised by Try again, which shows only when this has a listener |
| `ExportName` | `string?` | the title | The exported file's name without its extension. Else the title or label, as `traffic-today` |
| `Text` | `MokaChartText?` | English | Every word the frame writes and says |

On a chart with an x axis (line and bar charts):

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Zoom` | `MokaChartZoom?` | - | `Strip` draws the range strip under the plot |
| `Range` | `MokaChartRange?` | - | The span the chart is zoomed into. Two-way bindable |
| `RangePresets` | `IReadOnlyList<MokaChartRangePreset>?` | - | The spans `Ranges` offers on a time axis, such as `new("24 h", TimeSpan.FromHours(24))` |
| `RangePreset` | `string?` | - | The label of the preset in force. Two-way bindable |

### Methods

| Method | Does |
|--------|------|
| `ExportAsync(MokaChartControls format)` | Saves the chart as one of `ExportPng`, `ExportSvg` or `ExportCsv`, from anywhere in the app. A failure shows in the frame |

## The toolbar

Each control is a flag on `Controls`, so a chart turns on exactly what it needs. The toolbar keeps to one row: when the chart is narrow, what doesn't fit drops out whole, the legend first, then the range presets and your actions, and a button opens the toolbar onto more rows with the same controls in place. The view switch and the Export menu stay longest. Under 24em of chart width the title row stacks, title over toolbar.

```blazor-preview
<MokaLineChart Series="_series" Labels="_labels" Title="Traffic today" Subtitle="Mbps"
               Controls="MokaChartControls.View | MokaChartControls.Export">
    <Actions>
        <MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm">Refresh</MokaButton>
    </Actions>
</MokaLineChart>

@code {
    private readonly string[] _labels = ["00", "04", "08", "12", "16", "20"];

    private readonly MokaLineSeries[] _series =
    [
        new() { Name = "LAN", Values = [120, 90, 310, 420, 380, 260] },
        new() { Name = "Wi-Fi", Values = [80, 60, 140, 210, 230, 190] }
    ];
}
```

### Replacing or wrapping a control

`ToolbarControl` is asked for each control the chart draws, with a `MokaChartControlContext`:

| Member | Type | Description |
|--------|------|-------------|
| `Which` | `MokaChartControls` | The control: `View`, `Ranges`, `ResetZoom`, `Legend`, or the export formats in the menu |
| `Enabled` | `bool` | Whether it can be used now. Reset zoom is disabled while nothing is zoomed |
| `Run` | `Func<Task>?` | What pressing it does, for a control that is one action: the view switch and Reset zoom |
| `Default` | `RenderFragment` | The control as the chart draws it |

Return `context.Default` to keep a control, draw it inside your own markup to wrap it, return something else to replace it, or return nothing to leave it out. Keep the chart in a field to call `ExportAsync` from your own button:

```razor
<MokaLineChart @ref="_chart" Series="_series" Title="Requests"
               Controls="MokaChartControls.View | MokaChartControls.Export" ToolbarControl="Own" />

@code {
    private MokaLineChart? _chart;

    private RenderFragment Own(MokaChartControlContext context) => context.Which == MokaChartControls.View
        ? context.Default
        : @<MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm" StartIcon="MokaIcons.Action.Download"
                       OnClick="() => _chart!.ExportAsync(MokaChartControls.ExportCsv)">Save CSV</MokaButton>;
}
```

## States

States draw inside the frame at the chart's own size, so the page never jumps:

| State | Drawn |
|-------|-------|
| `Loading` | The last drawing stays, dimmed, with an Updating chip. Never blank, never a skeleton |
| `Empty` | `EmptyContent`, else a compact empty state saying `EmptyText` |
| `Error` | `ErrorContent`, else an alert saying `ErrorText`, with Try again when `OnRetry` has a listener |

`Auto` is `Empty` while there is no data. A polite status region says "Updating" and the empty text when they appear; the error alert speaks for itself.

```razor
<MokaLineChart Series="_series" Title="Traffic" State="_state" OnRetry="LoadAsync"
               ErrorText="The router didn't answer within 10 seconds." EmptyText="No traffic in this range" />
```

## Table view

`MokaChartControls.View` adds a Chart and Table switch. The table is a `MokaTable` of the same values, one row per point, a page of 50 at a time, so a long series costs one page of DOM, and every column sorts. It sits over the chart's own place, which keeps its size. Escape goes back to the chart.

## Export

The Export menu offers the formats in `Controls`. A PNG or SVG is the chart as drawn, with its title and legend, rebuilt as one standalone SVG with the page's fonts embedded; a PNG is that drawing at twice the size. CSV is the table's values, written in .NET a few rows at a time and streamed to the browser, with cells that a spreadsheet would run as a formula made text. The export script loads the first time something is exported.

## Range presets and the range strip

On a time axis, `MokaChartControls.Ranges` draws the presets as a segmented control. Picking one zooms to the last span of the data, or of now when the data runs past now, and raises `RangePresetChanged` and `RangeChanged`, so the app can load other data and show `Loading` meanwhile.

`Zoom="MokaChartZoom.Strip"` draws a `MokaRangeBar` under the plot with the first shown series behind its window. Drag the window or its handles; the arrows move an hour and Shift a day on a week of data (the step follows the span), Home and End go to the ends, and the chart follows when you let go. `ResetZoom` shows a Reset zoom button while zoomed by hand.

The strip's track lines up with the plot box, not the whole frame: it starts where the plot starts and ends where it ends, whichever side the value axis sits on, with ticks inside or names at the line ends beside the plot, and under categories that scroll it spans the part of the plot in view. A fixed `Height` stays the plot's own, with the strip under it. On a time axis the overview places each value at its time, so an hour with no samples is a straight stretch and the overview lines up with the window over it, out to the ends `XAxis.MinTime` and `MaxTime` pin. Small multiples keep one strip under all of them, the frame's width.

With `SyncZoom` the strip, the presets and Reset zoom move the other charts of the sync group too (see [Chart Interaction](chart-interaction.md)).

```razor
<MokaLineChart Series="_series" Times="_times" Title="Traffic this week"
               Controls="MokaChartControls.Ranges | MokaChartControls.ResetZoom" Zoom="MokaChartZoom.Strip"
               RangePresets="_presets" @bind-RangePreset="_preset" @bind-Range="_range" />

@code {
    private string? _preset;
    private MokaChartRange? _range;

    private readonly MokaChartRangePreset[] _presets =
    [
        new("1 h", TimeSpan.FromHours(1)), new("6 h", TimeSpan.FromHours(6)),
        new("24 h", TimeSpan.FromHours(24)), new("7 d", TimeSpan.FromDays(7))
    ];
}
```

## Legend

The legend's placement, keys, toggles and folding are on [Chart Legend](chart-legend). A chart of one series has no
legend, since its title names it, unless you set `Legend`, the entry shows a value, or the series is hidden and the
legend is the way back. A donut ring or waffle lists its shares itself, so its frame draws no legend either.

## Height

A column chart keeps its proportion from its width, no taller than its `Size` allows: 6rem at `Xs`, 9rem at `Sm`, 14rem
at `Md` and 19rem at `Lg`, so a wide card gets a wider plot rather than a taller one. Set `--moka-chart-plot-max` on the
chart or any element around it to change the cap, or `Height` to take the plot out of proportion altogether.

## Accessibility

- A chart whose frame holds a control (a toolbar, legend toggles, Try again, the table, the strip) is a `figure`, not an `img`, since an image's contents can't be reached.
- The toolbar is a `toolbar` named by the chart's label. The view switch and the presets are radio groups named "Show as" and "Time range"; the Export button is named "Export" and sits in a tooltip.
- The table view is a region named "{label}, Table" over a table named by the chart's label.
- The range strip's handles are sliders named "Time range start" and "Time range end", reading the time they're on.
- Every word comes from `MokaChartText`.
