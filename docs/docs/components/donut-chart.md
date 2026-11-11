---
title: Donut Chart
description: Shares of a whole as arcs of one ring, with a legend and a centered slot.
order: 58
---

# Donut Chart

`MokaDonutChart` draws shares of a whole as arcs of one ring, with a legend beside it and a slot
in the middle for the total or a label. Pure SVG, so it themes and scales with the page.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Segments` | `IReadOnlyList<MokaDonutSegment>?` | -- | The shares to draw. Each has `Name`, `Value` and an optional `Color`, `ColorValue`, `Key` and `Slot` |
| `Shape` | `MokaDonutShape` | `Ring` | `Ring`, `Bar` (one tall bar) or `Waffle` (a hundred squares) |
| `ShowShares` | `bool?` | -- | Shares printed inside the segments where they fit. Null: on for the bar only |
| `Thickness` | `double` | `20` | Ring thickness, in viewBox units out of 200. A look other than Standard sets its own unless this is changed |
| `ShowLegend` | `bool` | `true` | Whether the shares are listed beside the chart with their percentages |
| `ChildContent` | `RenderFragment?` | -- | Content centered in the ring, such as the total |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Formatter` | `Func<double, string>?` | -- | Writes a segment's value, such as "3 GiB", for the tooltips and the legend. Null leaves the legend to percentages |
| `Total` | `double?` | -- | The whole the segments are shares of, such as a quota. The rest of the ring shows as the track. Null uses the segments' sum |
| `Size` | `MokaSize` | `Md` | The box the ring scales into |

It also takes the parameters every chart shares (`Title`, `Subtitle`, `Look`, `Texture`, `Colors`, `Highlight`,
`HiddenSeries`, `Tooltip`, `Height` and the rest; see the chart frame and chart looks pages). `Height` sets the ring's
or the waffle's size, or the bar's thickness.

With the ring, the Standard look and none of those set, the donut draws exactly what it always has. Any of them moves
it onto the shared chart frame.

## Basic

```blazor-preview
@code {
    MokaDonutSegment[] _segments =
    [
        new() { Name = "Direct", Value = 52 },
        new() { Name = "Referral", Value = 24 },
        new() { Name = "Social", Value = 15 },
        new() { Name = "Other", Value = 9 }
    ];
}

<MokaDonutChart Segments="_segments" />
```

## With a centered total

```blazor-preview
@code {
    MokaDonutSegment[] _segments =
    [
        new() { Name = "Shipped", Value = 312, Color = MokaColor.Success },
        new() { Name = "Pending", Value = 88, Color = MokaColor.Warning }
    ];
}

<MokaDonutChart Segments="_segments">
    <div style="text-align:center">
        <div style="font-size:var(--moka-font-size-xl);font-weight:var(--moka-font-weight-bold)">400</div>
        <div style="font-size:var(--moka-font-size-xs);opacity:0.7">orders</div>
    </div>
</MokaDonutChart>
```

Segments start at twelve o'clock and run clockwise, in the order given. Segments without an
explicit color take the palette in rotation, every arc carries a native tooltip with its value and
percentage, and the legend lists each share with its rounded percentage.

## Against a total

By default the segments fill the ring, each a share of their sum. `Total` makes them shares of a whole you name
instead, such as used space against a quota, and what's left of the ring shows as the track. `Formatter` writes the
values, so the tooltips say "Photos: 3 GiB (30%)" rather than "Photos: 3221225472 (30%)", and the legend lists the
value beside the percentage.

```razor
@code {
    const double GiB = 1024d * 1024 * 1024;

    MokaDonutSegment[] _used =
    [
        new() { Name = "Photos", Value = 3 * GiB },
        new() { Name = "Music", Value = 1.5 * GiB, Color = MokaColor.Info }
    ];

    static string Gib(double bytes) => $"{bytes / GiB:0.#} GiB";
}

<MokaDonutChart Segments="_used" Total="10 * GiB" Formatter="Gib">
    <div style="font-size:var(--moka-font-size-sm)">4.5 of 10 GiB</div>
</MokaDonutChart>
```

When the segments add up to more than `Total`, the ring is full and the arcs that run past its end are cut off. The
legend and tooltips still give each segment's share of `Total`, so an account over its quota reads 80% and 40%.
A `Total` of zero or less is ignored.

## Ring looks

The chart look (`Look`, or `MokaTheme.WithCharts`) shapes the ring: Quiet is thin with small gaps between segments,
Dense thinner still, Soft has round ends and wider gaps, Bold is thick. The shares stay listed beside the ring in every
look, with their values when a `Formatter` is set. On `MokaTheme.Dark` and `Light` the look is always Standard.

```blazor-preview
@code {
    MokaDonutSegment[] _traffic =
    [
        new() { Name = "Web", Value = 41200 },
        new() { Name = "API", Value = 23800 },
        new() { Name = "Jobs", Value = 9100 }
    ];
}

<MokaDonutChart Segments="_traffic" Look="MokaChartLook.Soft" Title="Traffic share" />
```

`Texture="true"` (or `MokaTheme.WithChartTexture`) hatches every arc, 45 degrees for odd slots and 135 for even ones,
so neighbours differ without color, for print and color-blind readers.

## Tall segmented bar

`Shape="MokaDonutShape.Bar"` draws the same segments as one tall bar, each share printed inside where it fits and a
legend of names. Parts on one line compare more exactly than angles, so for six parts or fewer it reads better than a
ring. `ChildContent` sits above it as a summary line, and with a `Total` the rest of the bar is the track.

```blazor-preview
@code {
    MokaDonutSegment[] _storage =
    [
        new() { Name = "Media", Value = 864 },
        new() { Name = "Backups", Value = 476 },
        new() { Name = "Work", Value = 185 },
        new() { Name = "System", Value = 95 }
    ];
}

<MokaDonutChart Shape="MokaDonutShape.Bar" Segments="_storage" Total="2000" Title="Storage on nas-01">
    <b>1.62 TB</b> used of 2 TB
</MokaDonutChart>
```

Each segment is a size container, so its share disappears when the segment is too narrow to hold it; the tooltip and
the table still give it.

## Waffle

`Shape="MokaDonutShape.Waffle"` draws a hundred squares, each one percent, with the shares listed beside them. Easier
to count than a slice, and good when one share is the story. Squares go to the segments by the largest remainder, so
they always add up to a hundred. Pointing at a square lights its whole share.

```blazor-preview
@code {
    MokaDonutSegment[] _protocols =
    [
        new() { Name = "HTTPS", Value = 62 },
        new() { Name = "QUIC", Value = 24 },
        new() { Name = "DNS", Value = 4 },
        new() { Name = "Other", Value = 10 }
    ];
}

<MokaDonutChart Shape="MokaDonutShape.Waffle" Segments="_protocols" Title="Traffic by protocol" />
```

## Hiding and highlighting

`HiddenSeries` takes segment keys (`Key`, else `Name`): a hidden segment leaves and the rest share the whole, without
changing color, since a segment's color follows its place in the full list. `Highlight` draws one segment in its color
and mutes the rest.
