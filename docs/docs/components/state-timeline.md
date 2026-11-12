---
title: State Timeline
description: The "is it up" chart. One row per service, device or link, a band along a shared clock colored by the state it was in, and each row's uptime.
order: 66
---

# State Timeline

`MokaStateTimeline` draws one row per thing (a service, a device, a link) with a band along a shared clock, colored by
the state it was in: up, degraded, down, in maintenance, or unknown. States mean good and bad, so each takes a status
color and carries an icon and a word in the legend and the tooltip, and its name in the table, so a state never rests
on color alone. The legend hides and isolates states. Each row can end with its uptime.

Where `MokaGanttChart` draws one bar per job, the state timeline draws a row's whole history, however many spans it has.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Rows` | `IReadOnlyList<MokaStateRow>?` | -- | The rows, one per thing, in order |
| `States` | `IReadOnlyList<MokaTimelineState>?` | -- | What the spans' state keys mean, in legend order. Null uses the built-in states |
| `From`, `To` | `DateTimeOffset?` | -- | The shared clock. Null takes `XAxis.MinTime` and `MaxTime`, else spans the data, an open span ending now |
| `Now` | `DateTimeOffset?` | -- | Where open spans end, where unknown time stops and the line is drawn. Null reads a registered `TimeProvider`, else the clock |
| `ShowNow` | `bool` | `true` | Whether a line marks now when it falls on the clock |
| `ShowUptime` | `bool` | `false` | Whether each row ends with the share of its known time it spent in a good state |
| `UptimeFormatter` | `Func<double, string>?` | -- | Writes an uptime from a share, 0 to 1. Defaults to two decimals that never round up to 100%: "99.95%" |
| `DurationText` | `Func<TimeSpan, string>?` | -- | Writes how long a span lasted. Defaults to "41 min", "2 h 5 min" |
| `TimelineText` | `MokaStateTimelineText?` | -- | The words beyond the shared chart words. Null is English |
| `TimeZone` | `TimeZoneInfo?` | -- | The zone the clock is written in. Null uses a registered `TimeProvider`'s, else the server's |
| `MaxHeight` | `string?` | -- | The height the rows scroll in, with the clock sticking on top |

It also takes the parameters every chart shares (`Title`, `Look`, `Tooltip`, `Keyboard`, `Legend`, `HiddenSeries`,
`Highlight`, `OnPointClick`, `ActiveChanged`, `Controls` and the rest), `XAxis` for the clock (`TimeFormat`, `Reverse`,
`MinTime`, `MaxTime`), and `Annotations`, which run down the rows at a time: a `MokaChartThreshold`, `MokaChartBand` or
`MokaChartWindow` whose values are Unix milliseconds (`MokaChartX.FromDateTimeOffset(deadline).Value`).

### MokaStateRow and MokaStateSpan

| Name | Type | Description |
|------|------|-------------|
| `MokaStateRow.Name` | `string` | What the row is, such as "api-gateway" |
| `MokaStateRow.Spans` | `IReadOnlyList<MokaStateSpan>` | When each state started, in any order |
| `MokaStateRow.Key` | `string?` | Its identity for highlighting and in `MokaChartPoint.RowKey`. Null uses `Name` |
| `MokaStateRow.Detail` | `string?` | A line for the tooltip, such as "10.0.0.5, eu-west" |
| `MokaStateSpan.Start` | `DateTimeOffset` | When the state started |
| `MokaStateSpan.State` | `string` | The state's key |
| `MokaStateSpan.End` | `DateTimeOffset?` | When it ended. Null lasts until the row's next span without an end, or now |
| `MokaStateSpan.Detail` | `string?` | A line for the tooltip, such as "HTTP 503 from upstream" |

`MokaStateSpan` is a record struct, so a long history costs no allocation per span.

### States

Without `States` the chart knows five keys, each a constant on `MokaStateTimeline`:

| Key | Constant | Drawn |
|-----|----------|-------|
| `up` | `UpKey` | the success color, a check |
| `degraded` | `DegradedKey` | the warning color, a warning sign |
| `down` | `DownKey` | the error color, a cross |
| `maintenance` | `MaintenanceKey` | the info color, a wrench |
| `unknown` | `UnknownKey` | hatched in a faint color, a question mark |

`MokaTimelineState(Key, Name)` names a state of your own:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Tone` | `MokaStateTone` | `Good` | `Good`, `Warn`, `Bad`, `Info`, `Off` or `Unknown`: its color and icon. Only `Good` counts as up |
| `ColorValue` | `string?` | -- | A CSS color or token that wins over the tone's |
| `Icon` | `MokaIconDefinition?` | -- | Its icon in the legend and the tooltip. Null takes the tone's |
| `Severity` | `int?` | -- | Which state shows where two overlap or a pixel holds several. Null takes the tone's: Unknown 0, Good 10, Info 20, Off 30, Warn 40, Bad 50 |
| `Hatched` | `bool?` | -- | Whether it is drawn hatched. Null hatches only the `Unknown` tone |

A key that isn't listed is a built-in state when it is one, else a state named by its key in the muted color.

```razor
<MokaStateTimeline Rows="_links" States="_states" ShowUptime Title="WAN links" />

@code {
    MokaTimelineState[] _states =
    [
        new("online", "Online") { Tone = MokaStateTone.Good },
        new("flapping", "Flapping") { Tone = MokaStateTone.Warn },
        new("offline", "Offline") { Tone = MokaStateTone.Bad },
        new("asleep", "Asleep") { Tone = MokaStateTone.Off, Icon = MokaIcons.Status.Clock },
        new("nodata", "No data") { Tone = MokaStateTone.Unknown }
    ];
}
```

## Is it up

```razor
<MokaStateTimeline Rows="_services" From="_now.AddHours(-24)" To="_now" Now="_now" ShowUptime
                   Title="Is it up" Subtitle="Last 24 hours" />

@code {
    static readonly DateTimeOffset _now = new(2026, 11, 12, 14, 30, 0, TimeSpan.Zero);

    MokaStateRow[] _services =
    [
        new("api-gateway",
        [
            new(_now.AddHours(-24), MokaStateTimeline.UpKey),
            new(_now.AddHours(-7), MokaStateTimeline.DownKey) { Detail = "HTTP 503 from upstream" },
            new(_now.AddHours(-7).AddMinutes(4), MokaStateTimeline.UpKey)
        ]),
        new("db-primary",
        [
            new(_now.AddHours(-24), MokaStateTimeline.UpKey),
            new(_now.AddHours(-11), MokaStateTimeline.MaintenanceKey) { End = _now.AddHours(-10.25), Detail = "Index rebuild" }
        ]) { Detail = "eu-west-1" },
        new("mail-relay", [new(_now.AddHours(-15), MokaStateTimeline.UpKey)])
    ];
}
```

The maintenance window has an end, so it sits over the up span around it, and db-primary is up again once it ends.
mail-relay has no data before its first span, so that stretch is hatched as unknown and left out of its uptime.

## How spans resolve

A row's spans are resolved once each time the data changes, never per render:

- They are sorted by start, so they can arrive in any order.
- A span without an end lasts until the next span without an end starts, and the last one until now. State changes can
  be passed as they happened, and a span with an end (an incident, a maintenance window) sits over whatever runs around
  it rather than cutting it off.
- Where spans overlap, the more severe state shows, and between equals the one that started later.
- Time no span covers is in the unknown state, drawn hatched, but never past now: time still to come stays empty.

The uptime is the share of the row's known time on the clock, up to now, spent in a `Good` state. Unknown time is left
out, and the tooltip says how much there was: "Uptime 99.95%, 1 h 20 min unknown". A state in maintenance counts as known
and not up; give your maintenance state the `Good` tone if it shouldn't count against the uptime.

## Short outages and dense rows

A stretch shorter than a quantum of time (about two pixels of the track, rounded to a step of 1, 1.25, 1.5, 2, 2.5, 3,
4, 5, 6 or 8 times a power of ten milliseconds) still shows, at least two pixels wide over its neighbours, so a four second
outage on a day's chart is never lost. Where many such stretches sit side by side, they gather into one mark per quantum
showing the most severe state among them, and its tooltip says what it holds: "Down 3 min, Degraded 12 min, Up 45 min".
Past 100 stretches in a row the chart measures its width, so a wider chart gathers less.

## Tooltip, keyboard and clicks

Each mark is its own target: the tooltip names the row, the state with its icon, how long it lasted, from when to when,
the span's detail and the row's. With the keyboard (on a look that takes it, or `Keyboard="true"`), Left and Right walk
the marks along a row and Up and Down move between rows, to the mark at the same time in the next row (a row with no
marks is skipped). Home and End go to the ends of the row, Control with Home or End to the first and the last mark of the
chart, and Page Up and Page Down a tenth of the row at a time. A mark in a row that isn't drawn yet, past 200 rows, scrolls
its row into view and is lit once the row arrives. The keys need where each mark sits, which the chart sends the first
time the grid takes the focus, once per data change.

`OnPointClick` and `ActiveChanged` get a `MokaChartPoint` for the span: `SeriesKey` and `SeriesName` its state, `Time` and
`X` its start (`X` in Unix milliseconds), `Value` its length in milliseconds, `Category` the row's name, `Index` the row's
position in `Rows`, `RowKey` the row's key and `Row` its drawn row, from 0.

```razor
<MokaStateTimeline Rows="_services" Look="MokaChartLook.Quiet" OnPointClick="ShowIncident" />

@code {
    void ShowIncident(MokaChartPoint point) =>
        _selected = (point.RowKey, point.SeriesKey, point.Time, TimeSpan.FromMilliseconds(point.Value));
}
```

The table view lists every span: name, state, from, to and duration, with ISO 8601 times and seconds in the CSV.

## Live data

The chart has no timer of its own. Pass a new `Now` (and `From` and `To` for a window that slides) when your data
changes, such as from a `PeriodicTimer`, and set `DataVersion` when the rows are long: without it the chart copies the
rows to compare them, which costs a few milliseconds a change at 100,000 spans.

Marks are placed in quanta from a fixed origin, and only the axis's start is written again as a window slides, so a tick
in which no state changed sends the ends of the rows and the clock, not every mark. A row whose number of marks changed
renumbers the marks after it, so a tick in which states change costs more the more marks follow.

## Many rows

Rows are HTML. Past 200 rows they go through Blazor's `Virtualize` with an exact row height, so a thousand rows draw about
thirty at a time.
