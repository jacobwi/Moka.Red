---
title: Image Compare
description: Two images over each other with a draggable divider between them, before and after.
order: 60
---

# Image Compare

`MokaImageCompare` stacks two images and puts a draggable divider between them: before on one
side, after on the other - for screenshots that changed, photos that were edited, renders that
were re-lit. The divider is also a slider the keyboard can move.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `BeforeSrc` | `string?` | -- | The image the divider reveals less of as it moves: the left (or top) one |
| `AfterSrc` | `string?` | -- | The image the divider reveals more of as it moves: the right (or bottom) one |
| `BeforeLabel` | `string?` | `"Before"` | Name of the corner label on the before image. An empty string takes the badge off |
| `AfterLabel` | `string?` | `"After"` | Name of the corner label on the after image. An empty string takes the badge off |
| `Alt` | `string?` | -- | Text the images stand for, read by assistive technology |
| `Position` | `double` | `50` | Where the divider sits, 0 to 100 percent. Two-way bindable |
| `PositionChanged` | `EventCallback<double>` | -- | Notified when the divider moves. A drag reports once, when it ends |
| `Orientation` | `MokaImageCompareOrientation` | `Horizontal` | Which way the divider runs. `Vertical` puts before above, after below |
| `Size` | `MokaSize` | `Md` | The height of the comparison |
| `Label` | `string?` | -- | Unused by the comparison itself; pass `Alt` for assistive technology |

## Basic

```blazor-preview
@code {
    double _position = 50;
}

<MokaImageCompare @bind-Position="_position"
                  BeforeSrc="https://picsum.photos/seed/moka-before/640/360"
                  AfterSrc="https://picsum.photos/seed/moka-after/640/360" />
```

## Top and bottom

```blazor-preview
<MokaImageCompare Orientation="MokaImageCompareOrientation.Vertical"
                  BeforeSrc="https://picsum.photos/seed/moka-before/640/360"
                  AfterSrc="https://picsum.photos/seed/moka-after/640/360"
                  BeforeLabel="Light" AfterLabel="Dark" />
```

The divider drags with a mouse, a pen or a finger; the drag runs in the browser, so the images do
not re-render per move, and `PositionChanged` hears the result once, when it ends. The handle is
a slider for the keyboard: the arrows along the divider move it a step (ten with Shift), and Home
and End go to the ends.
