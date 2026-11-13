---
title: Grid Background
description: Decorative background container with eighteen grid patterns, a choice of fades and glows, slow motion and theme tones.
order: 81
---

# Grid Background

`MokaGridBackground` draws a decorative pattern behind its content: a hero, a sign-in page, an empty state. Pick one of eighteen patterns, where the pattern fades, a glow, a slow motion and which theme colors draw it. Every pattern follows the theme, and the defaults look exactly as they always have.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Content rendered on top of the grid |
| `Pattern` | `MokaGridPattern` | `Lines` | Grid pattern style |
| `CellSize` | `int` | `0` | Cell size in pixels. Zero or less uses the pattern's own size (see the pattern table) |
| `MajorEvery` | `int` | `5` | How many fine cells sit between the stronger lines of `GraphPaper`. Less than 2 uses the default |
| `StrokeWidth` | `double` | `1` | Line thickness. NaN or infinity uses the default |
| `DotRadius` | `double` | `1` | Dot size for the `Dots` pattern. Also scales the dots of `Diamonds` (1.3 times) and the pads of `Circuit` (2.2 times). NaN or infinity uses the default |
| `CrossArm` | `int` | `3` | Cross arm length for the `Cross` pattern |
| `DashArray` | `string` | `"4 4"` | Dash and gap lengths for the `Dashed` pattern, separated by spaces or commas. Anything else draws the default |
| `DiagonalAngle` | `int` | `45` | Angle for `DiagonalLines` pattern |
| `PatternColor` | `string?` | - | Grid line color. Wins over `Tone`. A value that is not a CSS color draws the tone's colors |
| `Tone` | `MokaGridTone` | `Accent` | Which theme colors draw the pattern when `PatternColor` isn't set |
| `PatternOpacity` | `double` | pattern's own | Pattern opacity (0 to 1). Unset, NaN or infinity uses 0.7, or 1 for `Checker` and for any tone but `Accent`, 0.9 for `CornerMarks` and 0.75 for `Grain` |
| `Fade` | `MokaGridFade?` | - | Where the pattern fades. Unset keeps `FadeEdges` |
| `FadeEdges` | `bool` | `true` | Apply the centre fade (`Center`), or none when false. `Fade` wins when set |
| `FadeStart` | `int` | `30` | Where the centre fade starts, in percent |
| `FadeEnd` | `int` | `80` | Where the centre fade ends, in percent |
| `FadeMask` | `string?` | - | Custom CSS `mask-image` value. Wins over every `Fade` |
| `Glow` | `MokaGridGlow?` | - | A soft light over the pattern. Unset keeps `Highlighted` |
| `Highlighted` | `bool` | `false` | Show the centre glow. `Glow` wins when set |
| `HighlightColor` | `string?` | - | Glow color. It replaces the accent in every glow. A value that is not a CSS color uses the default |
| `HighlightRadius` | `int` | `60` | How far the glow reaches. 60 is each glow's designed size, and other values scale it |
| `Motion` | `MokaGridMotion` | `None` | How the background moves. Everything stands still under reduced motion |
| `BackgroundColor` | `string?` | - | Container background color. A value that is not a CSS color is ignored |
| `MinHeight` | `string?` | - | Minimum height of the container |
| `FullScreen` | `bool` | `false` | Fill the entire viewport height |
| `FillContent` | `bool` | `false` | The content fills the whole background, `MinHeight` or the window included, as a column `Justify` and `Align` place its children in. See [Placing the Content](#placing-the-content) |
| `Justify` | `MokaJustify?` | `null` | Where the content's children sit from top to bottom. Setting it fills the background as `FillContent` does |
| `Align` | `MokaAlign?` | `null` | Where the content's children sit from side to side. Setting it fills the background as `FillContent` does |

### MokaGridPattern Enum

| Value | Own cell size | Description |
|-------|---------------|-------------|
| `Lines` | 40 | Standard grid lines |
| `Dots` | 40 | Dot grid |
| `Dashed` | 40 | Dashed grid lines |
| `Cross` | 40 | Crosshatch marks at intersections |
| `DiagonalLines` | 40 | Angled lines |
| `Honeycomb` | 40 | Hexagonal pattern |
| `GraphPaper` | 16 | A fine grid with a stronger line every `MajorEvery` cells, like engineering paper |
| `Isometric` | 48 | Vertical lines crossed by two 30 degree families, the paper for drawing cubes |
| `Triangles` | 32 | Rows of equal triangles, every other one lightly filled |
| `Diamonds` | 48 | A lattice of wide diamonds with a dot where they meet |
| `Checker` | 24 | A checkerboard of faint squares, the cell being one square |
| `Rows` | 28 | Horizontal lines only, like lined paper |
| `Columns` | 48 | Vertical lines only, like a layout grid laid over the page |
| `Circuit` | 96 | Board traces with 45 degree bends, ending in round pads |
| `Topographic` | - | Contour lines around two hills, scaled to cover the box rather than repeated |
| `Radar` | 28 | Rings from the centre with a crosshair through the middle |
| `CornerMarks` | 48 | Small L marks at each cell's corners, like crop marks on a print sheet |
| `Grain` | 160 | Faint noise over the whole box, no lines at all |

### MokaGridFade Enum

| Value | Description |
|-------|-------------|
| `Center` | Full behind the middle, gone toward the corners. Shaped by `FadeStart` and `FadeEnd` |
| `Bottom` | Full along the top edge, gone by the bottom. Sits well at the top of a page |
| `Top` | Full along the bottom edge, gone at the top. For a band above a footer |
| `Frame` | Strong at the edges, clear in the middle, so no lines run behind the content |
| `Spotlight` | Faint everywhere, full in a circle that follows the pointer and rests in the centre |
| `None` | The pattern runs edge to edge at one strength |

### MokaGridGlow Enum

| Value | Description |
|-------|-------------|
| `Center` | An ellipse behind the middle, the glow `Highlighted` turns on |
| `Aurora` | Two soft clouds hanging from the top edge |
| `Corner` | One glow from the top right corner |
| `Beam` | A wide, low band of light right behind a headline |
| `TwoTone` | The accent and the info color, overlapping in the middle |
| `Horizon` | A glow rising from the bottom edge, like light under a door |

### MokaGridMotion Enum

| Value | Description |
|-------|-------------|
| `None` | Still |
| `Drift` | The pattern slides one tile down and to the right, without end, at a slow steady speed |
| `Pulse` | The glow swells and settles every five seconds. Needs a glow |
| `Sweep` | Every seven seconds a band of brighter pattern passes from left to right |

### MokaGridTone Enum

| Value | Description |
|-------|-------------|
| `Accent` | The primary border token, the original look |
| `Neutral` | Lines in the outline color, so the accent shows only in the glow |
| `AccentMajor` | Neutral minor lines with the accent on the major lines |
| `Text` | The text color at low strength. Reads as pencil on any theme |

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

## More patterns

Twelve more patterns sit beside the original six. Each has its own cell size, so leave `CellSize` unset to get the designed look, or set it to scale the pattern. `StrokeWidth` and `PatternColor` apply to all of them.

```razor
<MokaGridBackground Pattern="MokaGridPattern.GraphPaper" MinHeight="240px">
    <MokaHeading Level="2">Start a new app</MokaHeading>
</MokaGridBackground>

<MokaGridBackground Pattern="MokaGridPattern.GraphPaper" CellSize="12" MajorEvery="4" MinHeight="240px"/>

<MokaGridBackground Pattern="MokaGridPattern.CornerMarks" MinHeight="240px"/>

<MokaGridBackground Pattern="MokaGridPattern.Topographic" MinHeight="240px"/>
```

`Topographic` scales its contours to cover the box, so `CellSize` doesn't apply to it, and its lines keep `StrokeWidth` at any size. `Grain` has no lines, so `StrokeWidth` doesn't apply to it either.

## Where the pattern fades

`Fade` picks where the pattern fades out. Left unset, `FadeEdges` decides as it always has: the centre fade, or none. `FadeStart` and `FadeEnd` shape the centre fade, and `FadeMask` wins over every fade.

```razor
<MokaGridBackground Fade="MokaGridFade.Bottom" MinHeight="240px">
    <MokaHeading Level="2">Nothing deployed yet</MokaHeading>
</MokaGridBackground>

<MokaGridBackground Fade="MokaGridFade.Frame" MinHeight="240px">
    <MokaHeading Level="2">No lines behind the text</MokaHeading>
</MokaGridBackground>
```

`Spotlight` keeps the pattern faint and shows it in full in a circle that follows the pointer, easing back to the centre when the pointer leaves. A small script loaded only for this fade moves the circle in the browser, so moving the pointer sends nothing to the server.

```razor
<MokaGridBackground Pattern="MokaGridPattern.Dots" Fade="MokaGridFade.Spotlight" MinHeight="320px">
    <MokaHeading Level="2">Move the pointer here</MokaHeading>
</MokaGridBackground>
```

## Glow

`Glow` draws a soft light over the pattern in the accent. Left unset, `Highlighted` turns on the centre glow as before. `HighlightColor` replaces the accent in every glow (`TwoTone` keeps its info half), and `HighlightRadius` scales how far a glow reaches, 60 being its designed size.

```razor
<MokaGridBackground Glow="MokaGridGlow.Aurora" MinHeight="240px">
    <MokaHeading Level="2">What's new</MokaHeading>
</MokaGridBackground>

<MokaGridBackground Glow="MokaGridGlow.Beam" HighlightColor="var(--moka-color-info)" MinHeight="240px"/>
```

## Motion

`Motion` is off by default. Every motion stops when the system asks for reduced motion.

```razor
<MokaGridBackground Motion="MokaGridMotion.Drift" Glow="MokaGridGlow.Center" MinHeight="240px">
    <MokaHeading Level="2">Gateway online</MokaHeading>
</MokaGridBackground>

<MokaGridBackground Motion="MokaGridMotion.Pulse" Highlighted MinHeight="240px"/>

<MokaGridBackground Motion="MokaGridMotion.Sweep" MinHeight="240px"/>
```

- `Drift` moves the pattern under a fade that stays put, one tile per loop at the same slow speed for every pattern. `Topographic` and `Radar` don't repeat, so they stay still.
- `Pulse` moves the glow, not the pattern, so it needs `Glow` or `Highlighted`.
- `Sweep` passes a band in the accent through the pattern's own lines.

## Tone

`Tone` picks which theme colors draw the pattern. `PatternColor` wins over any tone. `Neutral` and `Text` draw at full strength unless `PatternOpacity` says otherwise.

```razor
<MokaGridBackground Tone="MokaGridTone.Neutral" Highlighted MinHeight="240px">
    <MokaHeading Level="2">Theme editor</MokaHeading>
</MokaGridBackground>

<MokaGridBackground Pattern="MokaGridPattern.GraphPaper" Tone="MokaGridTone.AccentMajor" Highlighted MinHeight="240px"/>

<MokaGridBackground Tone="MokaGridTone.Text" MinHeight="240px"/>
```

`AccentMajor` only differs from `Accent` on a pattern with major lines, and `GraphPaper` is the only one, so on every other pattern it draws like `Accent`.

## Placing the Content

The content is a block at the top of the background, as tall as what's in it, so it can't sit in the middle of a
`MinHeight` or a `FullScreen` background. `FillContent` makes it fill the whole background as a column, and `Justify`
(top to bottom) and `Align` (side to side) place what's in it, with the names `MokaFlexbox` uses. Either of those two
fills the background on its own.

```razor
<MokaGridBackground FullScreen Glow="MokaGridGlow.Center" Justify="MokaJustify.Center" Align="MokaAlign.Center">
    <div style="display: grid; gap: var(--moka-spacing-md); max-width: 26rem; text-align: center;">
        <MokaHeading Level="1">Your files, safely synced</MokaHeading>
        <span>Sign in to pick up where you left off on every device.</span>
        <MokaButton Variant="MokaVariant.Filled">Sign in</MokaButton>
    </div>
</MokaGridBackground>
```

With `FillContent` alone the children keep the full width and stack from the top, and a child with `flex: 1` takes the
height that's left. Without any of the three, the content is the block it has always been.

## Behaviour

- `PatternColor`, `HighlightColor` and `BackgroundColor` must be CSS colors: hex, a keyword such as `red`, or a color function such as `rgb()`, `hsl()`, `color-mix()` or `var()`. `DashArray` must be non-negative numbers with optional units, such as `"6, 3"`. Anything else falls back to the default, so a stray quote, parenthesis or semicolon cannot add markup to the pattern image or declarations to the style.
- Every pattern takes tokens. `Lines`, `Dots` and `DiagonalLines` are CSS gradients drawn in the color. Every other pattern is a mask over a layer filled with the color, so a `var()` color reaches it too, and no color goes into its image. Left unset, `Dashed`, `Cross` and `Honeycomb` keep their original `rgba(239, 83, 80, 0.25)` on `MokaTheme.Dark` and `Light` and take the primary border token on the new design. Every other pattern defaults to `var(--moka-color-primary-border)`.
- The SVG patterns go into the style as a percent-encoded data URI, with every value escaped first.
- Numbers are written with a decimal point and an ASCII minus sign whatever the culture, so a negative `DiagonalAngle` works under a culture such as Swedish, which writes its minus sign as U+2212.
