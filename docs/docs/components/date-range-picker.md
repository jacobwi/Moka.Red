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

Once both dates are set, the field reads `start - end` in `Format`.

## Keyboard

Down Arrow or Space on the field opens the calendar. Escape closes it and puts focus back on the field, and stops there, so a dialog around the picker stays open. The field reports `aria-haspopup="dialog"` and `aria-expanded`.

## Id, Class and Attributes

`Id` and unmatched attributes go on the text field, the control that takes focus, and the label's `for` follows `Id`. `Class`, `Style` and `Margin` go on the outer element, around the label, and `Padding` and `Rounded` on the text field.

```razor
<MokaDateRangePicker @bind-StartDate="_from" @bind-EndDate="_to" Id="trip" Label="Trip"
                     aria-describedby="trip-note" />
<p id="trip-note">Nights are counted from the first date.</p>
```

Up to 0.1.12 `Id` and the attributes went on the outer element, and the label pointed at an id of the picker's own.

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
