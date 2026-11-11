---
title: Gantt Chart
description: One row per job with a bar from start to end on a shared clock, status colors with an icon and a word, and a line at now.
order: 65
---

# Gantt Chart

`MokaGanttChart` draws one row per job with a bar from its start to its end on a shared clock, for runs, backups and
schedules, and a line at now. Done and failed mean good and bad, so the bars take status colors, and each carries an icon
and a word so the status never rests on color alone. The legend lists the statuses, and hiding one takes its jobs away.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Rows` | `IReadOnlyList<MokaGanttRow>?` | -- | The jobs, one row each, in order |
| `From`, `To` | `DateTimeOffset?` | -- | The shared clock. Null takes `XAxis.MinTime` and `MaxTime`, else spans the jobs, a running job ending now |
| `Now` | `DateTimeOffset?` | -- | The time now, where running jobs end and the line is drawn. Null reads a registered `TimeProvider`, else the clock |
| `ShowNow` | `bool` | `true` | Whether a line marks now when it falls on the clock |
| `StatusText` | `Func<MokaGanttStatus, string>?` | -- | The word for a status, on the bars, in the legend and the tooltip |
| `NowText` | `string` | `"Now"` | The word before the time now, over the names |
| `DurationText` | `Func<TimeSpan, string>?` | -- | Writes how long a job took. Defaults to "41 min", "2 h 5 min" |
| `TookLabel`, `StatusLabel` | `string` | `"Took"`, `"Status"` | The table's headings |
| `TimeZone` | `TimeZoneInfo?` | -- | The zone the clock is written in. Null uses a registered `TimeProvider`'s, else the server's |
| `MaxHeight` | `string?` | -- | The height the rows scroll in, with the clock sticking on top |

It also takes the parameters every chart shares, and `XAxis` for the clock (`TimeFormat`, `Reverse`). `Annotations` run
down the rows at a time: a `MokaChartThreshold` or `MokaChartBand` whose values are Unix milliseconds
(`MokaChartX.FromDateTimeOffset(deadline).Value`).

### MokaGanttRow

| Name | Type | Description |
|------|------|-------------|
| `Name` | `string` | The job |
| `Start` | `DateTimeOffset` | When it started, or is planned to |
| `End` | `DateTimeOffset?` | When it ended, or is planned to. Null runs until now |
| `Status` | `MokaGanttStatus` | `Planned`, `Running`, `Done` (the default), `Failed` or `Skipped` |
| `Detail` | `string?` | A line for the tooltip, such as "Disk full on /index" |
| `Key` | `string?` | Its identity for highlighting. Null uses `Name` |

| Status | Drawn |
|--------|-------|
| Done | the success color, a check |
| Failed | the error color, an alert mark |
| Running | the info color, a play mark, the bar open at its end |
| Planned | a dashed outline in the neutral color, a clock |
| Skipped | a thin muted bar, a dash |

## Last night's jobs

```blazor-preview
@code {
    static readonly DateTimeOffset Midnight = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

    MokaGanttRow[] _jobs =
    [
        new("Photos backup", Midnight.AddHours(1)) { End = Midnight.AddMinutes(160) },
        new("Database dump", Midnight.AddMinutes(90)) { End = Midnight.AddMinutes(112) },
        new("Index rebuild", Midnight.AddMinutes(120))
            { End = Midnight.AddMinutes(165), Status = MokaGanttStatus.Failed, Detail = "Disk full on /index" },
        new("Offsite sync", Midnight.AddMinutes(170)) { Status = MokaGanttStatus.Running },
        new("Thumbnails", Midnight.AddMinutes(180)) { End = Midnight.AddMinutes(255) }
    ];
}

<MokaGanttChart Rows="_jobs" Now="Midnight.AddMinutes(310)" TimeZone="TimeZoneInfo.Utc" Title="Last night's jobs" />
```

The tooltip of the failed job reads "Index rebuild: 2:00 AM to 2:45 AM, 45 min, Failed: Disk full on /index".

## Now

A job without an end runs until now, and the line at now runs down every row, with the time written over the names where
it stays while the rows scroll. The chart reads now when its data changes and has no timer of its own, so pass a new `Now`
to move the line, such as from a `PeriodicTimer` once a minute.

## Many jobs

Rows are HTML. Past 200 rows they go through Blazor's `Virtualize` with an exact row height, so ten thousand jobs draw
about thirty at a time. Each word sits after its bar where there is room, before it where there isn't, and inside it when
the bar fills the row; on a short track the icon stays and the word goes.
