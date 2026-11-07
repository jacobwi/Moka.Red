---
title: Stat
description: A big number with a label, a unit and a trend, or a small fact tile.
order: 124
---

# Stat

`MokaStat` shows a value with an optional label, unit, trend and icon. `Tile` draws it as a small panel, for a row of facts.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | -- | The number or value to show |
| `Label` | `string?` | -- | Description under the value |
| `HelpText` | `string?` | -- | More context, under the label |
| `Prefix` | `string?` | -- | Text before the value, such as `$` |
| `Suffix` | `string?` | -- | Unit after the value, such as `GB today` |
| `Trend` | `double?` | -- | Percentage change. Positive is up, negative is down |
| `TrendLabel` | `string?` | -- | What the trend compares with, such as `vs last month` |
| `Icon` | `MokaIconDefinition?` | -- | Icon above the value |
| `Align` | `MokaTextAlign?` | -- | Text alignment |
| `Tile` | `bool` | `false` | Draws a fact tile. `Size` is ignored |
| `Color` | `MokaColor?` | -- | The icon's color. On a tile it also tints the value |
| `Size` | `MokaSize` | `Md` | The size of the value |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Big number

```razor
<MokaStat Value="48.2" Suffix="GB today" />
```

The unit sits on the number's baseline, smaller and muted.

## Fact tiles

A row of tiles is a `MokaGrid`.

```razor
<MokaGrid Columns="3" Gap="MokaSpacingScale.Md">
    <MokaStat Tile Value="99.9%" Label="Up" Color="MokaColor.Success" />
    <MokaStat Tile Value="14 ms" Label="Delay" />
    <MokaStat Tile Value="2" Label="Dropped" Color="MokaColor.Warning" />
</MokaGrid>
```
