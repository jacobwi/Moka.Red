---
title: Box Plot
description: One row per group of values, with the middle half as a box, the median, whiskers and the values as faint dots.
order: 63
---

# Box Plot

`MokaBoxPlot` draws one row per group of values: the box covers the middle half, the bar inside it is the median, and
the whiskers reach the 5th and 95th percentiles. With `ShowPoints` the values themselves are drawn as faint dots behind
the box. A key under the rows says what each part means, since people who don't know the form misread it.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Groups` | `IReadOnlyList<MokaBoxPlotGroup>?` | -- | The groups, one row each, in order |
| `Whiskers` | `MokaWhiskers` | `Percentile5To95` | Where the whiskers end: the 5th and 95th percentile, the lowest and highest value, or Tukey's 1.5 times the middle half |
| `ShowPoints` | `bool` | `false` | Draws the values as faint dots behind each box |
| `MaxPoints` | `int` | `200` | The most points a row draws. More draw an even stride of the sorted values, the lowest and highest among them |
| `ShowKey` | `bool` | `true` | Whether the key under the rows is drawn |
| `MedianText`, `MiddleText` | `string` | `"Median"`, `"Middle half"` | The words for the median and the box, in the tooltip, the key and the table |
| `WhiskerText` | `string?` | -- | The whiskers' name. Null names them by `Whiskers`: "5th to 95th", "Lowest to highest", "Whiskers" |
| `PointText` | `string` | `"One value"` | What a dot is called in the key |
| `CountText` | `Func<int, string>?` | -- | Writes a group's count for the tooltip. Defaults to "1,204 values" |
| `LowText`, `HighText`, `CountLabel` | `string` | `"Low"`, `"High"`, `"Count"` | The table's headings |

It also takes the parameters every chart shares, and `Unit`, `Formatter` and `XAxis` for the value axis, `Annotations`
and `TargetLow` and `TargetHigh`, which run down the rows.

### MokaBoxPlotGroup

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | What the group is |
| `Values` | `IReadOnlyList<double>` | The values, in any order. A value that isn't a finite number doesn't count |
| `Summary` | `MokaBoxSummary?` | The box worked out already, such as on the server for a million values. Wins over `Values`, which then only feed the points |
| `Color` | `MokaColor?` | The color of its box |
| `Key` | `string?` | Its identity for highlighting. Null uses `Name` |

## Response times

```blazor-preview
@code {
    MokaBoxPlotGroup[] _endpoints =
    [
        new("/api/login", [92, 110, 104, 131, 88, 140, 99, 121, 160, 85]),
        new("/api/search", [180, 210, 260, 195, 320, 240, 205, 410, 230, 199]),
        new("/api/thumbs", [52, 70, 61, 90, 48, 77, 65, 130, 58, 71])
    ];
}

<MokaBoxPlot Groups="_endpoints" ShowPoints Unit="MokaChartUnit.Milliseconds" Title="Response time by endpoint" />
```

The tooltip of a row names its median, the middle half, the whiskers and its count, such as "api: Median 120 ms, Middle
half 90 ms to 160 ms, 5th to 95th 60 ms to 300 ms, 1,204 values".

## Outliers

With `Whiskers="MokaWhiskers.Tukey"` the whiskers stop at the most extreme values within one and a half times the middle
half's spread of the box, and the values past them are drawn as dots even without `ShowPoints`.

## Large groups

The box, the median and the whiskers are HTML. The points are one small svg per row holding one path, so a row costs the
same whatever its count. Working a box out sorts its group's values, which for a million values takes about a tenth of a
second each time the data changes: pass a precomputed `Summary`, or set `DataVersion` so the values are compared by
version rather than copied.
