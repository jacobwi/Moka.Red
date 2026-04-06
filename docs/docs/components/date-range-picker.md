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
| `StartDate` | `DateTime?` | `null` | The selected start date (two-way bindable) |
| `EndDate` | `DateTime?` | `null` | The selected end date (two-way bindable) |
| `MinDate` | `DateTime?` | `null` | Earliest selectable date |
| `MaxDate` | `DateTime?` | `null` | Latest selectable date |
| `Label` | `string?` | `null` | Field label |
| `Placeholder` | `string?` | `null` | Placeholder text when no dates are selected |
| `Format` | `string` | `"yyyy-MM-dd"` | Date display format |
| `Disabled` | `bool` | `false` | Disables the input |
| `Size` | `MokaSize` | `Md` | Input size |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Date Range

```blazor-preview
<MokaDateRangePicker @bind-StartDate="_start" @bind-EndDate="_end" Label="Date Range" />

@code {
    private DateTime? _start = DateTime.Today;
    private DateTime? _end = DateTime.Today.AddDays(7);
}
```

## With Min/Max Constraints

```blazor-preview
<MokaDateRangePicker @bind-StartDate="_start"
                     @bind-EndDate="_end"
                     MinDate="DateTime.Today.AddDays(-30)"
                     MaxDate="DateTime.Today.AddDays(90)"
                     Label="Booking Window" />

@code {
    private DateTime? _start;
    private DateTime? _end;
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
    private DateTime? _start;
    private DateTime? _end;
}
```
