---
title: Floating Action Button
description: Fixed-position action button for primary or promoted actions.
order: 43
---

# Floating Action Button

`MokaFloatingActionButton` renders a prominent circular button typically fixed to a corner of the viewport. It supports icon-only and extended (icon + label) variants, configurable positioning, and all standard color and size options.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition` | *(required)* | The icon displayed in the button |
| `OnClick` | `EventCallback` | -- | Callback when the button is clicked |
| `Color` | `MokaColor` | `Primary` | Button color theme |
| `Size` | `MokaSize` | `Md` | Button size: `Sm`, `Md`, `Lg` |
| `Position` | `MokaFabPosition` | `BottomRight` | Corner position: `BottomRight`, `BottomLeft`, `TopRight`, `TopLeft` |
| `Extended` | `bool` | `false` | Shows the label alongside the icon |
| `Label` | `string?` | -- | Text label shown when `Extended` is `true` |
| `Disabled` | `bool` | `false` | Disables the button |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic FAB (Bottom Right)

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" OnClick="() => { }" />
</div>
```

## Top Left Position

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Edit" Position="MokaFabPosition.TopLeft" Color="MokaColor.Secondary" />
</div>
```

## Extended with Label

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add"
                               Extended="true"
                               Label="New Item"
                               OnClick="() => { }" />
</div>
```

## Colors

```blazor-preview
<div style="position:relative;height:250px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" Position="MokaFabPosition.TopLeft" Color="MokaColor.Primary" />
    <MokaFloatingActionButton Icon="MokaIcons.Action.Edit" Position="MokaFabPosition.TopRight" Color="MokaColor.Secondary" />
    <MokaFloatingActionButton Icon="MokaIcons.Status.CheckCircle" Position="MokaFabPosition.BottomLeft" Color="MokaColor.Success" />
    <MokaFloatingActionButton Icon="MokaIcons.Status.Error" Position="MokaFabPosition.BottomRight" Color="MokaColor.Error" />
</div>
```

## Disabled

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" Disabled="true" />
</div>
```
