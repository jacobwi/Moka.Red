---
title: Scale In
description: Scale content from a smaller size to full scale on render.
order: 67
---

# Scale In

`MokaScaleIn` animates content by scaling it from a smaller initial size to its natural dimensions. This creates a "pop in" effect that works well for cards, images, and interactive elements.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Content to animate |
| `Duration` | `int` | `400` | Animation duration in milliseconds |
| `Delay` | `int` | `0` | Delay before the animation starts, in milliseconds |
| `InitialScale` | `double` | `0.8` | Starting scale factor (0.0 to 1.0) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Scale In

Content scales smoothly from 80% to 100%.

```blazor-preview
<MokaScaleIn>
    <MokaCard Title="Welcome" Outlined>
        <ChildContent>
            <MokaText>This card pops in from 80% scale.</MokaText>
        </ChildContent>
    </MokaCard>
</MokaScaleIn>
```

## Small Initial Scale

A smaller starting scale creates a more dramatic entrance.

```blazor-preview
<MokaScaleIn InitialScale="0.3">
    <MokaFlexbox Justify="MokaJustify.Center" Padding="MokaSpacingScale.Md">
        <MokaButton Color="MokaColor.Primary" Variant="MokaVariant.Filled">Pop!</MokaButton>
    </MokaFlexbox>
</MokaScaleIn>
```

## With Delay

Delay the scale animation to sequence it after other entrance effects.

```blazor-preview
<MokaScaleIn InitialScale="0.5" Delay="300" Duration="500">
    <MokaCallout Type="MokaCalloutType.Warning" Title="Attention">
        This callout scales in after a 300ms delay.
    </MokaCallout>
</MokaScaleIn>
```
