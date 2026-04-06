---
title: Reveal
description: Scroll-triggered entrance animations.
order: 75
---

# Reveal

`MokaReveal` triggers an entrance animation when the element scrolls into view. Wrap any content to add fade, slide, or scale effects that fire once (or every time) the element enters the viewport.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Content to reveal |
| `Animation` | `MokaRevealAnimation` | `FadeUp` | Animation type: `FadeIn`, `FadeUp`, `FadeDown`, `FadeLeft`, `FadeRight`, `ScaleIn`, `SlideUp` |
| `Duration` | `int` | `600` | Animation duration in milliseconds |
| `Delay` | `int` | `0` | Delay before animation starts (ms) |
| `Threshold` | `double` | `0.1` | Intersection threshold (0.0 -- 1.0) before triggering |
| `Once` | `bool` | `true` | Animate only the first time the element enters the viewport |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Fade Up on Scroll

```blazor-preview
<MokaReveal Animation="MokaRevealAnimation.FadeUp">
    <MokaCard Title="Hello" Outlined>
        <ChildContent>
            <MokaText>This card fades up into view when you scroll to it.</MokaText>
        </ChildContent>
    </MokaCard>
</MokaReveal>
```

## Scale In

```blazor-preview
<MokaReveal Animation="MokaRevealAnimation.ScaleIn" Duration="800">
    <MokaCallout Type="MokaCalloutType.Info" Title="Scaled Entrance">
        This callout scales in from a smaller size.
    </MokaCallout>
</MokaReveal>
```

## With Delay

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Md">
    <MokaReveal Animation="MokaRevealAnimation.FadeUp" Delay="0">
        <MokaCard Title="First" Outlined><ChildContent><MokaText>No delay</MokaText></ChildContent></MokaCard>
    </MokaReveal>
    <MokaReveal Animation="MokaRevealAnimation.FadeUp" Delay="200">
        <MokaCard Title="Second" Outlined><ChildContent><MokaText>200ms delay</MokaText></ChildContent></MokaCard>
    </MokaReveal>
    <MokaReveal Animation="MokaRevealAnimation.FadeUp" Delay="400">
        <MokaCard Title="Third" Outlined><ChildContent><MokaText>400ms delay</MokaText></ChildContent></MokaCard>
    </MokaReveal>
</MokaFlexbox>
```
