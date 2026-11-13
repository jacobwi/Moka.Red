---
title: Reveal
description: Scroll-triggered entrance animations.
order: 75
---

# Reveal

`MokaReveal` triggers an entrance animation when the element scrolls into view. Wrap any content to add fade, slide, or scale effects that fire once (or every time) the element enters the viewport. It also tells the app when its content comes into view and leaves it, so a thumbnail can load only once it can be seen.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Content to reveal |
| `Animation` | `MokaRevealAnimation` | `FadeUp` | Animation type: `FadeIn`, `FadeUp`, `FadeDown`, `FadeLeft`, `FadeRight`, `ScaleIn`, `SlideUp`, or `None` to only watch |
| `Duration` | `int` | `600` | Animation duration in milliseconds |
| `Delay` | `int` | `0` | Delay before animation starts (ms) |
| `Threshold` | `double` | `0.1` | Intersection threshold (0.0 -- 1.0) before triggering |
| `Once` | `bool` | `true` | Watch only until the element first enters the viewport: it animates once, `OnEnter` is raised once and `OnLeave` never |
| `OnEnter` | `EventCallback` | -- | Raised when the content comes into view, on the first render too when it is already there |
| `OnLeave` | `EventCallback` | -- | Raised when the content leaves the view again. Needs `Once="false"` |
| `RootMargin` | `string?` | -- | Grows or shrinks the view the content is measured against, as a CSS margin in px or %, such as `"200px"`. A value the browser wouldn't take is ignored |
| `Debounce` | `int` | `0` | Milliseconds the content has to stay in view or out of it before the change counts, so content a fast scroll carries past raises nothing |
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
    <MokaCallout Type="MokaCalloutType.Note" Title="Scaled Entrance">
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

## After the reveal

A revealed element keeps no transform and no `will-change`, so a dialog, drawer, command palette or tooltip opened inside it is placed against the window rather than the element, and nothing traps it under the content around it. Up to now `will-change` stayed on for good, which did both. The transition looks the same.

## Knowing when content is in view

`OnEnter` and `OnLeave` tell the app when the content crosses into the view and out of it. With `Animation="MokaRevealAnimation.None"` the reveal draws the content as it is and only watches, which is what a lazy thumbnail needs: ask for the picture when its tile comes into view, and let it go when the tile leaves.

```razor
@foreach (Photo photo in _photos)
{
    <MokaReveal Animation="MokaRevealAnimation.None" Once="false" RootMargin="200px" Debounce="120"
                OnEnter="() => _thumbnails.Want(photo)" OnLeave="() => _thumbnails.Drop(photo)">
        <img src="@_thumbnails.UrlOf(photo)" alt="@photo.Name" width="160" height="120" />
    </MokaReveal>
}
```

The browser does the watching with an `IntersectionObserver`, one for every reveal on the page that shares a threshold and a margin, and .NET hears only the moment content crosses, never the scrolling in between. A leave costs a call only when `OnLeave` listens or an animation plays again. `RootMargin` starts the load a little before the tile scrolls in, and `Debounce` lets a fast scroll past a long list ask for nothing.

Each reveal binds itself with one call when it first renders, so for thousands of tiles drawn at once, draw them in steps or under `Virtualize`, which renders only what is near the view. With `Once` left on, `OnEnter` comes once and the reveal stops watching, which suits a picture that stays once loaded. A reveal with no animation and no listener loads no script at all.

