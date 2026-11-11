---
title: Candlestick Chart
description: Open, high, low and close per period, hollow on the way up and filled on the way down.
order: 59
---

# Candlestick Chart

`MokaCandlestickChart` draws where a price opened and closed in each period and how high and low it went, for prices
and rates. An up period is a hollow body in the up color and a down period a filled body in the down color, so the
direction reads without color. A hairline wick runs from the low to the high.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Candles` | `IReadOnlyList<MokaCandle>?` | -- | The periods, in time order |
| `Gapless` | `bool` | `true` | Whether periods without a candle, such as weekends, take no room. False leaves an empty slot for each |
| `ShowVolume` | `bool` | `false` | Thin bars along the bottom fifth for each period's `Volume` |
| `CandleText` | `MokaCandleText?` | -- | The words for open, high, low, close, volume, the table's time heading and the legend's up and down. English by default |
| `Formatter`, `Unit` | | -- | Write a price; without them prices keep as many decimals as they are given with |

It also takes what every chart shares: `Title`, `Subtitle`, `Look`, `Tooltip`, `Keyboard`, `Pinnable`, `Controls`,
`State`, `YAxis`, `XAxis` (`TimeFormat` writes the dates along the bottom), `TimeZone`, `Annotations` and the rest.

### MokaCandle

| Member | Type | Description |
|--------|------|-------------|
| `At` | `DateTimeOffset` | When the period starts |
| `Open`, `High`, `Low`, `Close` | `double` | The prices |
| `Volume` | `double?` | How much traded |

## Basic

```blazor-preview
@code {
    MokaCandle[] _days =
    [
        new(new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero), 1.0812, 1.0848, 1.0790, 1.0841),
        new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero), 1.0841, 1.0852, 1.0801, 1.0809),
        new(new DateTimeOffset(2026, 3, 11, 12, 0, 0, TimeSpan.Zero), 1.0809, 1.0833, 1.0795, 1.0828),
        new(new DateTimeOffset(2026, 3, 12, 12, 0, 0, TimeSpan.Zero), 1.0828, 1.0870, 1.0821, 1.0866)
    ];
}

<MokaCandlestickChart Candles="_days" Look="MokaChartLook.Quiet" Title="EUR to USD" />
```

The value axis spans the prices rather than starting at zero. Pointing at a candle says its open, high, low and close
and the change: "+0.0029 (+0.27%)". The legend's Up and Down entries hide or show either kind.

## Many periods

When there are more periods than the plot has three pixels each for, neighbours merge into one candle: the first open,
the highest high, the lowest low and the last close, with the volumes added. An interactive chart measures its plot for
this; a static page assumes 800 pixels. The merged candle's tooltip names its first and last day.

## Gaps in time

Markets close at weekends. By default those days take no room. With `Gapless="false"` the chart finds the usual step
between candles and leaves an empty slot wherever one is missing, so a week's gap reads as one.
