---
title: Select
description: Dropdown select with single/multi selection, search, grouping, loading state, and chip templates.
order: 9
---

# Select

`MokaSelect<TValue>` is a full-featured dropdown component. It supports keyboard navigation, single and multiple selection, searchable filtering, option grouping, a "Select All" checkbox, custom chip rendering in multi-select mode, and a loading state for async option sources.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `TValue?` | — | Selected value (two-way bindable) |
| `ValueChanged` | `EventCallback<TValue?>` | — | Notified when value changes |
| `SelectedValues` | `IList<TValue>?` | — | Multi-select values (two-way bindable) |
| `SelectedValuesChanged` | `EventCallback<IList<TValue>>` | — | Notified when multi-selection changes |
| `Items` | `IEnumerable<TValue>` | **required** | Available options |
| `Label` | `string?` | — | Field label |
| `HelperText` | `string?` | — | Help text below the field |
| `ErrorText` | `string?` | — | Error message (puts field in error state) |
| `Required` | `bool` | `false` | Marks the field as required |
| `Disabled` | `bool` | `false` | Disables interaction |
| `Placeholder` | `string?` | — | Placeholder text when nothing is selected |
| `ValueSelector` | `Func<TValue, string>?` | — | Converts an item to display string. Default: `ToString()` |
| `Searchable` | `bool` | `false` | Shows a search input inside the dropdown |
| `Clearable` | `bool` | `false` | Shows an X to clear the selection |
| `Multiple` | `bool` | `false` | Enables multi-selection with chips |
| `SelectAll` | `bool` | `false` | "Select All" checkbox (requires `Multiple`) |
| `GroupBy` | `Func<TValue, string?>?` | — | Groups options under labeled headers |
| `IsOptionDisabled` | `Func<TValue, bool>?` | — | Per-option disabled predicate |
| `ChipTemplate` | `RenderFragment<TValue>?` | — | Custom chip renderer for multi-select |
| `Loading` | `bool` | `false` | Shows loading indicator in dropdown |
| `NoResultsText` | `string` | `"No options found"` | Empty search results message |
| `Size` | `MokaSize` | `Md` | Field size |
| `Variant` | `MokaVariant` | `Outlined` | Field visual variant |

## Single Select

```blazor-preview
@code {
    string? _role;
    string[] _roles = ["Admin", "Editor", "Viewer", "Guest"];
}

<MokaSelect @bind-Value="_role" Label="Role" Items="_roles" Placeholder="Select a role" Clearable />
```

## With ValueSelector

When `TValue` is a complex object, use `ValueSelector` to control what text appears in the trigger.

```blazor-preview
@code {
    record Country(int Id, string Name, string Code);
    Country? _selected;
    Country[] _countries = [
        new(1, "Germany", "DE"),
        new(2, "France", "FR"),
        new(3, "Italy", "IT"),
    ];
}

<MokaSelect @bind-Value="_selected"
            Label="Country"
            Items="_countries"
            ValueSelector="c => c.Name"
            Clearable />
```

## Searchable

```blazor-preview
@code {
    string? _city;
    string[] _cities = ["Berlin", "Paris", "Rome", "Madrid", "Warsaw", "Vienna", "Lisbon"];
}

<MokaSelect @bind-Value="_city" Label="City" Items="_cities" Searchable Clearable />
```

## Multiple Selection

```blazor-preview
@code {
    IList<string> _selected = new List<string>();
    string[] _skills = ["Blazor", "C#", "TypeScript", "CSS", "SQL", "Docker"];
}

<MokaSelect Multiple
            @bind-SelectedValues="_selected"
            Label="Skills"
            Items="_skills"
            Searchable
            SelectAll
            Placeholder="Choose skills" />
```

## Grouped Options

```blazor-preview
@code {
    record MenuItem(string Name, string Category);
    MenuItem? _item;
    MenuItem[] _menu = [
        new("Espresso", "Coffee"),
        new("Latte", "Coffee"),
        new("Cappuccino", "Coffee"),
        new("Green Tea", "Tea"),
        new("Earl Grey", "Tea"),
        new("Orange Juice", "Juice"),
    ];
}

<MokaSelect @bind-Value="_item"
            Label="Drink"
            Items="_menu"
            ValueSelector="x => x.Name"
            GroupBy="x => x.Category"
            Clearable />
```

## Disabled Options

```blazor-preview
@code {
    string? _plan;
    string[] _plans = ["Free", "Pro", "Enterprise", "Custom"];
    bool IsDisabled(string p) => p == "Custom";
}

<MokaSelect @bind-Value="_plan"
            Label="Plan"
            Items="_plans"
            IsOptionDisabled="IsDisabled" />
```

## Loading State

```blazor-preview
@code {
    string? _user;
    string[] _users = [];
    bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(1500);
        _users = ["Alice", "Bob", "Carol", "Dave"];
        _loading = false;
    }
}

<MokaSelect @bind-Value="_user" Label="Assignee" Items="_users" Loading="_loading" />
```

## Custom Chip Template

Use `ChipTemplate` to control how selected items render as chips in multi-select mode.

```blazor-preview
@code {
    IList<string> _selected = new List<string>();
    string[] _tags = ["urgent", "bug", "enhancement", "wontfix", "duplicate"];
}

<MokaSelect Multiple
            @bind-SelectedValues="_selected"
            Label="Labels"
            Items="_tags">
    <ChipTemplate Context="tag">
        <MokaChip Color="MokaColor.Primary" Size="MokaSize.Sm">@tag</MokaChip>
    </ChipTemplate>
</MokaSelect>
```

## Error State

```blazor-preview
<MokaSelect Label="Department"
            Items='new[] { "Engineering", "Design", "Product" }'
            ErrorText="Please select a department." />
```
