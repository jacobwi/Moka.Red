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
| `ChildContent` | `RenderFragment?` | — | Content rendered on top of the grid |
| `Pattern` | `MokaGridPattern` | `Lines` | Grid pattern style |
| `CellSize` | `int` | `40` | Cell size in pixels |
| `StrokeWidth` | `double` | `1` | Line thickness |
| `DotRadius` | `double` | `1` | Dot size for the `Dots` pattern |
| `CrossArm` | `int` | `3` | Cross arm length for the `Cross` pattern |
| `DashArray` | `string` | `"4 4"` | Dash pattern for the `Dashed` pattern |
| `DiagonalAngle` | `int` | `45` | Angle for `DiagonalLines` pattern |
| `PatternColor` | `string?` | — | Grid line color override |
| `PatternOpacity` | `double` | `0.7` | Pattern opacity (0 to 1) |
| `FadeEdges` | `bool` | `true` | Apply a radial fade mask at the edges |
| `FadeStart` | `int` | `30` | Fade start percentage |
| `FadeEnd` | `int` | `80` | Fade end percentage |
| `FadeMask` | `string?` | — | Custom CSS `mask-image` value |
| `Highlighted` | `bool` | `false` | Show a center glow highlight |
| `HighlightColor` | `string?` | — | Glow color override |
| `HighlightRadius` | `int` | `60` | Glow extent percentage |
| `BackgroundColor` | `string?` | — | Container background color |
| `MinHeight` | `string?` | — | Minimum height of the container |
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
