---
title: Meter
description: Gauge/meter visualization for displaying a value within a range.
order: 36
---

# Meter

`MokaMeter` renders a gauge-style meter that visualizes a value within a defined range. It supports colored segments for threshold zones (e.g., green/yellow/red), value formatting, and optional labels.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value |
| `Min` | `double` | `0` | Minimum value of the range |
| `Max` | `double` | `100` | Maximum value of the range |
| `Label` | `string?` | -- | Label text displayed below the meter |
| `ShowValue` | `bool` | `true` | Shows the current value as text |
| `Format` | `string?` | -- | .NET format string for the displayed value (e.g., `"P0"`, `"N1"`) |
| `Color` | `MokaColor` | `Primary` | Default fill color (overridden by segments when present) |
| `Size` | `MokaSize` | `Md` | Meter size |
| `Segments` | `IReadOnlyList<MokaMeterSegment>?` | -- | Colored zones within the meter |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaMeterSegment

| Name | Type | Description |
|------|------|-------------|
| `FromPercent` | `double` | Start of the zone as a percentage (0--100) |
| `ToPercent` | `double` | End of the zone as a percentage (0--100) |
| `Color` | `MokaColor` | Color for this zone |

## Basic Percentage

```blazor-preview
<MokaMeter Value="72" />
```

## With Label

```blazor-preview
<MokaMeter Value="45" Max="100" Label="CPU Usage" Format="N0" />
```

## Colored Segments

Define threshold zones with different colors for visual feedback.

```blazor-preview
<MokaMeter Value="78" Label="Disk Usage" Segments="@segments" />

@code {
    IReadOnlyList<MokaMeterSegment> segments = new[]
    {
        new MokaMeterSegment { FromPercent = 0, ToPercent = 60, Color = MokaColor.Success },
        new MokaMeterSegment { FromPercent = 60, ToPercent = 85, Color = MokaColor.Warning },
        new MokaMeterSegment { FromPercent = 85, ToPercent = 100, Color = MokaColor.Error }
    };
}
```

## Custom Range

```blazor-preview
<MokaMeter Value="37.5" Min="0" Max="50" Label="Temperature (C)" Format="N1" Color="MokaColor.Info" />
```
