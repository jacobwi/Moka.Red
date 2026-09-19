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
| `AvailableItems` | `IList<TItem>` | `[]` | Items shown in the left (available) list. Transfers move items between the two lists in place, so pass mutable lists such as `List<T>` |
| `SelectedItems` | `IList<TItem>` | `[]` | Items shown in the right (selected) list |
| `ItemTemplate` | `RenderFragment<TItem>?` | -- | Custom template for rendering each item |
| `AvailableTitle` | `string` | `"Available"` | Header text for the left list |
| `SelectedTitle` | `string` | `"Selected"` | Header text for the right list |
| `Searchable` | `bool` | `false` | Shows search inputs above each list |
| `OnTransfer` | `EventCallback` | -- | Invoked after items move in either direction |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of both list panels |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the component |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the two lists and the buttons between them |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`Rounded` shapes the two list panels, the boxes the component draws. Up to 0.1.12 it went on the element around them, which draws nothing, so it did not show.

## Basic String List

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="available"
                  SelectedItems="selected" />

@code {
    List<string> available = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];
    List<string> selected = [];
}
```

## Custom Item Template

Use `ItemTemplate` to customize how each item is rendered in both lists.

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="langs"
                  SelectedItems="chosenLangs"
                  AvailableTitle="Languages"
                  SelectedTitle="My Stack">
    <ItemTemplate>
        <MokaFlexbox Align="MokaAlign.Center" Gap="MokaSpacingScale.Xs">
            <MokaIcon Icon="MokaIcons.File.Code" Size="MokaSize.Sm" />
            <MokaText>@context</MokaText>
        </MokaFlexbox>
    </ItemTemplate>
</MokaTransferList>

@code {
    List<string> langs = ["C#", "TypeScript", "Python", "Rust", "Go"];
    List<string> chosenLangs = [];
}
```

## Searchable

Enable `Searchable` to let users filter items in both lists.

```blazor-preview
<MokaTransferList TItem="string"
                  AvailableItems="cities"
                  SelectedItems="visitedCities"
                  AvailableTitle="All Cities"
                  SelectedTitle="Visited"
                  Searchable="true" />

@code {
    List<string> cities = ["Tokyo", "London", "New York", "Paris", "Sydney", "Berlin", "Toronto"];
    List<string> visitedCities = [];
}
```
