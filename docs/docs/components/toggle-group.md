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
| `FullWidth` | `bool` | `false` | The group fills its container's width and the items share it |
| `StackBelow` | `string?` | `null` | Below this width of the group itself, such as `"30rem"`, the items stack one per row. Any CSS length or a `calc()`, `var()`, `min()`, `max()` or `clamp()` of lengths. Only with `FullWidth`. See [Rows that wrap](#rows-that-wrap) |
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

## Rows that wrap

Items that don't fit on one row move to the next, each as wide as its label, rather than being cut off where nobody can
reach them. The separators sit in the gaps between the items, so a group that fits on one row looks and lays out as it
always has; when it wraps, a line parts the rows and the group's rounded edge clips the separators at its sides.

`FullWidth` makes the group fill its container, the items sharing the width. With it, `StackBelow` gives a width below
which every item takes a row of its own, so a phone shows a list of whole labels. It compares the group's own width, not
the window's, so a group in a narrow panel stacks too. It works as `MokaSegmentedControl.StackBelow` does.

```blazor-preview
<MokaToggleGroup @bind-Value="_range" FullWidth="true" StackBelow="26rem">
    <MokaToggleGroupItem Value="hour" Text="Last hour" />
    <MokaToggleGroupItem Value="day" Text="Last day" />
    <MokaToggleGroupItem Value="week" Text="Last week" />
    <MokaToggleGroupItem Value="month" Text="Last month" />
</MokaToggleGroup>

@code {
    private string? _range = "day";
}
```

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
