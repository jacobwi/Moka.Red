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
| `IconContent` | `RenderFragment?` | -- | Anything in the icon's place, such as an emoji or a small picture, in the icon's 24 px box. Wins over `Icon` |
| `Align` | `MokaTextAlign?` | -- | Text alignment |
| `Tile` | `bool` | `false` | Draws a fact tile. `Size` is ignored |
| `Visual` | `RenderFragment?` | -- | A small picture on the value's row, such as a `MokaSparkline` or a `MokaMeter` |
| `Mono` | `bool` | `false` | Draws the value in the mono font with tabular figures |
| `Spark` | `IReadOnlyList<double>?` | -- | Recent values, drawn as a muted sparkline in the `Visual` place with the latest point in the color. `Visual` wins |
| `SparkLabel` | `string?` | -- | The sparkline's accessible name. Null names it after `Label` |
| `UpIsGood` | `bool` | `true` | False colors a rise as bad and a fall as good, for latency or errors, and says "better" or "worse" to screen readers |
| `BetterText`, `WorseText` | `string` | `"better"`, `"worse"` | What a screen reader hears after the trend when `UpIsGood` is off |
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

## With a visual

`Visual` puts a sparkline or a meter beside the value, on the same row. It takes the width the value leaves, and moves under the value when too little is left. A meter fills that width; a sparkline keeps its own.

```razor
<MokaStat Value="48.2" Suffix="GB today" Label="Traffic">
    <Visual>
        <MokaSparkline Values="_traffic" Size="MokaSize.Sm" Label="Traffic this week" />
    </Visual>
</MokaStat>

<MokaStat Tile Value="62%" Label="Disk">
    <Visual>
        <MokaMeter Value="62" ShowValue="false" Size="MokaSize.Sm" aria-label="Disk used" />
    </Visual>
</MokaStat>
```

On a tile the meter's track uses the color of things that are off, so it stays visible on the tile's panel color. That holds on the new design; on `MokaTheme.Dark` and `Light` the track is still the panel color.

## Mono

`Mono` draws the value in the mono font with tabular figures, so a number that updates in place keeps its width.

```razor
<MokaStat Mono Value="1 284" Suffix="req/s" Label="Rate" />
```

## KPI tiles with a sparkline

`Spark` draws the last values as a gray sparkline beside the number, with the latest point in the color: the day's
shape at a glance without a second color competing with the value. `UpIsGood="false"` is for figures where less is
better, such as latency or errors: a fall is green and a rise red, while the arrow still points the way the number
moved, and a screen reader hears "better" or "worse" after the change. With `UpIsGood` left on, the markup is what it
always was.

```blazor-preview
@code {
    double[] _requests = [18, 22, 31, 52, 70, 64, 48, 39];
    double[] _latency = [180, 186, 240, 228, 205, 214, 212];
}

<MokaGrid MinColumn="11rem" Gap="MokaSpacingScale.Sm">
    <MokaStat Tile Value="1.28M" Label="Requests today" Trend="4.2" TrendLabel="vs yesterday" Spark="_requests" />
    <MokaStat Tile Value="212 ms" Label="p95 latency" Trend="-8" TrendLabel="vs yesterday" UpIsGood="false" Spark="_latency" />
</MokaGrid>
```
