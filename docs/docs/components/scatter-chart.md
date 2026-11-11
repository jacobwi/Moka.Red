---
title: Scatter Chart
description: Dots placed by two measures, or bubbles whose area is a third, with the tooltip on the nearest dot.
order: 64
---

# Scatter Chart

`MokaScatterChart` places each thing by two measures, such as devices by connections and data used. A `Size` on the
points makes it a bubble chart, whose areas follow a third measure. The tooltip finds the dot nearest the pointer, within
32 pixels, so small dots need no aiming. Three series at most read well, since any two dots can touch and more colors
can't all be told apart.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Series` | `IReadOnlyList<MokaScatterSeries>?` | -- | The groups of points, each in one color |
| `XName`, `YName` | `string?` | -- | What the two measures are called, in the tooltip and as the axes' titles |
| `SizeName` | `string?` | -- | What the bubbles' size measures, in the tooltip |
| `SizeFormatter` | `Func<double, string>?` | -- | Writes a bubble's size |
| `MinSize`, `MaxSize` | `double` | `4`, `28` | The smallest and largest bubble's diameter in pixels; areas follow the sizes between |
| `MaxBubbles` | `int` | `2000` | The most bubbles drawn. Past it the largest are kept, and a line under the chart and its summary say so |
| `SeriesLabel` | `string` | `"Series"` | The table's heading for a point's series |
| `LargestText` | `Func<int, int, string>?` | -- | Says how many bubbles are drawn of how many. Defaults to "The 2,000 largest of 5,400 are drawn" |
| `Zoom` | `MokaChartZoom?` | -- | Brush and wheel zoom along the measure across |

It also takes the parameters every chart shares, and `XAxis` and `YAxis` (`Scale = MokaAxisScale.Log` for a log axis,
`Unit`, `Formatter`, `Min`, `Max`), `Unit` and `Formatter` for the measure up, `Annotations`, `Height` and `Range`.

### MokaScatterSeries and MokaScatterPoint

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | The series' name, in the legend and the tooltip |
| `Points` | `IReadOnlyList<MokaScatterPoint>` | Its points |
| `Color`, `ColorValue`, `Slot` | | Its color: a tone, any CSS color or token, or a palette slot kept across charts |
| `Key` | `string?` | Its identity for hiding and highlighting. Null uses `Name` |

`MokaScatterPoint(double X, double Y, double? Size = null, string? Label = null)` is a struct, so a hundred thousand
points cost no allocation each. `Label` names the point in the tooltip and the table, such as "router-2".

## Devices

```blazor-preview
@code {
    MokaScatterSeries[] _devices =
    [
        new("Laptops", [new(190, 11.2, null, "laptop-01"), new(220, 14.5, null, "laptop-02"), new(150, 6.4, null, "laptop-03")]),
        new("Phones", [new(80, 3.2, null, "pixel-8"), new(60, 2.1, null, "iphone-anna")]),
        new("IoT", [new(290, 0.9, null, "cam-door"), new(214, 26.4, null, "tv-living-room")])
    ];
}

<MokaScatterChart Series="_devices" XName="connections" YName="used" Unit="@(MokaChartUnit.Of("GB"))"
                  Title="Devices by connections and data used" />
```

The tooltip of the outlier reads "tv-living-room, IoT: 26.4 GB used, 214 connections".

## Bubbles

```blazor-preview
@code {
    MokaScatterSeries[] _services =
    [
        new("Services", [new(4200, 0.4, 6, "api"), new(2600, 1.1, 8, "search"), new(2100, 1.8, 3, "sync"),
            new(1200, 0.6, 3, "files"), new(900, 2.4, 4, "thumbs"), new(300, 0.9, 1, "mail")])
    ];
}

<MokaScatterChart Series="_services" XName="requests/min" YName="errors" SizeName="CPU cores"
                  Unit="MokaChartUnit.Percent" Title="Services by load and errors" />
```

A bubble's area follows its size: the largest is `MaxSize` across, and one a quarter of its size is half as wide.
Area is hard to judge exactly, so sizes give a rough read and the tooltip the numbers. Bubbles are drawn largest first, so
the small ones stay on top.

## Many points

Dots are one svg path per series of zero-length strokes with round caps, so a series of a hundred thousand points is one
element. Past 2,000 points a series keeps the first point in each 2 pixel cell of the plot (`MokaDownsampler.GridDedupe`),
which keeps the shape of the cloud; such a series draws smaller, fainter dots, so where it is dense still reads as dense.
The cells follow the plot's width as the browser reports it, in 64 pixel steps. The dots the pointer finds are exactly the
ones drawn. Pass `DataVersion` with large series, so they are compared by version rather than copied.

On the Standard look, which has no tooltip of its own, a chart of up to 1,000 dots draws each with a native tooltip.

## Keyboard

With `Keyboard` on (every look but Standard), the chart takes one tab stop. Left and Right step through a series' dots
from left to right, Up and Down move to the next series' dot nearest across, Home and End go to the series' ends, and
Enter pins the dot.
