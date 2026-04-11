---
title: Tree Select
description: Hierarchical dropdown picker for selecting values from nested tree structures.
order: 39
---

# Tree Select

`MokaTreeSelect<TValue>` renders a dropdown picker that displays items in a hierarchical tree structure. It supports single and multiple selection, built-in search filtering, and nested item groups.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `TValue?` | -- | Currently selected value. Supports two-way binding via `@bind-Value`. |
| `ValueChanged` | `EventCallback<TValue?>` | -- | Callback when the selected value changes |
| `Items` | `IReadOnlyList<MokaTreeSelectItem<TValue>>` | `[]` | Hierarchical list of selectable items |
| `Placeholder` | `string?` | -- | Placeholder text when no value is selected |
| `Label` | `string?` | -- | Field label displayed above the picker |
| `Disabled` | `bool` | `false` | Disables the picker |
| `Searchable` | `bool` | `false` | Shows a search input to filter items |
| `Multiple` | `bool` | `false` | Enables multiple selection mode |
| `SelectedValues` | `IReadOnlyList<TValue>` | `[]` | List of selected values when `Multiple` is `true` |
| `SelectedValuesChanged` | `EventCallback<IReadOnlyList<TValue>>` | -- | Callback when the selected values change in multiple mode |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaTreeSelectItem&lt;TValue&gt;

| Name | Type | Description |
|------|------|-------------|
| `Value` | `TValue` | The item's value |
| `Text` | `string` | Display text |
| `Icon` | `MokaIconDefinition?` | Optional icon shown before the text |
| `Children` | `IReadOnlyList<MokaTreeSelectItem<TValue>>?` | Nested child items |
| `Disabled` | `bool` | Whether this item is disabled |

## Basic Single Select

```blazor-preview
<MokaTreeSelect TValue="string"
                @bind-Value="selected"
                Label="Category"
                Placeholder="Choose a category..."
                Items="categories" />

<MokaText Style="margin-top:8px">Selected: @(selected ?? "none")</MokaText>

@code {
    string? selected;

    IReadOnlyList<MokaTreeSelectItem<string>> categories = new[]
    {
        new MokaTreeSelectItem<string>("fruits", "Fruits", Children: new[]
        {
            new MokaTreeSelectItem<string>("apple", "Apple"),
            new MokaTreeSelectItem<string>("banana", "Banana"),
            new MokaTreeSelectItem<string>("orange", "Orange")
        }),
        new MokaTreeSelectItem<string>("vegetables", "Vegetables", Children: new[]
        {
            new MokaTreeSelectItem<string>("carrot", "Carrot"),
            new MokaTreeSelectItem<string>("broccoli", "Broccoli")
        })
    };
}
```

## With Search

Enable `Searchable` to let users filter the tree by typing.

```blazor-preview
<MokaTreeSelect TValue="string"
                @bind-Value="searchSelected"
                Label="Department"
                Placeholder="Search departments..."
                Searchable="true"
                Items="departments" />

@code {
    string? searchSelected;

    IReadOnlyList<MokaTreeSelectItem<string>> departments = new[]
    {
        new MokaTreeSelectItem<string>("eng", "Engineering", Children: new[]
        {
            new MokaTreeSelectItem<string>("fe", "Frontend"),
            new MokaTreeSelectItem<string>("be", "Backend"),
            new MokaTreeSelectItem<string>("devops", "DevOps")
        }),
        new MokaTreeSelectItem<string>("design", "Design", Children: new[]
        {
            new MokaTreeSelectItem<string>("ux", "UX"),
            new MokaTreeSelectItem<string>("visual", "Visual")
        })
    };
}
```

## Nested Items

Tree items can be nested to any depth.

```blazor-preview
<MokaTreeSelect TValue="string"
                @bind-Value="deepSelected"
                Label="Location"
                Placeholder="Pick a location..."
                Items="locations" />

@code {
    string? deepSelected;

    IReadOnlyList<MokaTreeSelectItem<string>> locations = new[]
    {
        new MokaTreeSelectItem<string>("na", "North America", Children: new[]
        {
            new MokaTreeSelectItem<string>("us", "United States", Children: new[]
            {
                new MokaTreeSelectItem<string>("ny", "New York"),
                new MokaTreeSelectItem<string>("ca", "California")
            }),
            new MokaTreeSelectItem<string>("ca-country", "Canada")
        }),
        new MokaTreeSelectItem<string>("eu", "Europe", Children: new[]
        {
            new MokaTreeSelectItem<string>("de", "Germany"),
            new MokaTreeSelectItem<string>("fr", "France")
        })
    };
}
```

## Multiple Selection

Set `Multiple="true"` to allow selecting several values at once.

```blazor-preview
<MokaTreeSelect TValue="string"
                Multiple="true"
                @bind-SelectedValues="multiSelected"
                Label="Skills"
                Placeholder="Select skills..."
                Items="skills" />

<MokaText Style="margin-top:8px">Selected: @string.Join(", ", multiSelected)</MokaText>

@code {
    IReadOnlyList<string> multiSelected = [];

    IReadOnlyList<MokaTreeSelectItem<string>> skills = new[]
    {
        new MokaTreeSelectItem<string>("lang", "Languages", Children: new[]
        {
            new MokaTreeSelectItem<string>("csharp", "C#"),
            new MokaTreeSelectItem<string>("ts", "TypeScript"),
            new MokaTreeSelectItem<string>("python", "Python")
        }),
        new MokaTreeSelectItem<string>("fw", "Frameworks", Children: new[]
        {
            new MokaTreeSelectItem<string>("blazor", "Blazor"),
            new MokaTreeSelectItem<string>("react", "React")
        })
    };
}
```
