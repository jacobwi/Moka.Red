---
title: Calendar
description: Full month-view calendar grid for date selection and display.
order: 34
---

# Calendar

`MokaCalendar` renders an interactive month-view calendar grid. It supports date selection with two-way binding, min/max date constraints, a disabled-dates predicate, and configurable first day of week.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `DateOnly?` | -- | The selected date. Supports two-way binding via `@bind-Value`. |
| `ValueChanged` | `EventCallback<DateOnly?>` | -- | Callback when the selected date changes |
| `MinDate` | `DateOnly?` | -- | Earliest selectable date |
| `MaxDate` | `DateOnly?` | -- | Latest selectable date |
| `DisplayMonth` | `DateOnly?` | -- | The month to display (defaults to current month or `Value`'s month) |
| `FirstDayOfWeek` | `DayOfWeek` | `Sunday` | Which day starts the week |
| `ShowAdjacentMonthDays` | `bool` | `true` | Shows trailing/leading days from adjacent months |
| `HighlightToday` | `bool` | `true` | Visually highlights today's date |
| `DisabledDates` | `Func<DateOnly, bool>?` | -- | Predicate that returns `true` for dates that should be disabled |
| `OnDateClick` | `EventCallback<DateOnly>` | -- | Callback when any date cell is clicked |
| `Size` | `MokaSize` | `Md` | Calendar size |
| `Color` | `MokaColor` | `Primary` | Accent color for selected date and today highlight |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Calendar

```blazor-preview
<MokaCalendar @bind-Value="selectedDate" />
<MokaParagraph>Selected: @(selectedDate?.ToString("yyyy-MM-dd") ?? "none")</MokaParagraph>

@code {
    DateOnly? selectedDate;
}
```

## With Min/Max Dates

Restrict selection to a specific date range.

```blazor-preview
<MokaCalendar @bind-Value="date"
              MinDate="@(new DateOnly(2026, 4, 1))"
              MaxDate="@(new DateOnly(2026, 4, 30))" />

@code {
    DateOnly? date;
}
```

## Highlight Today

Today's date is highlighted by default. Set `HighlightToday="false"` to disable.

```blazor-preview
<MokaCalendar HighlightToday />
```

## Disabled Dates Predicate

Use a predicate function to disable specific dates, such as weekends.

```blazor-preview
<MokaCalendar @bind-Value="date"
              DisabledDates="@(d => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)" />

@code {
    DateOnly? date;
}
```

## First Day Monday

```blazor-preview
<MokaCalendar @bind-Value="date" FirstDayOfWeek="DayOfWeek.Monday" />

@code {
    DateOnly? date;
}
```
