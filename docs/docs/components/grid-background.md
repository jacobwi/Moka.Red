---
title: Grid Background
description: Decorative background container with configurable grid patterns, edge fading, and center glow.
order: 81
---

# Grid Background

`MokaGridBackground` renders a decorative SVG grid pattern behind its content. Choose from six pattern styles, control line color and opacity, apply a radial edge fade, and optionally highlight the center with a soft glow. Useful for hero sections, landing pages, and empty-state backgrounds.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Content rendered on top of the grid |
| `Pattern` | `MokaGridPattern` | `Lines` | Grid pattern style |
| `CellSize` | `int` | `40` | Cell size in pixels. Zero or less uses the default |
| `StrokeWidth` | `double` | `1` | Line thickness. NaN or infinity uses the default |
| `DotRadius` | `double` | `1` | Dot size for the `Dots` pattern. NaN or infinity uses the default |
| `CrossArm` | `int` | `3` | Cross arm length for the `Cross` pattern |
| `DashArray` | `string` | `"4 4"` | Dash and gap lengths for the `Dashed` pattern, separated by spaces or commas. Anything else draws the default |
| `DiagonalAngle` | `int` | `45` | Angle for `DiagonalLines` pattern |
| `PatternColor` | `string?` | - | Grid line color override. A value that is not a CSS color draws the default |
| `PatternOpacity` | `double` | `0.7` | Pattern opacity (0 to 1). NaN or infinity uses the default |
| `FadeEdges` | `bool` | `true` | Apply a radial fade mask at the edges |
| `FadeStart` | `int` | `30` | Fade start percentage |
| `FadeEnd` | `int` | `80` | Fade end percentage |
| `FadeMask` | `string?` | - | Custom CSS `mask-image` value |
| `Highlighted` | `bool` | `false` | Show a center glow highlight |
| `HighlightColor` | `string?` | - | Glow color override. A value that is not a CSS color uses the default |
| `HighlightRadius` | `int` | `60` | Glow extent percentage |
| `BackgroundColor` | `string?` | - | Container background color. A value that is not a CSS color is ignored |
| `MinHeight` | `string?` | - | Minimum height of the container |
| `FullScreen` | `bool` | `false` | Fill the entire viewport height |

### MokaGridPattern Enum

| Value | Description |
|-------|-------------|
| `Lines` | Standard grid lines |
| `Dots` | Dot grid |
| `Dashed` | Dashed grid lines |
| `Cross` | Crosshatch marks at intersections |
| `DiagonalLines` | Angled lines |
| `Honeycomb` | Hexagonal pattern |

## Basic Lines with Highlight

```blazor-preview
<MokaGridBackground Pattern="MokaGridPattern.Lines" Highlighted MinHeight="240px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaHeading Level="2">Lines Pattern</MokaHeading>
    </div>
</MokaGridBackground>
```

## Dots Pattern

```blazor-preview
<MokaGridBackground Pattern="MokaGridPattern.Dots" CellSize="24" DotRadius="1.5" PatternOpacity="0.5" MinHeight="200px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>Small dot grid</MokaText>
    </div>
</MokaGridBackground>
```

## Cross Pattern

```blazor-preview
<MokaGridBackground Pattern="MokaGridPattern.Cross" CellSize="32" CrossArm="4" MinHeight="200px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>Cross marks at intersections</MokaText>
    </div>
</MokaGridBackground>
```

## Honeycomb

```blazor-preview
<MokaGridBackground Pattern="MokaGridPattern.Honeycomb" CellSize="30" PatternColor="#d32f2f" PatternOpacity="0.3" MinHeight="200px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>Hexagonal honeycomb pattern</MokaText>
    </div>
</MokaGridBackground>
```

## Behaviour

- `PatternColor`, `HighlightColor` and `BackgroundColor` must be CSS colors: hex, a keyword such as `red`, or a color function such as `rgb()`, `hsl()` or `var()`. `DashArray` must be non-negative numbers with optional units, such as `"6, 3"`. Anything else falls back to the default, so a stray quote, parenthesis or semicolon cannot add markup to the pattern image or declarations to the style.
- `Dashed`, `Cross` and `Honeycomb` are drawn as an SVG image, which cannot read the page's CSS variables, so a `var()` color does not reach them. Give those patterns a literal color such as `#d32f2f` or `rgba(239, 83, 80, 0.25)`. Unset, they use `rgba(239, 83, 80, 0.25)`. `Lines`, `Dots` and `DiagonalLines` are CSS gradients and take tokens, defaulting to `var(--moka-color-primary-border)`.
- The SVG patterns go into the style as a percent-encoded data URI, with every value escaped first.
- Numbers are written with a decimal point and an ASCII minus sign whatever the culture, so a negative `DiagonalAngle` works under a culture such as Swedish, which writes its minus sign as U+2212.
