---
title: Tooltip
description: Pure CSS tooltip with zero JavaScript -- shows on hover or keyboard focus.
order: 21
---

# Tooltip

`MokaTooltip` is a lightweight tooltip that appears on hover or keyboard focus. It is implemented entirely in CSS with zero JavaScript interop, zero C# event handlers, and zero allocations per hover. The tooltip element is always in the DOM but takes no layout until it shows; CSS handles show/hide transitions.

A button that keeps focus after a mouse click does not keep its tooltip: only focus the keyboard put there (`:focus-visible`) shows it, so the tooltip goes when the mouse leaves and does not come back when a menu or dialog hands focus back to its button. `Delay` applies to showing only; hiding never waits.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The trigger element the tooltip wraps |
| `Text` | `string?` | -- | Plain text tooltip content (ignored if `Content` is set) |
| `Content` | `RenderFragment?` | -- | Rich tooltip content (overrides `Text`) |
| `Position` | `MokaTooltipPosition` | `Top` | `Top`, `Bottom`, `Left`, `Right`, and the edge placements `TopStart`, `TopEnd`, `BottomStart`, `BottomEnd`. See [Triggers near the window edge](#triggers-near-the-window-edge) |
| `Delay` | `int` | `300` | Delay in milliseconds before showing. Hiding never waits |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Look

The tooltip is a raised surface with the theme's outline and popup shadow, the same family as the context menu: in the
dark theme a near-black box with a red-tinted ring and glow, in the light theme a light grey one. Three CSS variables
retune it without overriding the component's scoped styles:

| Variable | Default |
|----------|---------|
| `--moka-tooltip-bg` | `var(--moka-color-surface-3)` |
| `--moka-tooltip-color` | `var(--moka-color-on-surface)` |
| `--moka-tooltip-border` | `var(--moka-color-outline)` |

```css
:root {
    --moka-tooltip-border: var(--moka-color-primary-border);
}
```

## Placement

Where the browser supports CSS anchor positioning, the tooltip is placed against the window next to its
trigger rather than inside it. A panel that clips its overflow (a toolbar, a scrolling list, a dialog
body) cannot cut it off, and when the asked side has no room the browser tries the others in order:
a `Top` tooltip on a trigger at the top of the window shows below it, a `Right` one at the right edge
shows on the left, and a centred one that would reach past a side edge is shifted along instead. Every
tooltip names its own anchor, so any number of them can share a page.

## Triggers near the window edge

Without anchor positioning the tooltip sits inside its trigger and does not flip. `Top` and `Bottom` centre the tooltip under the trigger. On the right-most button of a toolbar that
reaches past the window, which makes the page scrollable sideways while the tooltip is showing. The
`-start` and `-end` placements line the tooltip up with one edge of the trigger instead:

```razor
<MokaTooltip Text="Settings" Position="MokaTooltipPosition.BottomEnd">
    <MokaButton StartIcon="MokaIcons.Action.Settings" />
</MokaTooltip>
```

Use `BottomEnd` or `TopEnd` for a trigger at the right edge and `BottomStart` or `TopStart` at the
left. A hidden tooltip takes no layout at all, so one that is not showing can never move the page.

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
