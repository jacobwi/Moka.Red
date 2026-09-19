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
| `Value` | `TValue?` | -- | Selected value (two-way bindable) |
| `ValueChanged` | `EventCallback<TValue?>` | -- | Notified when value changes |
| `SelectedValues` | `IList<TValue>?` | -- | Multi-select values (two-way bindable) |
| `SelectedValuesChanged` | `EventCallback<IList<TValue>>` | -- | Notified when multi-selection changes |
| `Items` | `IEnumerable<TValue>` | **required** | Available options |
| `Label` | `string?` | -- | Field label |
| `HelperText` | `string?` | -- | Help text below the field |
| `ErrorText` | `string?` | -- | Error message (puts field in error state) |
| `Required` | `bool` | `false` | Marks the field as required |
| `Disabled` | `bool` | `false` | Disables interaction |
| `Placeholder` | `string?` | -- | Placeholder text when nothing is selected |
| `ValueSelector` | `Func<TValue, string>?` | -- | Converts an item to display string. Default: `ToString()` |
| `Searchable` | `bool` | `false` | Shows a search input inside the dropdown |
| `Clearable` | `bool` | `false` | Shows an X to clear the selection |
| `Multiple` | `bool` | `false` | Enables multi-selection with chips |
| `SelectAll` | `bool` | `false` | "Select All" checkbox (requires `Multiple`) |
| `GroupBy` | `Func<TValue, string?>?` | -- | Groups options under labeled headers |
| `IsOptionDisabled` | `Func<TValue, bool>?` | -- | Per-option disabled predicate |
| `ChipTemplate` | `RenderFragment<TValue>?` | -- | Custom chip renderer for multi-select |
| `Loading` | `bool` | `false` | Shows loading indicator in dropdown |
| `NoResultsText` | `string` | `"No options found"` | Empty search results message |
| `IsOpen` | `bool` | `false` | Whether the list is open (two-way bindable). The list still opens and closes itself; a value from the parent takes effect when it differs from the last one passed |
| `IsOpenChanged` | `EventCallback<bool>` | -- | Notified when the list opens or closes |
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

## A Null Option

A `null` in `Items` is a normal option, for "Any" or "None". Picking it shows its text instead of the placeholder, so give `ValueSelector` a name for it:

```razor
<MokaSelect TValue="string" @bind-Value="_owner" Label="Owner"
            Items="@(new string?[] { null, "Ana", "Ben" })"
            ValueSelector="@(o => o ?? "Anyone")" />
```

The placeholder shows only while the value is `null` and no option is `null`.

## Id, Class and Attributes

`Id` goes on the trigger, and the ids built from it follow: the label is `{Id}-label`, the option list `{Id}-listbox` and each option `{Id}-listbox-option-0` and on. Other attributes go on the trigger as well, so `aria-describedby` or a `data-` attribute lands on the control, and inside an `EditForm` the trigger gets `aria-invalid="true"` while the field fails validation. `Class` and `Style` go on the element around the trigger and the list.

```razor
<MokaSelect @bind-Value="_country" Id="country" Label="Country" Items="_countries"
            aria-describedby="country-note" />
<p id="country-note">Where the invoice goes.</p>
```

`Margin` goes on the field, around the label and the helper text. `Padding` and `Rounded` go on the trigger.

Up to 0.1.12 the select ignored `Id`, and every attribute except `aria-label`.

## Accessibility

The trigger has `role="combobox"` and takes its accessible name from `Label`. Without a visible label, pass `aria-label`. The placeholder is the last fallback. The open option list is a `role="listbox"` with the same name, and the trigger points at it with `aria-controls`.

```razor
<MokaSelect @bind-Value="_priority" Items="_priorities" aria-label="Priority" Placeholder="Any" />
```

Keyboard:

- Enter, Space or Down Arrow opens the list. The arrow keys move the highlight, Enter or Space picks the highlighted option, and Escape closes the list. None of these keys scroll the page.
- Escape stops at an open list, so a `MokaDialog` around the select stays open.
- Focus stays on the field while you move through the options, and screen readers announce the highlighted one (`aria-activedescendant`). Options report whether they are selected.
- With `Searchable`, focus moves into the search box when the list opens, so you can type straight away. Enter there picks the highlighted option without submitting a surrounding form, and Escape returns focus to the field.
- With `GroupBy`, the arrow keys follow the order on screen, and each group is announced by its name.
- A disabled select is out of the tab order and announced as unavailable. Its clear and chip buttons are disabled too.
