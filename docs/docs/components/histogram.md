---
title: Histogram
description: How many raw values fell in each bucket, with lines at chosen percentiles.
order: 58
---

# Histogram

`MokaHistogram` counts raw values into buckets of one size and draws a bar per bucket, such as requests per 25 ms of
response time. It answers whether something is usually fast with a long tail, or slow for everyone. Lines at chosen
percentiles say where the 95th percentile sits.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Values` | `IReadOnlyList<double>?` | -- | The raw values. A value that isn't a finite number isn't counted |
| `BucketSize` | `double?` | -- | How wide each bucket is, in the values' units. Null works one out from the spread and rounds it to a step that reads well in `Unit` |
| `Origin` | `double?` | -- | A value buckets start at. Null starts them at multiples of the size |
| `Percentiles` | `IReadOnlyList<double>?` | -- | Percentiles from 0 to 100 to draw a line at, such as `[50, 95]` |
| `CountLabel` | `string?` | "values" | What the values are, in the tooltip: "1,204 requests" |
| `PercentileText` | `Func<double, string, string>?` | -- | Writes a line's label from the percentile and its value. Defaults to "p95 240 ms" |
| `MaxBins` | `int` | `200` | The most buckets. More, or more than the plot has three pixels each for, widen each bucket by a whole factor |
| `Unit` | `MokaChartUnit?` | -- | The values' unit, along the x axis, in the tooltip and on the percentile lines |
| `Formatter` | `Func<double, string>?` | -- | Writes a value; wins over `Unit` |
| `Color` | `MokaColor?` | -- | The bars' color. Null is the palette's first |

It also takes what every chart shares: `Title`, `Subtitle`, `Look`, `Colors`, `Texture`, `Tooltip`, `Keyboard`,
`Controls` (table view and export), `State`, `TargetLow`, `TargetHigh`, `YAxis` for the counts, `XAxis` for the values,
and the rest.

## Basic

```blazor-preview
@code {
    double[] _times = [.. Enumerable.Range(0, 2000).Select(i => 60 + 140 * Math.Pow(Math.Sin(i * 0.37) * 0.5 + 0.5, 3) + i % 37)];
}

<MokaHistogram Values="_times" BucketSize="25" Origin="0" Percentiles="@(new double[] { 50, 95 })"
               Unit="MokaChartUnit.Milliseconds" CountLabel="requests" Look="MokaChartLook.Quiet"
               Title="Response times" />
```

The x axis is labelled at the buckets' edges in `Unit`; the y axis counts. Pointing at a bucket says its range, its
count and its share: "200 ms to 225 ms: 1,204 requests, 8%". The table lists each bucket's edges and count.

## Bucket size

Without `BucketSize` the chart picks one from the spread of the middle half of the values (Freedman and Diaconis),
rounded to a step in the values' unit: 10 ms rather than 9.7 ms, 5 s rather than 4,300 ms. More than `MaxBins`
buckets, or more than the plot has three pixels each for, widen every bucket by a whole factor, so a narrow card shows
fewer, wider buckets rather than slivers.

## Large inputs

Counting is one pass over the values; percentiles sort them once. A million values count in a few milliseconds. Pass
`DataVersion` and change it with the data, so the chart compares a version instead of copying the values to compare
them. The chart's marks are one bar per bucket, never per value.

```razor
<MokaHistogram Values="_samples" DataVersion="_version" Unit="MokaChartUnit.Milliseconds" />
```
