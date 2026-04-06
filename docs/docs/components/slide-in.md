---
title: Slide In
description: Slide content in from any edge with configurable distance and timing.
order: 66
---

# Slide In

`MokaSlideIn` animates content by sliding it in from a specified edge. Unlike `MokaFadeIn`, this component does not fade opacity -- the element is fully opaque and translates into its final position.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Content to animate |
| `Duration` | `int` | `400` | Animation duration in milliseconds |
| `Delay` | `int` | `0` | Delay before the animation starts, in milliseconds |
| `From` | `MokaSlideFrom` | `Left` | Edge to slide from: `Left`, `Right`, `Top`, `Bottom` |
| `Distance` | `string` | `"2rem"` | Slide distance (any CSS length) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Slide from Left

```blazor-preview
<MokaSlideIn From="MokaSlideFrom.Left">
    <MokaCallout Type="MokaCalloutType.Info" Title="Navigation">
        This panel slides in from the left edge.
    </MokaCallout>
</MokaSlideIn>
```

## Slide from Bottom

```blazor-preview
<MokaSlideIn From="MokaSlideFrom.Bottom">
    <MokaCard Title="Notification" Outlined>
        <ChildContent>
            <MokaText>This card slides up from the bottom.</MokaText>
        </ChildContent>
    </MokaCard>
</MokaSlideIn>
```

## Custom Distance

Use a larger distance for more dramatic entrances.

```blazor-preview
<MokaSlideIn From="MokaSlideFrom.Right" Distance="6rem" Duration="600">
    <MokaCallout Type="MokaCalloutType.Success" Title="Done!">
        Slides in from 6rem to the right over 600ms.
    </MokaCallout>
</MokaSlideIn>
```
