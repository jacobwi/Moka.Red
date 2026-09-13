---
title: Date Range Picker
description: Dual-date range selection input with calendar dropdowns.
order: 55
---

# Date Range Picker

`MokaDateRangePicker` provides a dual-date input for selecting a start and end date. Each date opens a calendar dropdown. Ideal for filtering, booking, and reporting scenarios.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `StartDate` | `DateOnly?` | `null` | The selected start date (two-way bindable) |
| `EndDate` | `DateOnly?` | `null` | The selected end date (two-way bindable) |
| `MinDate` | `DateOnly?` | `null` | Earliest selectable date |
| `MaxDate` | `DateOnly?` | `null` | Latest selectable date |
| `Label` | `string?` | `null` | Field label |
| `Placeholder` | `string` | `"Select date range"` | Placeholder text when no dates are selected |
| `Format` | `string` | `"yyyy-MM-dd"` | Date display format |
| `Disabled` | `bool` | `false` | Disables the input |
| `Size` | `MokaSize` | `Md` | Input size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Date Range

```blazor-preview
<MokaDateRangePicker @bind-StartDate="_start" @bind-EndDate="_end" Label="Date Range" />

@code {
    private DateOnly? _start = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? _end = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
}
```

## With Min/Max Constraints

```blazor-preview
<MokaDateRangePicker @bind-StartDate="_start"
                     @bind-EndDate="_end"
                     MinDate="_today.AddDays(-30)"
                     MaxDate="_today.AddDays(90)"
                     Label="Booking Window" />

@code {
    private static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
    private DateOnly? _start;
    private DateOnly? _end;
}
```

## Labeled with Custom Format

```blazor-preview
<MokaDateRangePicker @bind-StartDate="_start"
                     @bind-EndDate="_end"
                     Label="Report Period"
                     Format="dd/MM/yyyy"
                     Placeholder="Select dates..." />

@code {
    private DateOnly? _start;
    private DateOnly? _end;
}
```
