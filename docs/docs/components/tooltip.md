---
title: Tooltip
description: Pure CSS tooltip with zero JavaScript -- shows on hover or focus.
order: 21
---

# Tooltip

`MokaTooltip` is a lightweight tooltip that appears on hover or focus. It is implemented entirely in CSS with zero JavaScript interop, zero C# event handlers, and zero allocations per hover. The tooltip element is always in the DOM but invisible; CSS handles show/hide transitions.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The trigger element the tooltip wraps |
| `Text` | `string?` | -- | Plain text tooltip content (ignored if `Content` is set) |
| `Content` | `RenderFragment?` | -- | Rich tooltip content (overrides `Text`) |
| `Position` | `MokaTooltipPosition` | `Top` | `Top`, `Bottom`, `Left`, `Right` |
| `Delay` | `int` | `300` | Delay in milliseconds before showing |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Usage

```blazor-preview
<MokaTooltip Text="Save your changes">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
</MokaTooltip>
```

## Positions

```blazor-preview
<div style="display:flex;gap:16px;padding:40px;justify-content:center">
    <MokaTooltip Text="Top tooltip" Position="MokaTooltipPosition.Top">
        <MokaButton Variant="MokaVariant.Outlined">Top</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Bottom tooltip" Position="MokaTooltipPosition.Bottom">
        <MokaButton Variant="MokaVariant.Outlined">Bottom</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Left tooltip" Position="MokaTooltipPosition.Left">
        <MokaButton Variant="MokaVariant.Outlined">Left</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Right tooltip" Position="MokaTooltipPosition.Right">
        <MokaButton Variant="MokaVariant.Outlined">Right</MokaButton>
    </MokaTooltip>
</div>
```

## Custom Delay

```blazor-preview
<MokaTooltip Text="Appears after 1 second" Delay="1000">
    <MokaButton Variant="MokaVariant.Soft">Slow tooltip</MokaButton>
</MokaTooltip>
```

## Rich Content

Use the `Content` parameter for tooltips with formatted markup.

```blazor-preview
<MokaTooltip>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined">Hover me</MokaButton>
    </ChildContent>
    <Content>
        <div style="text-align:center">
            <strong>Rich Tooltip</strong>
            <p style="margin:4px 0 0">Supports any Blazor content.</p>
        </div>
    </Content>
</MokaTooltip>
```
