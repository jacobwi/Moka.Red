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
| `Controlled` | `bool` | `false` | After raising its change callback, the control shows what the parent passes back, so a refused change reverts. See [Controlled](#controlled) |
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

## Controlled

By default the group shows the user's pick at once and keeps it, and follows `Value` or `Values` only when the parent passes a different one. A parent that can turn a change down, because a save failed or the choice isn't allowed, has no way to put the old pick back while it passes the same value.

`Controlled` makes the parent the owner: after raising its change callback, the control shows what the parent passes back, so a refused change reverts. The callback is `ValueChanged`, or `ValuesChanged` with `Multiple`. Set the value in the callback to accept the change, or leave it as it was to refuse it. A callback that awaits (a save) keeps the old pick showing until it sets the value, and `@bind-Value` accepts every change.

```razor
<MokaToggleGroup Value="_plan" ValueChanged="PickPlanAsync" Controlled>
    <MokaToggleGroupItem Value="free" Text="Free" />
    <MokaToggleGroupItem Value="team" Text="Team" />
    <MokaToggleGroupItem Value="enterprise" Text="Enterprise" />
</MokaToggleGroup>

@code {
    private string? _plan = "free";

    private async Task PickPlanAsync(string? plan)
    {
        if (await Billing.TryChangePlanAsync(plan))
        {
            _plan = plan;
        }
    }
}
```

`MokaSegmentedControl` and `MokaChip` take the same `Controlled`, with the same meaning.

## With Icons

```blazor-preview
<MokaToggleGroup @bind-Value="_view" Size="MokaSize.Sm">
    <MokaToggleGroupItem Value="list" Icon="MokaIcons.Navigation.Menu" />
    <MokaToggleGroupItem Value="gallery" Icon="MokaIcons.Content.Image" />
</MokaToggleGroup>

@code {
    private string? _view = "list";
}
```
