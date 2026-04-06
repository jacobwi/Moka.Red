---
title: Knob
description: Rotary dial input for numeric values, inspired by audio software knobs.
order: 70
---

# Knob

`MokaKnob` renders a rotary dial input reminiscent of audio software controls. Drag or scroll to change the value within a min/max range. Use it for volume controls, parameter dials, or any bounded numeric input where a circular gesture feels natural.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value (two-way bindable) |
| `ValueChanged` | `EventCallback<double>` | -- | Callback when value changes |
| `Min` | `double` | `0` | Minimum value |
| `Max` | `double` | `100` | Maximum value |
| `Step` | `double` | `1` | Increment step |
| `Label` | `string?` | -- | Text label displayed below the knob |
| `ShowValue` | `bool` | `true` | Displays the numeric value in the center |
| `Format` | `string` | `"N0"` | .NET numeric format string for the displayed value |
| `Color` | `MokaColor` | `Primary` | Knob arc color theme |
| `Size` | `MokaSize` | `Md` | Knob size: `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables interaction |
| `StartAngle` | `double` | `-135` | Arc start angle in degrees |
| `EndAngle` | `double` | `135` | Arc end angle in degrees |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Volume Knob

```blazor-preview
<MokaKnob Value="50" />
```

## With Label

```blazor-preview
<MokaKnob Value="75" Label="Volume" />
```

## Colored Knobs

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Xl" Align="MokaAlign.End">
    <MokaKnob Value="30" Label="Bass" Color="MokaColor.Success" />
    <MokaKnob Value="60" Label="Mid" Color="MokaColor.Warning" />
    <MokaKnob Value="85" Label="Treble" Color="MokaColor.Error" />
</MokaFlexbox>
```

## Sizes

```blazor-preview
<MokaFlexbox Gap="MokaSpacingScale.Xl" Align="MokaAlign.End">
    <MokaKnob Value="40" Label="Small" Size="MokaSize.Sm" />
    <MokaKnob Value="60" Label="Medium" Size="MokaSize.Md" />
    <MokaKnob Value="80" Label="Large" Size="MokaSize.Lg" />
</MokaFlexbox>
```

## Disabled

```blazor-preview
<MokaKnob Value="50" Label="Locked" Disabled="true" Color="MokaColor.Secondary" />
```
