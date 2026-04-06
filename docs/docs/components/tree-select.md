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
        new MokaTreeSelectItem<string>
        {
            Text = "Fruits", Value = "fruits", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "Apple", Value = "apple" },
                new MokaTreeSelectItem<string> { Text = "Banana", Value = "banana" },
                new MokaTreeSelectItem<string> { Text = "Orange", Value = "orange" }
            }
        },
        new MokaTreeSelectItem<string>
        {
            Text = "Vegetables", Value = "vegetables", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "Carrot", Value = "carrot" },
                new MokaTreeSelectItem<string> { Text = "Broccoli", Value = "broccoli" }
            }
        }
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
        new MokaTreeSelectItem<string>
        {
            Text = "Engineering", Value = "eng", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "Frontend", Value = "fe" },
                new MokaTreeSelectItem<string> { Text = "Backend", Value = "be" },
                new MokaTreeSelectItem<string> { Text = "DevOps", Value = "devops" }
            }
        },
        new MokaTreeSelectItem<string>
        {
            Text = "Design", Value = "design", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "UX", Value = "ux" },
                new MokaTreeSelectItem<string> { Text = "Visual", Value = "visual" }
            }
        }
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
        new MokaTreeSelectItem<string>
        {
            Text = "North America", Value = "na", Children = new[]
            {
                new MokaTreeSelectItem<string>
                {
                    Text = "United States", Value = "us", Children = new[]
                    {
                        new MokaTreeSelectItem<string> { Text = "New York", Value = "ny" },
                        new MokaTreeSelectItem<string> { Text = "California", Value = "ca" }
                    }
                },
                new MokaTreeSelectItem<string> { Text = "Canada", Value = "ca-country" }
            }
        },
        new MokaTreeSelectItem<string>
        {
            Text = "Europe", Value = "eu", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "Germany", Value = "de" },
                new MokaTreeSelectItem<string> { Text = "France", Value = "fr" }
            }
        }
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
        new MokaTreeSelectItem<string>
        {
            Text = "Languages", Value = "lang", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "C#", Value = "csharp" },
                new MokaTreeSelectItem<string> { Text = "TypeScript", Value = "ts" },
                new MokaTreeSelectItem<string> { Text = "Python", Value = "python" }
            }
        },
        new MokaTreeSelectItem<string>
        {
            Text = "Frameworks", Value = "fw", Children = new[]
            {
                new MokaTreeSelectItem<string> { Text = "Blazor", Value = "blazor" },
                new MokaTreeSelectItem<string> { Text = "React", Value = "react" }
            }
        }
    };
}
```
