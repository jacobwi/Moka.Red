---
title: Toggle Group
description: Multi-select toggle button group for selecting one or many options.
order: 57
---

# Toggle Group

`MokaToggleGroup` renders a row of toggle buttons that can operate in single-select or multi-select mode. Each item is defined with `MokaToggleGroupItem`. Useful for view switchers, filter bars, and formatting toolbars.

## Parameters

### MokaToggleGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | `MokaToggleGroupItem` children |
| `Value` | `string?` | `null` | Selected value in single-select mode (two-way bindable) |
| `Values` | `IReadOnlyList<string>` | `[]` | Selected values in multi-select mode (two-way bindable) |
| `Multiple` | `bool` | `false` | Enable multi-select mode |
| `Size` | `MokaSize` | `Md` | Button size |
| `Color` | `MokaColor` | `Primary` | Color of selected items |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaToggleGroupItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | **required** | Unique identifier for this item |
| `Text` | `string?` | `null` | Display text |
| `Icon` | `MokaIconDefinition?` | `null` | Optional icon |

## Single Select

```blazor-preview
<MokaToggleGroup @bind-Value="_selected">
    <MokaToggleGroupItem Value="list" Text="List" />
    <MokaToggleGroupItem Value="grid" Text="Grid" />
    <MokaToggleGroupItem Value="board" Text="Board" />
</MokaToggleGroup>

@code {
    private string? _selected = "list";
}
```

## Multi Select

```blazor-preview
<MokaToggleGroup @bind-Values="_selected" Multiple="true" Color="MokaColor.Secondary">
    <MokaToggleGroupItem Value="bold" Text="Bold" />
    <MokaToggleGroupItem Value="italic" Text="Italic" />
    <MokaToggleGroupItem Value="underline" Text="Underline" />
</MokaToggleGroup>

@code {
    private IReadOnlyList<string> _selected = new[] { "bold" };
}
```

## With Icons

```blazor-preview
<MokaToggleGroup @bind-Value="_view" Size="MokaSize.Sm">
    <MokaToggleGroupItem Value="list" Icon="MokaIcons.Content.List" />
    <MokaToggleGroupItem Value="grid" Icon="MokaIcons.Content.Grid" />
</MokaToggleGroup>

@code {
    private string? _view = "list";
}
```
