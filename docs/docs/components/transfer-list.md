---
title: Transfer List
description: Dual-list picker for moving items between an available pool and a selected set.
order: 40
---

# Transfer List

`MokaTransferList<TItem>` renders two side-by-side lists with controls to move items between them. It supports custom item templates, optional search filtering, and configurable list titles.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `AvailableItems` | `IReadOnlyList<TItem>` | `[]` | Items shown in the left (available) list |
| `SelectedItems` | `IReadOnlyList<TItem>` | `[]` | Items shown in the right (selected) list |
| `SelectedItemsChanged` | `EventCallback<IReadOnlyList<TItem>>` | -- | Callback when the selected items change |
| `ItemTemplate` | `RenderFragment<TItem>?` | -- | Custom template for rendering each item |
| `AvailableTitle` | `string` | `"Available"` | Header text for the left list |
| `SelectedTitle` | `string` | `"Selected"` | Header text for the right list |
| `Searchable` | `bool` | `false` | Shows search inputs above each list |
| `OnTransfer` | `EventCallback<IReadOnlyList<TItem>>` | -- | Callback after items are transferred |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic String List

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="available"
                  @bind-SelectedItems="selected" />

@code {
    IReadOnlyList<string> available = new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" };
    IReadOnlyList<string> selected = Array.Empty<string>();
}
```

## Custom Item Template

Use `ItemTemplate` to customize how each item is rendered in both lists.

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="langs"
                  @bind-SelectedItems="chosenLangs"
                  AvailableTitle="Languages"
                  SelectedTitle="My Stack">
    <ItemTemplate>
        <MokaFlexbox Align="MokaAlign.Center" Gap="MokaSpacingScale.Xs">
            <MokaIcon Icon="MokaIcons.Content.Code" Size="MokaSize.Sm" />
            <MokaText>@context</MokaText>
        </MokaFlexbox>
    </ItemTemplate>
</MokaTransferList>

@code {
    IReadOnlyList<string> langs = new[] { "C#", "TypeScript", "Python", "Rust", "Go" };
    IReadOnlyList<string> chosenLangs = Array.Empty<string>();
}
```

## Searchable

Enable `Searchable` to let users filter items in both lists.

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="cities"
                  @bind-SelectedItems="visitedCities"
                  AvailableTitle="All Cities"
                  SelectedTitle="Visited"
                  Searchable="true" />

@code {
    IReadOnlyList<string> cities = new[] { "Tokyo", "London", "New York", "Paris", "Sydney", "Berlin", "Toronto" };
    IReadOnlyList<string> visitedCities = Array.Empty<string>();
}
```
