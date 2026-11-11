---
title: Dot Plot
description: A dot per row on a hairline for values that don't start at zero, with stems for a lollipop and a second value for a dumbbell.
order: 62
---

# Dot Plot

`MokaDotPlot` draws a dot per row on a hairline, for values that don't start at zero, such as uptime between 99% and
100% where bars would all look full. `Stems` turns it into a lollipop chart, and a `Before` value on the rows draws a
dumbbell: a pale dot, a dark dot and the line between them, for before and after.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Rows` | `IReadOnlyList<MokaDotPlotRow>?` | -- | The rows, drawn in order unless `Sort` says otherwise |
| `Stems` | `bool` | `false` | Draws a stem from `Baseline` to each dot: a lollipop chart |
| `Baseline` | `double` | `0` | Where the stems start. The axis reaches it while stems are drawn |
| `ZeroBased` | `bool` | `false` | Whether the axis starts at zero. Off, it spans the data on round numbers |
| `Sort` | `MokaChartSort?` | -- | Orders the rows by value, missing values last. Null keeps them as given |
| `BeforeLabel`, `AfterLabel` | `string` | `"Before"`, `"After"` | What a dumbbell's two dots are called, in the legend, the tooltip and the table |
| `ChangeLabel` | `string` | `"Change"` | The table's heading for the change |
| `UpIsGood` | `bool` | `true` | Whether a higher value is better, which decides which rows got worse |
| `MarkWorse` | `bool` | `false` | Draws the rows that got worse in the accent and the rest muted |
| `BetterText`, `WorseText` | `string` | `"better"`, `"worse"` | The tooltip's words for the direction of a change |
| `NameTemplate` | `RenderFragment<MokaDotPlotRow>?` | -- | Draws a row's name; the text still names it in the tooltip and the table |
| `MaxHeight` | `string?` | -- | The height the rows scroll in, with the value axis sticking on top |

It also takes the parameters every chart shares (`Title`, `Subtitle`, `Label`, `Look`, `Colors`, `Highlight`, `Tooltip`,
`Keyboard`, `Controls`, `Formatter`, `Unit`, `XAxis` for the value axis, `Annotations`, `TargetLow` and `TargetHigh` and the
rest). `Highlight` names a row's key: that row keeps its color and the rest are muted.

### MokaDotPlotRow

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | What the row is |
| `Value` | `double` | Its value, or the value after when `Before` is set |
| `Before` | `double?` | The value before, which turns the row into a dumbbell |
| `Color` | `MokaColor?` | The color of its dots. Null takes the chart's first palette color |
| `Detail` | `string?` | A line for the tooltip, such as "4 min down" |
| `Key` | `string?` | Its identity for highlighting. Null uses `Name` |

## Uptime

```blazor-preview
@code {
    MokaDotPlotRow[] _uptime =
    [
        new("Router", 99.99), new("DNS", 99.97), new("Mail relay", 99.93), new("VPN", 99.82),
        new("NAS", 99.64), new("Media server", 99.21) { Detail = "5 h 41 min down" }
    ];

    static readonly MokaChartAxis Percent = new() { Formatter = v => $"{v:0.##}%" };
}

<MokaDotPlot Rows="_uptime" XAxis="Percent" Sort="MokaChartSort.Descending" Title="Uptime by service" />
```

The axis starts on a round number just under the lowest value, so the spread between 99.21% and 99.99% fills the row.
Set `ZeroBased` to start it at zero.

## Lollipop

```razor
<MokaDotPlot Rows="_volumes" Stems ZeroBased Unit="@(MokaChartUnit.Of("GB"))" Title="Free space by volume" />
```

## Dumbbell

Give each row a `Before` value. With `UpIsGood="false"` a value that went up got worse, and `MarkWorse` draws those rows
in the accent and the rest muted, so the ones to look at stand out without a second color.

```blazor-preview
@code {
    MokaDotPlotRow[] _cache =
    [
        new("/api/feed", 260) { Before = 520 }, new("/api/search", 180) { Before = 420 },
        new("/api/files", 240) { Before = 310 }, new("/api/login", 150) { Before = 140 }
    ];
}

<MokaDotPlot Rows="_cache" UpIsGood="false" MarkWorse ZeroBased Unit="MokaChartUnit.Milliseconds"
             Title="p95 before and after the cache" />
```

The legend lists Before and After, and hiding one leaves the other as a plain dot plot. Their keys are
`MokaDotPlot.BeforeKey` and `MokaDotPlot.ValueKey`, for `HiddenSeries`. The tooltip reads "/api/feed: Before 520 ms, After
260 ms, -260 ms (-50%), better". On a narrow chart the "520 ms -> 260 ms" figures leave, and the tooltip and the table keep
them.

## Many rows

Rows are HTML positioned by percent on each row's track. Past 200 rows they go through Blazor's `Virtualize` with an exact
row height, so ten thousand rows draw about thirty at a time; set `MaxHeight` to scroll them inside the chart. Names stay
whole on one line, with the whole name in a tooltip, and move above their rows when the chart is too narrow for them. With
a tooltip or the keyboard on, the whole row is one target: Up and Down move between rows, Home and End go to the ends.
