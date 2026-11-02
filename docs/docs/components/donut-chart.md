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
| `Segments` | `IReadOnlyList<MokaDonutSegment>?` | -- | The shares to draw. Each has `Name`, `Value` and an optional `Color` |
| `Thickness` | `double` | `20` | Ring thickness, in viewBox units out of 200 |
| `ShowLegend` | `bool` | `true` | Whether the shares are listed beside the chart with their percentages |
| `ChildContent` | `RenderFragment?` | -- | Content centered in the ring, such as the total |
| `Label` | `string?` | -- | What the chart shows, for assistive technology |
| `Size` | `MokaSize` | `Md` | The box the ring scales into |

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
