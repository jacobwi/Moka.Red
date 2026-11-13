---
title: Gauge
description: Semicircular arc gauge for displaying values within a range.
order: 47
---

# Gauge

`MokaGauge` renders a speedometer-style semicircular arc that fills proportionally to the current value within a min/max range. Use it for CPU usage, performance scores, temperature, or any bounded metric. With a `Word` it is a full ring around a score, and with `AutoScale` a half dial for a value with no fixed top, such as a live rate.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value |
| `Min` | `double` | `0` | Minimum value |
| `Max` | `double` | `100` | Maximum value. With `AutoScale` it is where the scale starts |
| `Label` | `string?` | -- | Text label below the value, and the accessible name. With a `Word` or a `Display` on a ring it is only the name |
| `Word` | `string?` | -- | A word under the number. Makes the gauge a full ring unless `StartAngle` or `EndAngle` are set |
| `WordColor` | `MokaColor?` | the gauge's `Color` | Color of `Word` |
| `Display` | `string?` | -- | Short text drawn large in place of the number, such as a letter grade, with the number as a small line under it. Makes the gauge a full ring, like `Word` |
| `DisplayColor` | `MokaColor?` | as the number | Color of `Display` |
| `DisplaySize` | `string?` | in proportion to the gauge | Size of `Display`'s text, any CSS length, so a small ring can hold a large grade |
| `AutoScale` | `bool` | `false` | Raises the scale to the next round number above the value, and draws a half dial with both ends of the scale under it |
| `ShowValue` | `bool` | `true` | Displays the numeric value in the center |
| `Format` | `string` | `"F0"` | .NET numeric format string for the displayed value |
| `Color` | `MokaColor` | `Primary` | Arc color theme |
| `Size` | `MokaSize` | `Md` | Gauge size: `Xs`, `Sm`, `Md`, `Lg` |
| `SizeValue` | `string?` | -- | Any CSS length, instead of `Size`. A bare number is read as pixels, and the number in the middle keeps its proportion |
| `StartAngle` | `double` | `-135` | Arc start angle in degrees. Doesn't apply with `AutoScale` |
| `EndAngle` | `double` | `135` | Arc end angle in degrees. Doesn't apply with `AutoScale` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## A Word Under the Number

`Word` turns the gauge into a ring: 132 px at the default `Size`, with a 38 px number and a 12 px word. The word takes the gauge's color unless `WordColor` says otherwise, and a screen reader hears it with the value. `Label` names the gauge without being drawn, since the ring has room for two lines.

```razor
<MokaGauge Value="84" Word="Good" Color="MokaColor.Success" Label="Network health" />

<MokaGauge Value="84" Word="Good" Color="MokaColor.Success" SizeValue="200px" />
```

The number and the word stay in proportion at any other size. Set `StartAngle` and `EndAngle` to keep an arc with a word under the number.

## Text of Your Own in the Middle

`Display` draws short text of the app's choosing large in the center, in the number's place, size and font, and the number moves to a small line under it. A letter grade with its score is the common one.

```razor
<MokaGauge Value="92" Display="A" Format="0' of 100'" Color="MokaColor.Success" Label="Security grade" />

<MokaGauge Value="58" Display="D" DisplayColor="MokaColor.Warning" Color="MokaColor.Warning" Word="Needs work" />
```

Like `Word`, it makes the gauge a full ring unless `StartAngle` or `EndAngle` are set, and `Label` is then only the gauge's name. `Format` still writes the small line, and a quoted part of a .NET format string is written as it is, so `"0' of 100'"` reads "92 of 100". `ShowValue="false"` leaves the display alone, and a `Word` goes under the small line. A screen reader hears the display first: "A, 92 of 100".

On a ring or a dial the display is drawn in the theme's number font, so a theme with `WithNumberFont(MokaNumberFont.Mono)`, such as `MokaTheme.Ember`, draws a grade in JetBrains Mono, and the small line takes the font of small numbers, mono there too. It takes the number's color unless `DisplayColor` says otherwise. Keep it to a few characters: it doesn't wrap, and a word belongs in `Word`.

The display is sized in proportion to the gauge, so a letter in a 64 px ring is 18 px. `DisplaySize` sets it apart from the ring, as any CSS length or a `calc()`, `var()`, `min()`, `max()` or `clamp()` of lengths, for a grade that has to read at a glance in a small space. It sizes the display alone: the small line under it and a `Word` keep theirs, so with a large display in a small ring leave them out.

```razor
<MokaGauge Value="41" Display="D" DisplaySize="1.75rem" ShowValue="false" SizeValue="64px"
           DisplayColor="MokaColor.Warning" Color="MokaColor.Warning" Label="Checkup grade" />
```

## A Scale That Grows

`AutoScale` raises the scale to the next of 1, 2.5 and 5 times a power of ten: 412 on a scale of 100 draws against 500. The dial is at most 280 px wide unless `SizeValue` says otherwise, with a 40 px number and `Label` under it. The two ends of the scale sit under the arc.

```razor
<MokaGauge Value="@_rate" AutoScale Label="Mbit/s down" />
```

The scale stays up when the value falls, so a live rate doesn't make it jump. It comes back down only when `Max` changes, so pass a new `Max` to start over.

The number in a ring or a dial is drawn in the theme's number font: the title font unless `MokaTheme.WithNumberFont(MokaNumberFont.Mono)` puts big numbers in JetBrains Mono, as `MokaTheme.Ember` does (see [Theming](/getting-started/theming#style)).

## Basic Percentage

```blazor-preview
<MokaGauge Value="72" />
```

## With Label

```blazor-preview
<MokaGauge Value="72" Label="CPU Usage" Color="MokaColor.Primary" />
```

## Custom Range (0-200 RPM)

```blazor-preview
<MokaGauge Value="145" Min="0" Max="200" Label="RPM" Format="N0" Color="MokaColor.Warning" />
```

## Colors and Sizes

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Xl" Align="MokaAlign.End">
    <MokaGauge Value="25" Label="Low" Color="MokaColor.Success" Size="MokaSize.Sm" />
    <MokaGauge Value="60" Label="Medium" Color="MokaColor.Warning" Size="MokaSize.Md" />
    <MokaGauge Value="92" Label="High" Color="MokaColor.Error" Size="MokaSize.Lg" />
</MokaFlexbox>
```

## Without Value Display

```blazor-preview
<MokaGauge Value="50" ShowValue="false" Label="Progress" Color="MokaColor.Secondary" />
```
