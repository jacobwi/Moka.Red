---
title: Footer
description: Page or panel footer that flows with the content, sticks to the bottom, or stays fixed.
order: 111
---

# Footer

`MokaFooter` renders a `<footer>` element on the surface color, with small text and a top border. `Position` keeps it in the normal flow, sticks it to the bottom of its scroll container, or fixes it to the bottom of the viewport.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Footer content |
| `Position` | `MokaFooterPosition` | `Static` | `Static` flows with the content, `Sticky` sticks to the bottom of the scroll container, `Fixed` pins it to the bottom of the viewport |
| `Bordered` | `bool` | `true` | Shows a top border |
| `Elevated` | `bool` | `false` | Adds the `--moka-shadow-1` shadow |
| `Padding` | `MokaSpacingScale?` | -- | Padding from the spacing scale. With neither `Padding` nor `PaddingValue`, the footer uses `var(--moka-spacing-sm) var(--moka-spacing-lg)` |
| `PaddingValue` | `string?` | -- | Any CSS padding. Wins over `Padding` |
| `Margin` | `MokaSpacingScale?` | -- | Margin from the spacing scale |
| `MarginValue` | `string?` | -- | Any CSS margin. Wins over `Margin` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<div style="width:100%">
    <MokaFooter>
        <div style="display:flex;align-items:center;gap:var(--moka-spacing-md);flex-wrap:wrap">
            <span>© 2026 Acme Inc.</span>
            <MokaSpacer />
            <a href="#">Privacy</a>
            <a href="#">Terms</a>
            <a href="#">Status</a>
        </div>
    </MokaFooter>
</div>
```

## Sticky

A sticky footer stays at the bottom of the scroll container while the content above it scrolls.

```blazor-preview
<div style="width:100%;height:200px;overflow-y:auto;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <div style="padding:var(--moka-spacing-sm) var(--moka-spacing-md)">
        @for (var i = 1; i <= 12; i++)
        {
            <p style="margin:0 0 var(--moka-spacing-sm)">Build #@(1040 + i) passed</p>
        }
    </div>
    <MokaFooter Position="MokaFooterPosition.Sticky">
        <div style="display:flex;align-items:center;gap:var(--moka-spacing-sm)">
            <span>12 builds</span>
            <MokaSpacer />
            <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Outlined">Load more</MokaButton>
        </div>
    </MokaFooter>
</div>
```

## Elevated

Drop the border and add the shadow, with padding from the spacing scale.

```blazor-preview
<div style="width:100%">
    <MokaFooter Elevated Bordered="false" Padding="MokaSpacingScale.Md">
        <div style="display:flex;align-items:center;gap:var(--moka-spacing-sm)">
            <span>3 files changed</span>
            <MokaSpacer />
            <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Text">Discard</MokaButton>
            <MokaButton Size="MokaSize.Sm">Commit</MokaButton>
        </div>
    </MokaFooter>
</div>
```

## Fixed

A fixed footer spans the full width of the viewport and covers whatever sits at the bottom of the page, so give the page a bottom padding at least as tall as the footer.

```razor
<main style="padding-bottom: 3rem">
    @Body
</main>

<MokaFooter Position="MokaFooterPosition.Fixed">
    Connected to prod-eu-1
</MokaFooter>
```

## Behaviour

- `Sticky` uses `position: sticky` with `bottom: 0` and the `--moka-z-sticky` layer. It sticks inside the nearest scrolling ancestor and settles at the end of the content.
- `Fixed` uses `position: fixed` with `left: 0`, `right: 0` and `bottom: 0` on the `--moka-z-fixed` layer.

## Accessibility

Browsers expose a `<footer>` outside `article`, `aside`, `main`, `nav` and `section` elements as the page's `contentinfo` landmark, and a page should have only one. Inside a `<section>` or `<article>`, a footer for a card or panel is not a landmark.
