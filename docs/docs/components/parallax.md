---
title: Parallax
description: Parallax scroll effect with background content.
order: 74
---

# Parallax

`MokaParallax` creates a parallax scrolling effect where background content moves at a different speed than the foreground. Use it for hero sections, feature highlights, or decorative page breaks.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | -- | Foreground content rendered on top |
| `BackgroundContent` | `RenderFragment?` | -- | Custom background markup (takes precedence over `BackgroundImage`) |
| `BackgroundImage` | `string?` | -- | URL of the background image |
| `Speed` | `double` | `0.5` | Parallax speed factor (0 = fixed, 1 = normal scroll) |
| `Height` | `string` | `"400px"` | Container height |
| `Overlay` | `bool` | `false` | Adds a dark overlay on top of the background |
| `OverlayOpacity` | `double` | `0.4` | Overlay opacity (0.0 -- 1.0) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic with Background Image

```blazor-preview
<MokaParallax BackgroundImage="https://picsum.photos/1200/600" Height="300px">
    <MokaHeading Level="2" Style="color: white; text-shadow: 0 2px 4px rgba(0,0,0,0.5);">Welcome</MokaHeading>
</MokaParallax>
```

## With Overlay

```blazor-preview
<MokaParallax BackgroundImage="https://picsum.photos/1200/601" Overlay="true" OverlayOpacity="0.5" Height="300px">
    <MokaFlexbox Direction="MokaDirection.Column" Align="MokaAlign.Center" Justify="MokaJustify.Center" Style="height: 100%;">
        <MokaHeading Level="2" Style="color: white;">Darkened Background</MokaHeading>
        <MokaText Style="color: rgba(255,255,255,0.8);">The overlay makes text easier to read.</MokaText>
    </MokaFlexbox>
</MokaParallax>
```

## Custom Height with Gradient Background

```blazor-preview
<MokaParallax Height="250px">
    <BackgroundContent>
        <div style="width: 100%; height: 100%; background: linear-gradient(135deg, var(--moka-color-primary), var(--moka-color-secondary));"></div>
    </BackgroundContent>
    <ChildContent>
        <MokaFlexbox Align="MokaAlign.Center" Justify="MokaJustify.Center" Style="height: 100%;">
            <MokaHeading Level="3" Style="color: white;">Gradient Parallax</MokaHeading>
        </MokaFlexbox>
    </ChildContent>
</MokaParallax>
```
