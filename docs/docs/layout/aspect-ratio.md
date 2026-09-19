---
title: Aspect Ratio
description: Box that keeps a fixed width-to-height ratio at any width.
order: 110
---

# Aspect Ratio

`MokaAspectRatio` sizes a box by a ratio instead of a fixed height. It takes the width its container gives it and sets its height from `Ratio` through the CSS `aspect-ratio` property. Use it for images, video frames, maps and chart placeholders that should keep their shape as the layout resizes.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content to size |
| `Ratio` | `string` | `"16/9"` | Any CSS `aspect-ratio` value, such as `"4/3"`, `"1"` or `"21 / 9"` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The box clips whatever does not fit (`overflow: hidden`), so tall content cannot stretch it past its ratio. It does not size its children: give an image or iframe `width: 100%` and `height: 100%` to fill it.

## Basic

The default ratio is 16:9. The width comes from the parent, here capped at 480px.

```blazor-preview
<div style="width:100%;max-width:480px">
    <MokaAspectRatio Style="background:var(--moka-color-surface-variant);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        <div style="height:100%;display:flex;align-items:center;justify-content:center;font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-sm);color:var(--moka-color-on-surface-variant)">
            16 / 9
        </div>
    </MokaAspectRatio>
</div>
```

## Common Ratios

Every box below gets the same width from the grid and a height from its own ratio.

```blazor-preview
<div style="width:100%;display:grid;grid-template-columns:repeat(4, 1fr);gap:var(--moka-spacing-md);align-items:start">
    @foreach (var ratio in new[] { "1", "4/3", "16/9", "21/9" })
    {
        <MokaAspectRatio Ratio="@ratio" Style="background:var(--moka-color-surface-variant);border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-sm)">
            <div style="height:100%;display:flex;align-items:center;justify-content:center;font-family:var(--moka-font-family-mono);font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">
                @ratio
            </div>
        </MokaAspectRatio>
    }
</div>
```

## Image

`object-fit: cover` fills the box and crops the image instead of distorting it.

```blazor-preview
<div style="width:100%;max-width:400px">
    <MokaAspectRatio Ratio="4/3" Style="border-radius:var(--moka-radius-md)">
        <img src="https://picsum.photos/800/600" alt="Sample landscape photo"
             style="display:block;width:100%;height:100%;object-fit:cover" />
    </MokaAspectRatio>
</div>
```
