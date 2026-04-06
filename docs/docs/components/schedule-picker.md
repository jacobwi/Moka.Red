---
title: Schedule Picker
description: Weekly time-slot grid for selecting recurring availability windows.
order: 79
---

# Schedule Picker

`MokaSchedulePicker` renders an interactive weekly grid where users click or drag to select time slots. Use it for meeting schedulers, availability planners, or recurring task configuration.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `SelectedSlots` | `IReadOnlyList<MokaTimeSlot>` | `[]` | Currently selected time slots (two-way bindable) |
| `SelectedSlotsChanged` | `EventCallback<IReadOnlyList<MokaTimeSlot>>` | -- | Callback when selection changes |
| `StartHour` | `int` | `9` | First visible hour (0-23) |
| `EndHour` | `int` | `17` | Last visible hour (0-23) |
| `SlotDuration` | `int` | `60` | Slot duration in minutes (30 or 60) |
| `Days` | `IReadOnlyList<DayOfWeek>?` | Mon-Fri | Days to display |
| `ReadOnly` | `bool` | `false` | Disables interaction |
| `Color` | `MokaColor` | `Primary` | Selected slot color theme |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaTimeSlot

| Property | Type | Description |
|----------|------|-------------|
| `Day` | `DayOfWeek` | Day of the week |
| `Hour` | `int` | Hour (0-23) |
| `Minute` | `int` | Minute (0 or 30) |

## Basic Weekday 9-5

```blazor-preview
<MokaSchedulePicker />
```

## Full Week

```blazor-preview
<MokaSchedulePicker Days="@_fullWeek" StartHour="8" EndHour="20" />

@code {
    private IReadOnlyList<DayOfWeek> _fullWeek = new[]
    {
        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
        DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
    };
}
```

## 30-Minute Slots

```blazor-preview
<MokaSchedulePicker SlotDuration="30" StartHour="9" EndHour="12" Color="MokaColor.Success" />
```
