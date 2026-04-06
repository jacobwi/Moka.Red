---
title: Fade In
description: Fade and optional slide animation wrapper for revealing content on render.
order: 65
---

# Fade In

`MokaFadeIn` wraps content in a fade animation that plays when the component first renders. Optionally combine with a directional slide to create entrance effects like "fade up" or "fade from left".

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Content to animate |
| `Duration` | `int` | `400` | Animation duration in milliseconds |
| `Delay` | `int` | `0` | Delay before the animation starts, in milliseconds |
| `Direction` | `MokaFadeDirection` | `None` | Slide direction: `None`, `Up`, `Down`, `Left`, `Right` |
| `Distance` | `string` | `"1rem"` | Slide distance (any CSS length) |
| `Once` | `bool` | `true` | When `true`, the animation plays only on first render |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Fade

Content fades in from fully transparent to fully opaque.

```blazor-preview
<MokaFadeIn>
    <MokaCallout Type="MokaCalloutType.Info" Title="Welcome">
        This content fades in smoothly.
    </MokaCallout>
</MokaFadeIn>
```

## Fade Up

Combine fade with an upward slide for a polished entrance effect.

```blazor-preview
<MokaFadeIn Direction="MokaFadeDirection.Up">
    <MokaCard Title="Feature Highlight" Outlined>
        <ChildContent>
            <MokaText>This card fades in while sliding up from below.</MokaText>
        </ChildContent>
    </MokaCard>
</MokaFadeIn>
```

## Fade with Delay

Stagger multiple elements by giving each a different delay.

```blazor-preview
<MokaFlexbox Direction="MokaDirection.Column" Gap="MokaSpacingScale.Sm">
    <MokaFadeIn Direction="MokaFadeDirection.Up" Delay="0">
        <MokaCallout Type="MokaCalloutType.Success" Title="Step 1">First item appears immediately.</MokaCallout>
    </MokaFadeIn>
    <MokaFadeIn Direction="MokaFadeDirection.Up" Delay="200">
        <MokaCallout Type="MokaCalloutType.Info" Title="Step 2">Second item follows after 200ms.</MokaCallout>
    </MokaFadeIn>
    <MokaFadeIn Direction="MokaFadeDirection.Up" Delay="400">
        <MokaCallout Type="MokaCalloutType.Warning" Title="Step 3">Third item arrives last.</MokaCallout>
    </MokaFadeIn>
</MokaFlexbox>
```
