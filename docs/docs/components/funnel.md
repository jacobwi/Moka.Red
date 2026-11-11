---
title: Funnel
description: Steps in order with how many reached each and the share that went on between them.
order: 66
---

# Funnel

`MokaFunnel` draws steps in order with how many reached each, and under each bar the share that went on to the next. The
bars start from one line, which compares more exactly than a trapezoid, and step from dark to light in one hue because
the order means something (`--moka-chart-ord-1` to `-5`; a sixth step and later keep the fifth).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Steps` | `IReadOnlyList<MokaFunnelStep>?` | -- | The steps, first to last |
| `ShowConversion` | `bool` | `true` | Whether the share that went on is written under each bar. On, the rows are 40% taller |
| `ShareText` | `Func<string, string, string>?` | -- | Writes a share for the tooltip from the percent and what it is a share of. Defaults to "62% of Visited" |
| `AllText` | `string` | `"all"` | What the first step is called in the tooltip's share of everything |
| `ConversionText` | `Func<string, string>?` | -- | Writes the share that went on. Defaults to "62% went on" |
| `ConversionLabel` | `string` | `"Went on"` | The table's heading for the share of the step before |

It also takes the parameters every chart shares, and `Unit`, `Formatter` and `XAxis` for the value axis, `Annotations`,
`TargetLow` and `TargetHigh` and `RowHeight`. `Highlight` names a step: it keeps its color and the rest are muted.

`MokaFunnelStep(string Name, double Value)` has a `Detail` for the tooltip.

## Device setup

```blazor-preview
@code {
    MokaFunnelStep[] _setup =
    [
        new("Discovered", 1240), new("Named", 980), new("Zone assigned", 712),
        new("Rules reviewed", 431), new("Trusted", 288)
    ];
}

<MokaFunnel Steps="_setup" Title="New device setup" Subtitle="Devices that reached each step, this month" />
```

The tooltip of a step says its share of the step before and of the first, such as "Zone assigned: 712, 73% of Named, 57% of
all". The table lists each step's value, its share of all and its share of the step before.
