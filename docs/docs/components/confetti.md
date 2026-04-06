---
title: Confetti
description: Celebration confetti burst effect triggered programmatically.
order: 48
---

# Confetti

`MokaConfetti` renders a burst of colorful confetti particles when activated. Wrap it around content or place it anywhere on the page, then toggle `Active` to fire the animation. The effect automatically resets after the configured duration.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Active` | `bool` | `false` | Triggers the confetti burst when set to `true` (bindable via `@bind-Active`) |
| `ActiveChanged` | `EventCallback<bool>` | -- | Fires when the burst completes and `Active` resets to `false` |
| `ParticleCount` | `int` | `50` | Number of confetti particles |
| `Duration` | `int` | `2000` | Burst duration in milliseconds |
| `Colors` | `IReadOnlyList<string>?` | -- | Custom particle colors (CSS color values). Uses a default rainbow palette when `null` |
| `Spread` | `double` | `60` | Angular spread of the burst in degrees |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Trigger

```blazor-preview
<MokaConfetti @bind-Active="_confettiActive" />
<MokaButton OnClick="() => _confettiActive = true">Celebrate!</MokaButton>

@code {
    private bool _confettiActive;
}
```

## Custom Colors

```blazor-preview
<MokaConfetti @bind-Active="_confettiColors" Colors="@(new[] { "#d32f2f", "#f44336", "#ff8a80", "#ffffff" })" />
<MokaButton OnClick="() => _confettiColors = true" Color="MokaColor.Error">Red Burst</MokaButton>

@code {
    private bool _confettiColors;
}
```

## More Particles and Wider Spread

```blazor-preview
<MokaConfetti @bind-Active="_confettiBig" ParticleCount="100" Spread="90" Duration="3000" />
<MokaButton OnClick="() => _confettiBig = true" Color="MokaColor.Success" Variant="MokaVariant.Outlined">Big Celebration!</MokaButton>

@code {
    private bool _confettiBig;
}
```
