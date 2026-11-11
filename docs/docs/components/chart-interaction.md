---
title: Chart Interaction
description: Tooltips, the keyboard grid, pins, sync groups, zoom gestures and the live hold every Moka.Red chart shares.
order: 60
---

# Chart Interaction

Every modern Moka.Red chart answers the pointer and the keyboard the same way. Pointing at the plot shows a tooltip,
Tab reaches the chart as one stop and the arrow keys walk its values, a click or Enter can pin a point to compare
against, charts in a sync group share one crosshair, and a live chart can hold still while someone reads it.

All of it is opt in. A chart on the original design with no interaction parameter renders exactly as before: an image
with the browser's own tooltips. A chart look (`Look="MokaChartLook.Quiet"`, or `MokaTheme.WithCharts`) turns on the
look's defaults, and each parameter below wins over the look.

## Parameters

Shared by every chart (they live on the chart base, so every chart takes them):

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Tooltip` | `MokaChartTooltip?` | the look's | `Crosshair`, `Nearest`, `Readout`, `Column`, `Native`, `None`, or `Auto` (crosshair on lines, column on bars and rows, nearest on scatter) |
| `TooltipTemplate` | `RenderFragment<MokaChartTooltipContext>?` | -- | Your own tooltip. Rendered only when the point under the pointer changes |
| `Formatter` | `Func<double, string>?` | -- | Writes the values in the tooltip, the readout and the keyboard cell |
| `Pinnable` | `bool?` | the look's | Whether a click or Enter pins the point, showing the change since it |
| `Keyboard` | `bool?` | the look's | One tab stop, and the arrow keys read the values |
| `SyncGroup` | `string?` | -- | Charts with the same group share their crosshair |
| `ActiveChanged` | `EventCallback<MokaChartPoint?>` | -- | The point under the pointer or the focus, and null when there is none |
| `OnPointClick` | `EventCallback<MokaChartPoint>` | -- | A click or Enter on a point |
| `Text` | `MokaChartText?` | English | Every word, including what a screen reader hears |

On charts with an x axis (`MokaLineChart`, `MokaBarChart` and the rest of the cartesian charts):

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Zoom` | `MokaChartZoom?` | -- | `Brush`, `Wheel`, `Strip`, or `All` |
| `Range` / `RangeChanged` | `MokaChartRange?` | -- | The zoomed span; bind it to keep or share it |

## Tooltip modes

| Mode | What it shows | Target |
|------|---------------|--------|
| `Crosshair` | A hairline at the nearest time, a dot on each line, and one tooltip listing every series there, value first | The whole plot, so you aim at a time, not at a line |
| `Nearest` | Only the point closest to the pointer, within 32 pixels, with a larger dot | Each point, for scatter-like data or many lines |
| `Readout` | The same values in a strip above the plot, the newest until you point | The whole plot; nothing covers the data, and a finger never hides the numbers |
| `Column` | The bar's whole column lifts and its tooltip shows the column's values | Each column, not just the bar |
| `Native` | The browser's own `title` tooltip on each mark | The mark |
| `None` | Nothing (the keyboard still works when `Keyboard` is on) | |

```blazor-preview
<MokaLineChart Label="Traffic today" Look="MokaChartLook.Quiet" Tooltip="MokaChartTooltip.Crosshair"
               Labels="@(new[] { "00:00", "06:00", "12:00", "18:00" })"
               Series="@(new[] { new MokaLineSeries { Name = "Download", Values = new[] { 40d, 90, 210, 320 } },
                                 new MokaLineSeries { Name = "Upload", Values = new[] { 12d, 30, 44, 70 } } })" />
```

The tooltip sits beside the crosshair and flips to the other side before it would leave the plot, above a bar, and
always inside the window: it never makes the page scroll sideways, at any width. It is drawn in the browser's top
layer, so a card or a table that clips its overflow can't cut it off, and it follows the plot when the page scrolls.

On a touch screen a tap shows the tooltip, dragging a finger across the plot moves it, and a tap anywhere else puts it
away. A second tap on the same point pins it.

### Your own tooltip

`TooltipTemplate` gets a `MokaChartTooltipContext`: the position, the heading the chart would write, one
`MokaChartPoint` per visible series, the point the keyboard is on, the pinned position and any notes (thresholds,
target bands). It is rendered by .NET only when the point changes, never on a pointer move.

```razor
<MokaLineChart Series="_series" Labels="_times" Tooltip="MokaChartTooltip.Crosshair" TooltipTemplate="Together"/>

@code {
    private static readonly RenderFragment<MokaChartTooltipContext> Together = context =>
        @<div>
            <strong>@context.Heading</strong>
            <span>@context.Points.Sum(point => point.Value).ToString("0") Mbps together</span>
        </div>;
}
```

Keep the template a field or a method. A lambda written inline is a new delegate on every parent render, which makes
the chart render again each time (the tooltip itself still waits for the point to change).

## Pin

With `Pinnable`, a click or Enter pins the point under the pointer. A card at the top of the plot names it with an
Unpin button, a line marks it, and while you point elsewhere the tooltip adds how far each series moved since the pin,
under a note that reads "Change since 14:30" (`MokaChartText.ChangeSince`). On a chart of rows the card sits at the
pinned row's end and a band marks the row; on a scatter chart the pinned dot is marked, with no line.
Escape, the Unpin button or a click on the same point lets go. A screen reader hears "Pinned at 14:30." and
"Unpinned." (`MokaChartText.PinnedAt` and `Unpinned`).

## Keyboard

With `Keyboard`, the chart is one tab stop: a grid over the plot whose active cell is the focused value
(`aria-activedescendant`). Only that one cell exists, so a series of a million points costs one element. The grid is
named by the chart's `Label` and how its keys work.

| Key | Lines | Bars | Rows |
|-----|-------|------|------|
| Left, Right | the previous or next point | the previous or next bar | |
| Up, Down | the line above or below, in legend order, skipping hidden ones; with legend highlighting, it dims the rest | | the row above or below |
| Home, End | the first or last point | the first or last bar | the first or last row |
| Page Up, Page Down | a tenth of the points back or on | the same | the same |
| Enter | pins (with `Pinnable`) and raises `OnPointClick` | the same | the same |
| Space | pins (with `Pinnable`) | the same | the same |
| Escape | lets the pin go, then hides the tooltip; with neither, the key goes on (a dialog around the chart closes) | the same | the same |
| `+`, `-`, `0` | zoom in, out, and back (with `Zoom` holding `Wheel`) | | |

What a screen reader hears: Tab in, and it reads the grid, "Traffic today. Left and Right move along a line, Up and
Down change line.", then the cell for the newest point of the first line, such as "Download, 23:30, 41 Mbps". Each
arrow reads the new cell. Bars read the whole column, such as "Wi-Fi, 31 devices, 38% of the 82 online".

## Legend highlight

With `LegendActions` holding `Highlight`, pointing at a legend entry, coming close to a line, or moving between lines
with Up and Down dims every other series. It is only visual and nothing reaches .NET. In a sync group the highlight
reaches every chart in it.

## Sync groups

Charts with the same `SyncGroup` share one crosshair: pointing at one sets every other at the same x value (a time or
a category), and each reports its own `ActiveChanged`. Only the chart you point at shows a floating tooltip; the
others show their crosshair and update a readout. This is the honest way to show two measures with different units on
one time axis, instead of a second y axis.

```razor
<MokaLineChart Label="Requests per second" Tooltip="MokaChartTooltip.Readout" SyncGroup="today" Series="_requests" Labels="_times"/>
<MokaLineChart Label="p95 response time" Tooltip="MokaChartTooltip.Readout" SyncGroup="today" Series="_latency" Labels="_times"/>
```

Small multiples inside one chart (`MokaLineChart.Split`) share the crosshair the same way. Log x axes don't sync.

## Zoom gestures

`Zoom` picks the gestures:

| Flag | Gesture |
|------|---------|
| `Brush` | Drag across the plot to zoom into that span. Escape cancels the drag, a double click goes back |
| `Wheel` | Ctrl (or Cmd) and the wheel, or a trackpad pinch, zoom around the pointer; a two-finger pinch on a touch screen; `+`, `-` and `0` on the focused chart. Once zoomed, dragging pans (with `Brush` too, Shift and drag pans). A plain wheel still scrolls the page and shows "Hold Ctrl and scroll to zoom" for a moment |
| `Strip` | A range strip under the chart (see the chart frame) |

While a gesture is under way the chart stretches what it already drew as a preview, and 150 ms after the gesture stops
it sets `Range` and raises `RangeChanged` once. The new drawing replaces the preview.

## Live, paused while you point

A live chart that holds still while pointed at or focused shows a chip in its header, beside the title, so it never
covers the newest values: "Live", "Paused", or "Paused · 3 new" while samples wait. It catches up when the pointer and
the focus leave. The words are `MokaChartText.Live`, `Paused` and `PausedWaiting`. While it holds, new samples don't
move the geometry the script holds, so pointing at a held chart sends nothing more to .NET.

## Charts of parts

A donut, a treemap and a sankey find the point under the pointer by the mark under it: the surface lets the pointer
through, so each part keeps its own hover, and a treemap tile that drills in still takes its click. The keyboard steps
through the parts with Left and Right (`MokaChartText.PartKeys`).

## Performance

Pointer movement never reaches .NET. The script works out the point under the pointer itself and calls .NET only when
that point changes, with one call in flight per chart: a newer point replaces the one waiting. Measured on Blazor
Server against a chart of three series:

| What the pointer does | WebSocket frames |
|-----------------------|------------------|
| moves within one point, 20 times | none |
| moves over the inside of one bar's column | none |
| crosses to the next point | 2 sent, 2 received, about 1.3 KB (the call, the tooltip's render, and their acknowledgements) |
| crosses 30 points in a hurry with 200 ms of latency | 2 calls in all, the last one for the point it stopped on |
| arrives for the first time after the data changed | one geometry pull, 2 bytes a point a series |

Data changing sends nothing to the browser's script until someone points at the chart again, so a dashboard of live
charts nobody points at costs nothing. Only the tooltip host renders when the point changes; the chart and its plot
don't.

Static rendering and the docs previews draw the whole chart and bind nothing: no tab stop, no tooltip, the chart is an
image named by its label.
