---
title: Marquee
description: Scrolling text or content ticker with configurable direction and speed.
order: 45
---

# Marquee

`MokaMarquee` continuously scrolls its child content in a chosen direction. The animation pauses on hover by default, making it ideal for news tickers, announcements, or decorative scrolling content.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | *(required)* | The content to scroll |
| `Speed` | `int` | `30` | Scroll speed in pixels per second |
| `Direction` | `MokaMarqueeDirection` | `Left` | Scroll direction: `Left`, `Right`, `Up`, `Down` |
| `PauseOnHover` | `bool` | `true` | Pauses the animation when hovered |
| `Gap` | `string?` | -- | Gap between repeated content (CSS value, e.g. `2rem`) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Left Scroll

```blazor-preview
<MokaMarquee>
    <MokaText>Breaking news: Moka.Red v0.2 is now available with 5 new components!</MokaText>
</MokaMarquee>
```

## Right Direction

```blazor-preview
<MokaMarquee Direction="MokaMarqueeDirection.Right" Speed="50">
    <MokaText>Scrolling to the right at 50px/s</MokaText>
</MokaMarquee>
```

## Vertical Scroll

```blazor-preview
<div style="height:60px;overflow:hidden">
    <MokaMarquee Direction="MokaMarqueeDirection.Up" Speed="20">
        <MokaText>Line 1: Server status — all systems operational</MokaText>
        <MokaText>Line 2: Deployment completed successfully</MokaText>
        <MokaText>Line 3: Next maintenance window: Sunday 2 AM</MokaText>
    </MokaMarquee>
</div>
```

## Pause on Hover Disabled

```blazor-preview
<MokaMarquee PauseOnHover="false" Speed="40">
    <MokaText>This marquee does not pause when you hover over it.</MokaText>
</MokaMarquee>
```

## Mixed Content

```blazor-preview
<MokaMarquee Gap="3rem">
    <MokaFlexbox Align="MokaAlign.Center" Gap="MokaSpacingScale.Md" Style="display:inline-flex">
        <MokaIcon Icon="MokaIcons.Status.Info" Size="MokaSize.Sm" />
        <MokaText>Welcome to Moka.Red</MokaText>
        <MokaDivider Vertical="true" Style="height:1rem" />
        <MokaIcon Icon="MokaIcons.Status.Success" Size="MokaSize.Sm" />
        <MokaText>Build passed</MokaText>
        <MokaDivider Vertical="true" Style="height:1rem" />
        <MokaIcon Icon="MokaIcons.Action.Star" Size="MokaSize.Sm" />
        <MokaText>Star us on GitHub</MokaText>
    </MokaFlexbox>
</MokaMarquee>
```
