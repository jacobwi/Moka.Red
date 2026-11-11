---
title: Chart Annotations
description: Thresholds, target bands, event markers and windows drawn over any cartesian chart.
order: 58
---

# Chart Annotations

Any cartesian chart takes `Annotations`, a list of `MokaChartAnnotation` records, and a target band through `TargetLow`
and `TargetHigh`. They explain the data rather than add to it: a limit, a range to stay in, a deploy, a maintenance
window. The value axis always reaches a threshold or a band, and the tooltip says where a point stands against them.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Annotations` | `IReadOnlyList<MokaChartAnnotation>?` | -- | Thresholds, bands, events and windows |
| `TargetLow`, `TargetHigh` | `double?` | -- | A target band; one alone runs to the axis's end |

Every annotation has `Label`, `Tone` (a `MokaColor`, `Warning` by default) and `Key`.

| Record | Drawn | Tooltip note |
|---|---|---|
| `MokaChartThreshold(Value)` | a dashed line in its tone, labelled at its end with `Label`, else the value as the axis writes it | "62 ms over the 200 ms line" |
| `MokaChartBand(Low, High)` | a soft band, labelled inside its top | "Inside target", "12 ms above target", "40 ms below target" |
| `MokaChartEvent(At)` | a marker under the plot in its shape (`Diamond`, `Triangle`, `Circle`, `Square`) and tone; pointing at it or tabbing to it shows its tooltip, a guide line through the plot, and with `To` the span it covered | its `Label`, for a point near it or inside its span |
| `MokaChartWindow(From, To)` | a hatched span, labelled above the plot. With `BreakLine` (the default) values inside it are drawn as gaps | its `Label` |

Places along the x axis are `MokaChartX`: a `DateTimeOffset` on a time axis, a number on a number axis, a category's
index otherwise. Each converts on its own: `new MokaChartEvent(deployedAt)`.

## Threshold and target

```blazor-preview
<MokaLineChart Labels="@(new[] { "00:00", "04:00", "08:00", "12:00", "16:00", "20:00" })"
               Series="@(new[] { new MokaLineSeries { Name = "p95", Values = new[] { 140d, 120, 180, 262, 210, 150 } } })"
               Unit="MokaChartUnit.Milliseconds"
               TargetLow="100" TargetHigh="250"
               Annotations="@(new MokaChartAnnotation[] { new MokaChartThreshold(200) { Label = "Limit 200 ms" } })"
               Look="MokaChartLook.Quiet" />
```

## Events and windows

```blazor-preview
@code {
    static DateTimeOffset _day = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
    DateTimeOffset[] _times = [.. Enumerable.Range(0, 73).Select(i => _day.AddMinutes(15 * i))];
    MokaLineSeries[] _series = [new() { Name = "p95", Values = [.. Enumerable.Range(0, 73).Select(i => 140 + 60 * Math.Sin(i / 9d))] }];
    MokaChartAnnotation[] _notes =
    [
        new MokaChartWindow(_day.AddHours(2), _day.AddHours(4)) { Label = "Maintenance" },
        new MokaChartEvent(_day.AddHours(9)) { Label = "Deploy 0.3.23", Detail = "Rolled back the cache change", Tone = MokaColor.Info },
        new MokaChartEvent(_day.AddHours(12.25)) { Label = "Incident", To = _day.AddHours(13.25), Tone = MokaColor.Error, Shape = MokaChartMarker.Triangle }
    ];
}

<MokaLineChart Times="@_times" Series="@_series" Annotations="@_notes" TimeZone="TimeZoneInfo.Utc"
               Unit="MokaChartUnit.Milliseconds" Look="MokaChartLook.Quiet" />
```

Keep the list in a field: a new list every render counts as a change and draws the chart again.

## Accessibility

Each event marker is a button named by its label and time ("Deploy 0.3.23, 09:00") in a `MokaTooltip`, so it is a tab
stop and screen readers hear its name; the guide line and span show on keyboard focus as well as the pointer, by CSS
alone. Thresholds, bands and windows are drawn for the eye and reach screen readers through the tooltip notes, which the
keyboard grid's cells read with each point.
