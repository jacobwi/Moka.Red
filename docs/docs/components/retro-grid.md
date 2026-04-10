---
title: Retro Grid
description: Animated perspective grid background with a glowing horizon line for retro-futuristic aesthetics.
order: 82
---

# Retro Grid

`MokaRetroGrid` renders a CSS perspective-projected grid that scrolls toward a glowing horizon line, evoking an 80s retro-futuristic aesthetic. Place content over it for hero sections or decorative backdrops.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Content rendered on top of the grid |
| `LineColor` | `string?` | — | Grid line color |
| `CellSize` | `int` | `60` | Cell size in pixels |
| `LineWidth` | `int` | `1` | Line thickness |
| `Perspective` | `int` | `300` | CSS perspective depth in pixels |
| `Angle` | `int` | `60` | Rotation angle in degrees |
| `HorizonPosition` | `int` | `65` | Horizon vertical position as a percentage |
| `ShowHorizonGlow` | `bool` | `true` | Show a glowing horizon line |
| `HorizonGlowColor` | `string?` | — | Horizon glow color |
| `Animated` | `bool` | `true` | Enable scrolling animation |
| `Duration` | `double` | `8` | Animation cycle duration in seconds |
| `BackgroundColor` | `string?` | — | Container background color |
| `MinHeight` | `string` | `"400px"` | Minimum height of the container |
| `FullScreen` | `bool` | `false` | Fill the entire viewport height |

## Default Retro Grid

```blazor-preview
<MokaRetroGrid MinHeight="300px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaHeading Level="2">Welcome to the Future</MokaHeading>
    </div>
</MokaRetroGrid>
```

## Custom Colors

```blazor-preview
<MokaRetroGrid LineColor="#d32f2f" HorizonGlowColor="#ff5252" BackgroundColor="#1a1a2e" MinHeight="300px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%;color:white">
        <MokaHeading Level="3">Red Grid</MokaHeading>
    </div>
</MokaRetroGrid>
```

## Static Grid

Disable animation for a static background.

```blazor-preview
<MokaRetroGrid Animated="false" CellSize="40" MinHeight="250px">
    <div style="display:flex;align-items:center;justify-content:center;height:100%">
        <MokaText>No animation</MokaText>
    </div>
</MokaRetroGrid>
```
