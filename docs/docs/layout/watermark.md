---
title: Watermark
description: Faint text or image drawn over content, tiled or centered.
order: 116
---

# Watermark

`MokaWatermark` draws a faint text or image over its content, such as "CONFIDENTIAL" across a report or "DRAFT" on a document preview. The mark sits on a layer that ignores the pointer, so the content under it stays clickable and selectable. It needs no JavaScript: text is drawn into an SVG tile that the layer uses as a CSS mask.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Content to mark |
| `Text` | `string?` | -- | Watermark text, such as `"CONFIDENTIAL"` |
| `ImageSrc` | `string?` | -- | Image URL. Wins over `Text` |
| `Opacity` | `double` | `0.08` | Opacity of the mark, from 0 to 1. Values outside that range are clamped |
| `Rotation` | `int` | `-30` | Angle of the text in degrees. Images are not rotated |
| `FontSize` | `string` | `"48px"` | Size of the text: a number with `px`, `pt`, `pc`, `in`, `cm`, `mm`, `q`, `em` or `rem` (`em` and `rem` count 16px). Anything else uses the default |
| `Color` | `string?` | -- | Text color: hex, a keyword, or a color function such as `rgb()` or `var(--moka-color-primary)`. `currentColor` works too. Unset, or not a color, uses `--moka-color-on-surface` |
| `Position` | `MokaWatermarkPosition` | `Tiled` | `Tiled` repeats the mark over the whole area, `Center` draws it once in the middle |
| `Repeat` | `bool` | `true` | Images only: repeats the image. Text follows `Position` |
| `Gap` | `string` | `"100px"` | Space between repeated text marks, in the same units as `FontSize` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

Each text tile is sized from the text, `FontSize`, `Rotation` and `Gap`, so the whole text shows and neighbouring marks sit one gap apart. The default color is the theme's on-surface color, so the mark shows on light and dark surfaces alike.

## Tiled Text

```blazor-preview
<div style="width:100%;max-width:520px">
    <MokaWatermark Text="CONFIDENTIAL" FontSize="28px" Color="#ef5350" Opacity="0.14"
                   Style="height:200px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        <div style="padding:var(--moka-spacing-lg)">
            <MokaHeading Level="4">Q3 revenue summary</MokaHeading>
            <MokaParagraph>Revenue grew 18% over Q2, led by the enterprise tier. Churn held at 2.1%.</MokaParagraph>
            <MokaParagraph>These figures are unaudited. Do not share them outside the finance team.</MokaParagraph>
        </div>
    </MokaWatermark>
</div>
```

## Centered

`Position="MokaWatermarkPosition.Center"` draws the text once, in the middle of the area.

```blazor-preview
<div style="width:100%;max-width:520px">
    <MokaWatermark Text="DRAFT" Position="MokaWatermarkPosition.Center"
                   FontSize="72px" Rotation="-20" Opacity="0.2" Color="#808080"
                   Style="height:200px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
        <div style="padding:var(--moka-spacing-lg)">
            <MokaHeading Level="4">Release plan</MokaHeading>
            <MokaParagraph>Freeze features on the 12th, run the beta for two weeks, then ship.</MokaParagraph>
        </div>
    </MokaWatermark>
</div>
```

## Image

With `ImageSrc`, the image tiles at its own size. For one image scaled to fit the area, set `Position="MokaWatermarkPosition.Center"` and `Repeat="false"`.

```razor
<MokaWatermark ImageSrc="img/logo.svg"
               Position="MokaWatermarkPosition.Center"
               Repeat="false"
               Opacity="0.06">
    <InvoiceView Invoice="_invoice" />
</MokaWatermark>
```

## Behaviour

- The mark is drawn above the content, on an absolutely positioned layer with `pointer-events: none`.
- The text is drawn into an SVG tile that masks the layer, and the layer is filled with `Color`. The color is ordinary CSS, so tokens and `var()` resolve against the page and follow a theme change.
- Nothing can measure text inside an image, so the tile's width comes from an estimate per character. It errs wide: a wrong guess costs a little extra gap, not a clipped word.
- The text uses the theme's font stack. An SVG image cannot load the page's web fonts, so the text is drawn in the first font of the stack installed on the device, or the system sans-serif.
- `Text` is escaped before it goes into the tile, so `R&D` or quotes draw as written. `Color`, `FontSize` and `Gap` are checked, and a value that does not parse falls back to its default. `ImageSrc` is written as a quoted CSS string with quotes and backslashes escaped, so none of these values can add markup or CSS declarations.
- The wrapper is `position: relative` and clips its content (`overflow: hidden`), so a popup rendered inside it can be cut off at its edges.

## Accessibility

The mark is a CSS background. Screen readers do not read it, and it cannot be selected or copied. When the status matters, such as a draft, state it in the content too. A watermark does not protect content: anyone can remove it in the browser's developer tools.
