---
title: Tooltip
description: Pure CSS tooltip with zero JavaScript -- shows on hover or keyboard focus.
order: 21
---

# Tooltip

`MokaTooltip` is a lightweight tooltip that appears on hover or keyboard focus. It is implemented entirely in CSS with zero JavaScript interop, zero C# event handlers, and zero allocations per hover. The tooltip element is always in the DOM but takes no layout until it shows; CSS handles show/hide transitions.

A button that keeps focus after a mouse click does not keep its tooltip: only focus the keyboard put there (`:focus-visible`) shows it, so the tooltip goes when the mouse leaves and does not come back when a menu or dialog hands focus back to its button. `Delay` applies to showing only; hiding never waits.

Since 0.1.20 `MokaTooltip` ships in the `Moka.Red.Primitives` package, under the namespace it always had
(`Moka.Red.Feedback.Tooltip`, forwarded from `Moka.Red.Feedback`), and every icon-only control the library draws names
itself with it rather than with a `title`: dock panel and tab buttons, close and clear buttons, arrows, toggles and
swatches. Their names show on hover and on keyboard focus, in the same box as an app's own tooltips.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The trigger element the tooltip wraps |
| `Text` | `string?` | -- | Plain text tooltip content (ignored if `Content` is set). Under a `Title` it is the line below |
| `Content` | `RenderFragment?` | -- | Rich tooltip content. Overrides `Text`, `Title`, `Shortcut`, `Icon` and `Dot` |
| `Title` | `string?` | `null` | A bold first line with `Text` under it. The tooltip wraps, at `MaxWidth` or 20rem |
| `Shortcut` | `string?` | `null` | Key caps after the text, one `MokaKbd` per key: `"Ctrl+Shift+P"` |
| `Icon` | `MokaIconDefinition?` | `null` | An icon before the text. Wins over `Dot` |
| `Dot` | `MokaColor?` | `null` | A glowing status dot before the text, in this color |
| `Mono` | `bool` | `false` | The text in the monospace font, with figures of one width |
| `Look` | `MokaTooltipLook?` | `null` | `Standard`, `Flat`, `Elevated`, `Glass`, `AccentTint`, `Contrast`, `Terminal`. Null takes the theme's. New design only |
| `Arrow` | `bool?` | `null` | An arrow pointing at the trigger. Null takes the theme's. New design only |
| `Motion` | `MokaTooltipMotion?` | `null` | `Fade`, `Slide`, `Grow`, `Instant`. Null takes the theme's. New design only |
| `Size` | `MokaTooltipSize?` | `null` | `Compact`, `Standard`, `Roomy`. Null takes the theme's. New design only |
| `Position` | `MokaTooltipPosition` | `Top` | `Top`, `Bottom`, `Left`, `Right`, and the edge placements `TopStart`, `TopEnd`, `BottomStart`, `BottomEnd`. See [Triggers near the window edge](#triggers-near-the-window-edge) |
| `Delay` | `int` | `300` | Delay in milliseconds before showing. Hiding never waits |
| `MaxWidth` | `string?` | `null` | The widest the tooltip grows, as a CSS length such as `20rem`, before longer text wraps; never wider than the window. A line break in `Text` starts a new line. `null` keeps the text on one line |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Look

The tooltip is a raised surface with the theme's outline and popup shadow, the same family as the context menu: in the
dark theme a near-black box with a red-tinted ring and glow, in the light theme a light grey one. Three CSS variables
retune it without overriding the component's scoped styles, and they win over any of the looks below:

| Variable | Default |
|----------|---------|
| `--moka-tooltip-bg` | `var(--moka-color-surface-3)` |
| `--moka-tooltip-color` | `var(--moka-color-on-surface)` |
| `--moka-tooltip-border` | `var(--moka-color-outline)` |

```css
:root {
    --moka-tooltip-border: var(--moka-color-primary-border);
}
```

## Looks and styles

On the new design (`MokaTheme.Default` and the other presets on `MokaLook.Gate`) tooltips take four choices, each for the
whole app on the theme and for one tooltip through a parameter, which wins. Each defaults to how tooltips have always
looked, so nothing changes until you ask. `MokaTheme.Dark` and `MokaTheme.Light`, the original design, draw every tooltip
as before, whatever the theme or the parameters say.

| Choice | Theme | Parameter | Values (first is the default) |
|--------|-------|-----------|-------------------------------|
| Look | `WithTooltips(MokaTooltipLook)` | `Look` | `Standard`, `Flat`, `Elevated`, `Glass`, `AccentTint`, `Contrast`, `Terminal` |
| Arrow | `WithTooltipArrows(bool)` | `Arrow` | off, on |
| Motion | `WithTooltipMotion(MokaTooltipMotion)` | `Motion` | `Fade`, `Slide`, `Grow`, `Instant` |
| Size | `WithTooltipSize(MokaTooltipSize)` | `Size` | `Standard`, `Compact`, `Roomy` |

The looks are the popup looks drawn for a tooltip. `Flat` drops the shadow and tightens the corners, `Elevated` trades the
border for a deep shadow, `Glass` blurs what is behind it, `AccentTint` carries a trace of the accent, `Contrast` is a dark
box on a light page and a lighter one on a dark page, and `Terminal` is monospace on the page color with a tinted border
and a glow. `Slide` moves a few pixels away from the trigger as it fades in, `Grow` grows slightly from the trigger's
side, and `Instant` appears at once, still after its `Delay`. With reduced motion asked for, `Slide` and `Grow` only fade.

```razor
<MokaThemeProvider Theme="_theme">
    @Body
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Default
        .WithTooltips(MokaTooltipLook.Elevated)
        .WithTooltipArrows(true)
        .WithTooltipMotion(MokaTooltipMotion.Slide);
}
```

One tooltip can differ from the rest:

```razor
<MokaTooltip Text="Deletes the folder for good" Look="MokaTooltipLook.Contrast" Arrow="true" Size="MokaTooltipSize.Roomy">
    <MokaButton Color="MokaColor.Error" StartIcon="MokaIcons.Action.Delete">Delete</MokaButton>
</MokaTooltip>

<MokaTooltip Text="Saved" Look="MokaTooltipLook.Standard" Arrow="false" Motion="MokaTooltipMotion.Instant">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
</MokaTooltip>
```

`Look="MokaTooltipLook.Standard"` and `Arrow="false"` keep one tooltip plain under a theme that picked otherwise.

A chart's own tooltip and pin card take the theme's tooltip look too: its colors, border, corners, shadow, blur and font.
They don't take the arrow, the motion or the size, since the chart places and sizes them itself. Under `Standard` they
follow the popup look, as they always have.

`MokaAppearancePanel` offers the four choices to the person using the app, as the Tooltips, Tooltip arrows, Tooltip motion
and Tooltip size rows of `MokaAppearanceRows.Components` (see [Appearance Panel](/components/appearance-panel#component-looks)).
`MokaTheme.WithComponentStyle` leaves the tooltips as they are.

### The arrow

The arrow sits on the side of the tooltip that faces the trigger and points at the trigger's middle, kept clear of the
tooltip's rounded corners, so on an icon button with a `-Start` or `-End` placement it sits near that end. When the
browser moves the tooltip to another side for room, or slides a centred one along the window's edge, the arrow follows:
the tooltip asks the browser which fallback it took through an anchored container query. A browser with anchor
positioning but without anchored container queries can't say where it moved the tooltip, so there the arrow isn't drawn
rather than risk pointing the wrong way. Without anchor positioning the tooltip never moves, so the arrow always shows.
While a `Grow` tooltip is still growing, the arrow sits in its middle and moves to the trigger's once the scale ends.

## Rich content

Past plain text, a tooltip can carry a title, an icon or a status dot, monospace text and key caps, in that order: the
icon or dot, the title, the text, then the shortcut. They work on both designs. `Content` still wins over all of them.

```razor
<MokaTooltip Text="Command palette" Shortcut="Ctrl+K">
    <MokaButton StartIcon="MokaIcons.Action.Search" aria-label="Search"/>
</MokaTooltip>

<MokaTooltip Title="Save changes" Text="Writes the draft to disk and keeps a copy of the last version.">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
</MokaTooltip>

<MokaTooltip Text="web01 is online" Dot="MokaColor.Success">
    <MokaStatusDot Color="MokaColor.Success" Label="web01"/>
</MokaTooltip>

<MokaTooltip Text="192.168.10.24" Mono Icon="MokaIcons.Content.Globe">
    <span>Address</span>
</MokaTooltip>
```

`Shortcut` is read the way every shortcut hint in the library is (`ShortcutKeys.Split`): keys separated by `+`, so
`"Ctrl++"` is Ctrl and plus. A title makes the tooltip wrap at 20rem, or at `MaxWidth` when you set one.

## Placement

Where the browser supports CSS anchor positioning, the tooltip is placed against the window next to its
trigger rather than inside it. A panel that clips its overflow (a toolbar, a scrolling list, a dialog
body) cannot cut it off, and when the asked side has no room the browser tries the others in order:
a `Top` tooltip on a trigger at the top of the window shows below it, a `Right` one at the right edge
shows on the left, and a centred one that would reach past a side edge is shifted along instead. Every
tooltip names its own anchor, so any number of them can share a page.

## Triggers near the window edge

Without anchor positioning the tooltip sits inside its trigger and does not flip. `Top` and `Bottom` centre the tooltip under the trigger. On the right-most button of a toolbar that
reaches past the window, which makes the page scrollable sideways while the tooltip is showing. The
`-start` and `-end` placements line the tooltip up with one edge of the trigger instead:

```razor
<MokaTooltip Text="Settings" Position="MokaTooltipPosition.BottomEnd">
    <MokaButton StartIcon="MokaIcons.Action.Settings" />
</MokaTooltip>
```

Use `BottomEnd` or `TopEnd` for a trigger at the right edge and `BottomStart` or `TopStart` at the
left. A hidden tooltip takes no layout at all, so one that is not showing can never move the page.

## Long text

A tooltip keeps its text on one line, which suits a name. For a sentence or a value of unknown length, `MaxWidth` lets it
grow up to that width and then wrap, and a line break in `Text` starts a new line:

```razor
<MokaTooltip Text="@($"root@web01:22{Environment.NewLine}Connected for 2 hours")" MaxWidth="20rem">
    <MokaStatusDot Color="MokaColor.Success" Label="web01" />
</MokaTooltip>
```

## Tooltips inside tooltips

Hovering a trigger shows that trigger's own tooltip, never one belonging to a control inside it. A `MokaTooltip` around a
closable chip shows its text while the chip is under the pointer, and the chip's "Remove" appears only once the pointer
reaches the remove button.

## Inside a transformed element

With anchor positioning the tooltip is fixed against the window, and a `transform` on any element around it makes that
element the box it is placed in instead. Inside a dragged dialog that is what holds the tooltip to the dialog. The
library's own controls centre their buttons with auto margins rather than a transform for this reason.

## Basic Usage

```blazor-preview
<MokaTooltip Text="Save your changes">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
</MokaTooltip>
```

## Positions

```blazor-preview
<div style="display:flex;gap:16px;padding:40px;justify-content:center">
    <MokaTooltip Text="Top tooltip" Position="MokaTooltipPosition.Top">
        <MokaButton Variant="MokaVariant.Outlined">Top</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Bottom tooltip" Position="MokaTooltipPosition.Bottom">
        <MokaButton Variant="MokaVariant.Outlined">Bottom</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Left tooltip" Position="MokaTooltipPosition.Left">
        <MokaButton Variant="MokaVariant.Outlined">Left</MokaButton>
    </MokaTooltip>
    <MokaTooltip Text="Right tooltip" Position="MokaTooltipPosition.Right">
        <MokaButton Variant="MokaVariant.Outlined">Right</MokaButton>
    </MokaTooltip>
</div>
```

## Custom Delay

```blazor-preview
<MokaTooltip Text="Appears after 1 second" Delay="1000">
    <MokaButton Variant="MokaVariant.Soft">Slow tooltip</MokaButton>
</MokaTooltip>
```

## Rich Content

Use the `Content` parameter for tooltips with formatted markup.

```blazor-preview
<MokaTooltip>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined">Hover me</MokaButton>
    </ChildContent>
    <Content>
        <div style="text-align:center">
            <strong>Rich Tooltip</strong>
            <p style="margin:4px 0 0">Supports any Blazor content.</p>
        </div>
    </Content>
</MokaTooltip>
```
