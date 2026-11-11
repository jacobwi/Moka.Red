---
title: Chart Looks
description: How charts are drawn, which colors their series take, and how they hatch for color-blind readers, print and forced colors.
order: 55
---

# Chart Looks

Every chart reads four choices from the theme: a look, a series palette, whether marks are hatched on screen, and how it prints. Set them once on the theme and every chart on the page follows. A chart can still set its own `Look`, `Colors` and `Texture`.

```razor
@code {
    private static readonly MokaTheme Theme = MokaTheme.Red
        .WithCharts(MokaChartLook.Quiet)
        .WithChartPalette(MokaChartPalette.Reference)
        .WithChartPrint(MokaChartPrint.Hatched);
}
```

All four default to today's charts, so an app sees no change until it asks. They only take hold on the new design (`Red`, `Gate`, `Crisp`, `Matrix`): on `MokaTheme.Dark` and `MokaTheme.Light` every method returns the same theme, and charts draw exactly as they always have. People can pick them in the [appearance panel](appearance-panel.md#chart-rows).

| Method | Property | Default |
|--------|----------|---------|
| `WithCharts(MokaChartLook)` | `Charts` | `Standard` |
| `WithChartPalette(MokaChartPalette)` | `ChartPalette` | `Standard` |
| `WithChartTexture(MokaChartTexture)` | `ChartTexture` | `None` |
| `WithChartPrint(MokaChartPrint)` | `ChartPrint` | `AsShown` |

## Looks

A look sets the defaults of grid, ticks, legend, points, tooltip and keyboard together. Any parameter a chart sets itself wins over its look.

| Look | What it is |
|------|------------|
| `Standard` | Today's charts: a gridline at every quarter, a dot on every point, bars as wide as their slot, a legend of dots under the chart, the browser's own tooltip |
| `Quiet` | Hairlines at round ticks, an end dot on each line, names at the line ends, bars capped at 24 px with a 4 px data end and a 2 px gap between segments, a thin ring with gaps, the legend on top, and a crosshair that lists every series at once |
| `Dense` | Monospace ticks on the right, the legend in the title row with each series' latest value, a readout under the plot, thinner bars and ring, shorter plots. For tool pages full of numbers |
| `Soft` | A fading area under each line, smooth curves, wide bars with 8 px tops, a ring with round ends, a pill legend. For overview and home pages |
| `Bold` | 3 px lines, 32 px bars, a thick ring, bigger text and the latest value at each line's end. For a wall display read from across a room |

A chart puts its look on its root as a class, `moka-chart-look-{look}`. The class only sets variables, and every part of the chart reads them:

| Variable | Standard | Quiet | Dense | Soft | Bold |
|----------|----------|-------|-------|------|------|
| `--moka-chart-line` | 2px | 2px | 1.5px | 2px | 3px |
| `--moka-chart-dot` | 6px | 8px | 6px | 8px | 10px |
| `--moka-chart-end-dot` | 8px | 8px | 6px | 8px | 12px |
| `--moka-chart-dot-ring` | 0px | 2px | 2px | 2px | 2px |
| `--moka-chart-bar-max` | none | 24px | 16px | 40px | 32px |
| `--moka-chart-bar-gap` | 0px | 2px | 1px | 3px | 2px |
| `--moka-chart-bar-radius` | 3px | 4px | 2px | 8px | 4px |
| `--moka-chart-area` | 0.12 | 0.10 | 0.10 | 0.22, the top of a fade | 0.10 |
| `--moka-chart-tick-size` | 11px | xs | xxs | xs | sm |
| `--moka-chart-tick-font` | body | body | mono | body | body |
| `--moka-chart-plot-ratio` | 600 / 260 | 600 / 260 | 600 / 200 | 600 / 260 | 600 / 300 |
| `--moka-chart-segbar` | 28px | 28px | 20px | 32px | 36px |
| `--moka-chart-hatch-gap` | 6px | 6px | 5px | 7px | 8px |

## Series colors

Series take the palette's eight colors in a fixed order, written as `--moka-chart-1` to `--moka-chart-8`. A series keeps its color when others are hidden, because the color follows its place in the full list. A series' own `Color`, or a chart's `Colors`, still wins.

| Palette | What it is |
|---------|------------|
| `Standard` | The theme's roles in turn, as today: primary, secondary, success, info, warning, error, the text color, then primary again |
| `Reference` | Blue, orange, teal, amber, pink, green, violet, red, stepped so all eight clear 3:1 on white. Dark has its own steps |
| `AccentFirst` | The accent first, then the reference hues without the one nearest it, ordered so every pair that touches still passes |
| `Muted` | The reference hues at lower chroma, for graphite surfaces and pages full of charts. Neighbours alternate lighter and darker |

| Slot | Reference light | Reference dark | Muted light | Muted dark |
|------|-----------------|----------------|-------------|------------|
| 1 | `#3073E2` | `#4083F3` | `#4F7CBF` | `#5D8BD0` |
| 2 | `#ED6B03` | `#D56004` | `#A05725` | `#A75D2B` |
| 3 | `#0CA397` | `#019D90` | `#01A6A0` | `#01A6A0` |
| 4 | `#BF8804` | `#BB8605` | `#9B6C0A` | `#A77820` |
| 5 | `#E36AA1` | `#D9498F` | `#85375D` | `#98496E` |
| 6 | `#028039` | `#028039` | `#4C985F` | `#58A46A` |
| 7 | `#7541D9` | `#9679EB` | `#574189` | `#9682CF` |
| 8 | `#EB3F41` | `#EA5A56` | `#B35B59` | `#C76C6B` |

Every palette but Standard passes the data viz method's checks, light on white and dark on the Red, Gate and Matrix surfaces: each color inside the lightness band and over the chroma floor, 3:1 or more against the surface, and every pair that touches at least 8 apart (OKLab, times 100) for readers with protanopia or deuteranopia and 15 apart for everyone else. Reference and AccentFirst also pass where a donut's last slice touches its first, up to four slices. Muted is for touching series: in a three-slice donut its teal meets its blue at 14.5. Standard fails the color-blind check on Red, where primary and error are the same red, which is why it is not the method's pick.

### Accent first

Each accent preset has its own order, found by a search and checked by the library's tests. Where a preset's own color falls outside the lightness band (Blue, Orchid and Pink in dark, Cyan in both), slot 1 is the same hue moved into it.

| Accent | Light | Dark |
|--------|-------|------|
| Red | `#C62828` blue, teal, violet, orange, pink, amber, green | `#EF5350` same order |
| Blue | `#2F58D6` green, pink, amber, teal, orange, violet, red | `#6B8EF7` same order |
| Violet | `#5B45E0` orange, teal, amber, pink, green, blue, red | `#8F7DFF` same order |
| Cyan | `#06829C` orange, violet, amber, pink, green, blue, red | `#3BA4BC` same order |
| Orchid | `#9330B8` orange, teal, amber, pink, green, blue, red | `#C369E0` same order |
| Pink | `#B02A88` amber, blue, green, violet, orange, teal, red | `#D369B4` same order |
| Silver | Reference | Reference |

Silver is under the chroma floor, so as a first series it would read as grey; AccentFirst on Silver writes Reference. A custom accent from `MokaAccent.FromHue` or `FromColor` leads the reference hues without the one nearest its hue, in Reference order. That order can't be checked ahead of time, so check it yourself if the chart matters. A custom accent too grey to lead gets Reference too.

The theme doesn't keep an accent's id, so the preset is found by matching the theme's primary color against each preset's color for its scheme.

## Ramps, steps and greys

Some charts color by value rather than by series. These follow the accent on the new design, and read the accent and text color on the original one.

| Tokens | Read by | New design |
|--------|---------|------------|
| `--moka-chart-seq-1` to `-5` | Heatmap cells, treemap depth | Low to high: the first step sits near the surface, the last is the strongest. Red light is `#FEE0DC` to `#B7191E`, dark `#5D0004` to `#FF7F74` |
| `--moka-chart-ord-1` to `-5` | Funnel stages | Strongest first, every step standing at least 2:1 off the page |
| `--moka-chart-up`, `--moka-chart-down` | Diverging bars, candles | Blue up, red down: `#2563EB` and `#C62828` light, `#3B82F6` and `#EF5350` dark |
| `--moka-chart-muted` | Series pushed back behind a highlight, a stat's sparkline | The text color at 34% over the surface |
| `--moka-chart-grid`, `--moka-chart-axis` | Gridlines, the baseline and zero line | The text color at 7% and 22% over the surface |
| `--moka-chart-band-1` to `-3` | Bullet ranges, target bands, gantt tracks | The text color at 5%, 10% and 17% over the surface |
| `--moka-chart-zero` | A heatmap's empty cell | The text color at 5% over the surface |

The greys are mixed in OKLab over each preset's own surface, so they sit the same on graphite, blue-grey and white. Each accent preset has its own ramps, all made the same way: OKLCH lightness steps at the accent's hue, as saturated as the accent. A custom accent gets ramps made that way when the page loads.

On the original design every one of these reads a token the theme already writes (the accent at 20% to 100% for the ramps, the outline for grid and axis), so nothing there moves.

## Texture

`WithChartTexture(MokaChartTexture.Hatched)` hatches bars, ring segments and legend keys on screen, as a backup for readers who can't tell the colors apart: lines at 45 degrees for odd series and 135 for even, in a darker step of the series' own color, spaced wider from the third series on. Lines keep their color and their names at the ends. A chart's `Texture` parameter wins over the theme.

## Print and forced colors

| `MokaChartPrint` | In print and forced colors |
|------------------|---------------------------|
| `AsShown` | As on screen. Today's charts |
| `Hatched` | Colors kept, and every series hatched as with `Texture` |
| `Ink` | No color at all: hatching in the text color on the page, lines in the text color |

Both stay off the original design, which prints exactly as it always has. Marks keep their own colors under forced colors (`forced-color-adjust: none`), so the hatching survives, and they print their backgrounds (`print-color-adjust: exact`), which browsers otherwise leave out.

## Classes

A chart's marks carry these classes, which live in `moka.css` so every chart shares them:

| Class | Sets |
|-------|------|
| `moka-chart-c1` to `moka-chart-c8` | `--moka-chart-color` from the slot's token, and the slot's hatch angle and spacing |
| `moka-chart-tone-primary` to `-surface` | `--moka-chart-color` from a `MokaColor` a series names (surface reads the text color) |
| `moka-chart-muted` | `--moka-chart-color` from `--moka-chart-muted` |
| `moka-chart-hatch` | `--moka-chart-fill`: the color, or the hatching where texture, print or forced colors ask for it |

A mark that can be hatched paints `background: var(--moka-chart-fill)`; a line strokes `var(--moka-chart-color)`. The same classes work in your own drawings next to a chart, such as a legend of your own.

`moka-meter-rows` lines up meters in their row layout through subgrid: a label column as wide as the longest label, the track, and the figure.
