---
title: Ribbon
description: Diagonal corner ribbon over a card or panel, for labels such as NEW, SALE or BETA.
order: 67
---

# Ribbon

`MokaRibbon` wraps content and lays a diagonal band with a short label across one of its corners. Use it to flag a card as new, on sale or in beta. `MokaBadge` pins a count or a dot to a corner instead, and `MokaTag` is an inline label.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | **required** | Label on the band, shown in uppercase |
| `Position` | `MokaRibbonPosition` | `TopRight` | Corner: `TopRight`, `TopLeft`, `BottomRight`, `BottomLeft` |
| `Color` | `MokaColor?` | `Primary` | Band color. The text uses the matching on-color. `Surface` uses the surface variant |
| `ChildContent` | `RenderFragment?` | -- | Content the ribbon sits on |
| `Rounded` | `MokaRounding?` | -- | Corner radius of the wrapper. The band ends are clipped along it |
| `RoundedValue` | `string?` | -- | Any CSS border radius for the wrapper. Overrides `Rounded` |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the wrapper |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the wrapper, between its edge and the content |
| `Class` | `string?` | -- | Additional CSS classes for the wrapper |
| `Style` | `string?` | -- | Additional inline styles for the wrapper |

## On a Card

The wrapper gets the card's corner radius through `Rounded`, so the band ends are clipped along the same curve.

```blazor-preview
<MokaRibbon Text="New" Rounded="MokaRounding.Lg" Style="width:280px">
    <MokaCard Outlined Title="Pro plan" Subtitle="For growing teams">
        Unlimited projects, priority support and audit logs.
    </MokaCard>
</MokaRibbon>
```

## Positions

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    @foreach (var position in new[] { MokaRibbonPosition.TopLeft, MokaRibbonPosition.TopRight, MokaRibbonPosition.BottomLeft, MokaRibbonPosition.BottomRight })
    {
        <MokaRibbon Text="Beta" Position="position" Style="width:160px;border-radius:var(--moka-radius-md)">
            <div style="height:110px;display:flex;align-items:center;justify-content:center;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);font-size:var(--moka-font-size-sm)">
                @position
            </div>
        </MokaRibbon>
    }
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:12px;flex-wrap:wrap">
    @foreach (var item in _ribbons)
    {
        <MokaRibbon Text="@item.Label" Color="item.Color" Style="width:160px;border-radius:var(--moka-radius-md)">
            <div style="height:110px;display:flex;align-items:center;justify-content:center;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);font-size:var(--moka-font-size-sm)">
                @item.Color
            </div>
        </MokaRibbon>
    }
</div>

@code {
    private readonly (string Label, MokaColor Color)[] _ribbons =
    [
        ("New", MokaColor.Primary),
        ("Sale", MokaColor.Error),
        ("Hot", MokaColor.Warning),
        ("Live", MokaColor.Success),
        ("Beta", MokaColor.Info)
    ];
}
```

## Behaviour

- The wrapper is `position: relative` with `overflow: hidden`, and the band is a 150px strip turned 45 degrees across the corner. The clip also cuts off anything in the content that reaches outside the wrapper, such as shadows, tooltips and dropdown menus, so prefer content without an outer shadow, like an outlined card.
- Only a short word fits on the visible part of the band.
- `Class`, `Style`, `Rounded`, `Margin` and `Padding` apply to the wrapper, not the band.
- The label comes after the content in the DOM, so screen readers read it last.
