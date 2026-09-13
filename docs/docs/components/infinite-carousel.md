---
title: Infinite Carousel
description: Continuous-loop carousel with auto-play and directional support.
order: 73
---

# Infinite Carousel

`MokaInfiniteCarousel` renders a seamlessly looping carousel that continuously scrolls through slides. Unlike `MokaCarousel`, slides wrap around infinitely without snapping back to the start.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaInfiniteCarouselSlide` elements |
| `AutoPlay` | `bool` | `true` | Automatically advance slides |
| `Interval` | `int` | `4000` | Auto-play interval in milliseconds |
| `Speed` | `int` | `500` | Transition speed in milliseconds |
| `ShowControls` | `bool` | `true` | Show prev/next arrow buttons |
| `ShowIndicators` | `bool` | `true` | Show dot indicators |
| `PauseOnHover` | `bool` | `true` | Pause auto-play when hovered |
| `Direction` | `MokaCarouselDirection` | `Horizontal` | Slide movement: `Horizontal` or `Vertical` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Auto-Play

```blazor-preview
<MokaInfiniteCarousel Style="height: 200px;">
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-primary); color: var(--moka-color-on-primary);">Slide 1</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-secondary); color: var(--moka-color-on-secondary);">Slide 2</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-success); color: var(--moka-color-on-success);">Slide 3</div>
    </MokaInfiniteCarouselSlide>
</MokaInfiniteCarousel>
```

## Vertical

```blazor-preview
<MokaInfiniteCarousel Direction="MokaCarouselDirection.Vertical" Style="height: 200px;">
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-primary); color: var(--moka-color-on-primary);">Top</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-warning); color: var(--moka-color-on-warning);">Middle</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-error); color: var(--moka-color-on-error);">Bottom</div>
    </MokaInfiniteCarouselSlide>
</MokaInfiniteCarousel>
```

## With Controls

```blazor-preview
<MokaInfiniteCarousel ShowControls="true" ShowIndicators="true" AutoPlay="false" Style="height: 200px;">
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-info); color: var(--moka-color-on-info);">First</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-success); color: var(--moka-color-on-success);">Second</div>
    </MokaInfiniteCarouselSlide>
    <MokaInfiniteCarouselSlide>
        <div style="display: flex; align-items: center; justify-content: center; height: 100%; background: var(--moka-color-secondary); color: var(--moka-color-on-secondary);">Third</div>
    </MokaInfiniteCarouselSlide>
</MokaInfiniteCarousel>
```
