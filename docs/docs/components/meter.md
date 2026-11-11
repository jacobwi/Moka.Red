---
title: Meter
description: Gauge/meter visualization for displaying a value within a range.
order: 36
---

# Meter

`MokaMeter` renders a gauge-style meter that visualizes a value within a defined range. It supports colored segments for threshold zones (e.g., green/yellow/red), value formatting, and optional labels.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value |
| `Min` | `double` | `0` | Minimum value of the range |
| `Max` | `double` | `100` | Maximum value of the range |
| `Label` | `string?` | -- | Label text displayed below the meter |
| `ShowValue` | `bool` | `true` | Shows the current value as text |
| `Format` | `string?` | -- | .NET format string for the displayed value (e.g., `"P0"`, `"N1"`) |
| `Color` | `MokaColor` | `Primary` | Default fill color (overridden by segments when present) |
| `ColorValue` | `string?` | -- | A CSS color or token to fill the bar with, such as `var(--zone)`. Wins over `Color`; anything that isn't a color is ignored. Past `WarnAt` and `CriticalAt` the fill still turns amber and red |
| `Size` | `MokaSize` | `Md` | Meter size |
| `Segments` | `IReadOnlyList<MokaMeterSegment>?` | -- | Colored zones within the meter. The filled part shows them at full strength and the rest of the track faintly. With a `Target` they are the bullet's gray ranges and their colors are ignored |
| `Target` | `double?` | -- | A goal or a limit in the meter's range. Draws a bullet chart: a thin bar for the value over gray ranges, with a tick at the target |
| `TargetText` | `string` | `"target"` | What a screen reader hears before the target's value |
| `Layout` | `MokaMeterLayout` | `Stacked` | `Row` puts the name, the bar and the figure on one line |
| `WarnAt` | `double?` | -- | A share of the range, 0 to 1, from which the fill turns the warning color and the name gains an icon and `WarnText` |
| `CriticalAt` | `double?` | -- | A share of the range from which the fill turns the error color and says `CriticalText` |
| `WarnText`, `CriticalText` | `string` | `"Nearly full"`, `"Full"` | The words beside the name past each limit |
| `Figure` | `string?` | -- | The figure shown in place of the formatted value, such as "412 of 500 GB" |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaMeterSegment

| Name | Type | Description |
|------|------|-------------|
| `FromPercent` | `double` | Start of the zone as a percentage (0--100) |
| `ToPercent` | `double` | End of the zone as a percentage (0--100) |
| `Color` | `string` | CSS color for this zone (e.g., `"var(--moka-color-success)"`). A value that is not a CSS color draws the zone transparent |

## Basic Percentage

```blazor-preview
<MokaMeter Value="72" />
```

## With Label

```blazor-preview
<MokaMeter Value="45" Max="100" Label="CPU Usage" Format="N0" />
```

## Colored Segments

Define threshold zones with different colors for visual feedback.

```blazor-preview
<MokaMeter Value="78" Label="Disk Usage" Segments="@segments" />

@code {
    IReadOnlyList<MokaMeterSegment> segments = new[]
    {
        new MokaMeterSegment(0, 60, "var(--moka-color-success)"),
        new MokaMeterSegment(60, 85, "var(--moka-color-warning)"),
        new MokaMeterSegment(85, 100, "var(--moka-color-error)")
    };
}
```

## Track

On the new design (`Red`, `Gate`, `Crisp`, `Matrix` and their light partners) the empty part of the track is `--moka-color-on-surface-quaternary`, the color of things that are off. It stays visible on a card and on a panel or a `MokaStat` tile, which are drawn in `--moka-color-surface-variant`. On `MokaTheme.Dark` and `Light` the track is still `--moka-color-surface-variant`, so on a panel or a tile it blends in.

## Custom Range

```blazor-preview
<MokaMeter Value="37.5" Min="0" Max="50" Label="Temperature (C)" Format="N1" Color="MokaColor.Info" />
```

## Bullet: against a target

`Target` turns the meter into a bullet chart: the value as a thin bar over gray ranges, darker each step, and a tick
at the target. The `Segments` become the ranges, so their colors are ignored; without segments the whole track is one
range. It fits where a gauge would take a whole card, one row per measure, and a screen reader hears "62, target 70".

```blazor-preview
@code {
    MokaMeterSegment[] _ranges = [new(0, 60, ""), new(60, 85, ""), new(85, 100, "")];
}

<div style="display:flex;flex-direction:column;gap:var(--moka-spacing-md)">
    <MokaMeter Label="CPU" Value="62" Target="70" Segments="_ranges" />
    <MokaMeter Label="WAN link" Value="88" Target="75" Segments="_ranges" />
</div>
```

## A list of meters, lined up

`Layout="MokaMeterLayout.Row"` puts the name, the bar and the figure on one line. Rows inside an element with the
`moka-meter-rows` class share its three columns through `subgrid`, so every bar starts and ends at the same place
whatever the names, and under 20rem of that list the name and figure move onto a line over the bar. `WarnAt` and
`CriticalAt` are shares of the range: past them the fill turns amber or red and the name gains an icon and a word, so
the state never rests on color alone.

```blazor-preview
<div class="moka-meter-rows">
    <MokaMeter Layout="MokaMeterLayout.Row" Label="System" Value="41" Max="64" Figure="41 of 64 GB" WarnAt="0.8" />
    <MokaMeter Layout="MokaMeterLayout.Row" Label="Backups" Value="3.4" Max="4" Figure="3.4 of 4 TB" WarnAt="0.8" />
    <MokaMeter Layout="MokaMeterLayout.Row" Label="Logs" Value="19.4" Max="20" Figure="19.4 of 20 GB" WarnAt="0.8" CriticalAt="0.95" />
</div>
```

A long list of meters belongs in a `MokaVirtualList` with a fixed item height; the meter itself doesn't virtualize.

## Your own color, inside a button

`ColorValue` fills the bar with any CSS color or token, such as the color an app gives each of its zones. It is checked
before it reaches the page, and a `var()` resolves where the meter sits, so a custom property set on a parent works.
Past `WarnAt` and `CriticalAt` the fill still turns amber and red, so the warning reads the same in every zone.

The meter is built of spans, as the native `meter` element is phrasing content, so it is valid inside a button, a link
or a label: a tile that opens a zone can hold its meter. (Since 0.4.2; it looks exactly as the divs did.)

```razor
<button type="button" class="zone" style="--zone: var(--moka-label-teal)" @onclick="Open">
    <span>Office</span>
    <MokaMeter Value="0.42" Max="1" ShowValue="false" Size="MokaSize.Sm" ColorValue="var(--zone)"
               WarnAt="0.8" WarnText="Getting full" CriticalAt="0.95" CriticalText="Nearly full" Figure="42%" />
</button>
```
