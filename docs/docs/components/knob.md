---
title: Knob
description: Rotary dial input for numeric values, inspired by audio software knobs.
order: 70
---

# Knob

`MokaKnob` renders a rotary dial input reminiscent of audio software controls. Drag, scroll or use the keyboard to change the value within a min/max range. Use it for volume controls, parameter dials, or any bounded numeric input where a circular gesture feels natural.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `double` | `0` | Current value (two-way bindable) |
| `ValueChanged` | `EventCallback<double>` | -- | Callback when value changes |
| `Min` | `double` | `0` | Minimum value |
| `Max` | `double` | `100` | Maximum value |
| `Step` | `double` | `1` | Increment step |
| `Label` | `string?` | -- | Text label displayed below the value. Also the knob's accessible name |
| `ShowValue` | `bool` | `true` | Displays the numeric value in the center |
| `Format` | `string` | `"F0"` | .NET numeric format string for the displayed value. Screen readers read the value in it too |
| `Color` | `MokaColor` | `Primary` | Knob arc color theme |
| `Size` | `MokaSize` | `Md` | Knob size: `Xs`, `Sm`, `Md`, `Lg` |
| `Disabled` | `bool` | `false` | Disables interaction and takes the knob out of the tab order |
| `StartAngle` | `double` | `-135` | Arc start angle in degrees |
| `EndAngle` | `double` | `135` | Arc end angle in degrees |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Keyboard and screen readers

The knob is a WAI-ARIA slider. It takes one tab stop, screen readers read its value in `Format`, and it takes its name from `Label`. Without a label, pass `aria-label`:

```razor
<MokaKnob @bind-Value="_gain" aria-label="Gain" />
```

| Key | Action |
|-----|--------|
| Right / Up | One `Step` more |
| Left / Down | One `Step` less |
| Page Up / Page Down | Ten steps more or less, stopping at `Min` and `Max` |
| Home | `Min` |
| End | `Max` |

- The keys don't scroll the page while the knob has focus. With Ctrl, Alt or Cmd held they don't change the value, so shortcuts such as Alt+Left still reach the browser.
- `Disabled` takes the knob out of the tab order and reports `aria-disabled="true"`.
- Keys, the wheel and dragging all land on `Min` plus a whole number of steps, so a `Step` of `0.1` gives `0.7`, not `0.7000000000000001`.
- A key or a drag changes the knob even when `Value` isn't bound, and a re-render of the parent doesn't undo it. Use `@bind-Value` to keep both in step.

Up to 0.1.12 the knob took focus but no key changed it.

## Spacing and Attributes

The dial is one element, and it takes `Margin`, `Padding` and `Rounded`, as well as `Id` and any unmatched attribute. `Rounded` replaces the round outline, and the shadow and the focus ring follow it. Up to 0.1.12 the knob ignored `Rounded`.

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
