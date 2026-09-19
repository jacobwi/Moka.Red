---
title: Countdown
description: Timer that counts down to a date in days, hours, minutes and seconds.
order: 64
---

# Countdown

`MokaCountdown` counts down to a `DateTime` and updates once a second. Use it for launches, sales, maintenance windows and other deadlines. It shows days, hours, minutes and seconds in one of three styles, and raises `OnComplete` when it reaches zero.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `TargetDate` | `DateTime` | **required** | Moment to count down to. A `Utc` value is read as UTC, a `Local` or `Unspecified` one as local time |
| `OnComplete` | `EventCallback` | -- | Raised once when the countdown reaches zero, after the render that shows `CompletedText` |
| `CountdownStyle` | `MokaCountdownStyle` | `Boxes` | `Boxes`, `Inline` or `Flip` |
| `ShowDays` | `bool` | `true` | Shows the days unit |
| `ShowHours` | `bool` | `true` | Shows the hours unit |
| `ShowMinutes` | `bool` | `true` | Shows the minutes unit |
| `ShowSeconds` | `bool` | `true` | Shows the seconds unit |
| `ShowLabels` | `bool` | `true` | Shows a label under each unit. `Inline` never shows labels |
| `CompactLabels` | `bool` | `false` | Uses `d`, `h`, `m` and `s` instead of `Days`, `Hours`, `Minutes` and `Seconds` |
| `Separator` | `string` | `":"` | Text between units |
| `CompletedText` | `string` | `"Time's up!"` | Text shown in place of the units once the countdown ends |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of each box: every unit in `Boxes`, every card in `Flip`. `Inline` draws no boxes and rounds the whole countdown instead |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the countdown |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the units |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

`Rounded` shapes the boxes the countdown draws. With `Inline` it rounds the countdown itself, which shows once you give it a background through `Style` or `Class`. Up to 0.1.12 it always went on the countdown itself, which draws nothing in `Boxes` and `Flip`, so it did not show.

## Basic

```blazor-preview
<MokaCountdown TargetDate="_launch" />

@code {
    private readonly DateTime _launch = DateTime.Now.AddDays(3).AddHours(7).AddMinutes(20);
}
```

## Styles

`Boxes` puts each unit in its own box. `Inline` prints one monospace line with no labels. `Flip` puts each value on a card split by a line across the middle, and the card flips in each time its value changes.

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:center;gap:16px">
    <MokaCountdown TargetDate="_target" CountdownStyle="MokaCountdownStyle.Boxes" />
    <MokaCountdown TargetDate="_target" CountdownStyle="MokaCountdownStyle.Inline" />
    <MokaCountdown TargetDate="_target" CountdownStyle="MokaCountdownStyle.Flip" />
</div>

@code {
    private readonly DateTime _target = DateTime.Now.AddDays(1).AddHours(4);
}
```

## Compact Labels

```blazor-preview
<MokaCountdown TargetDate="_target" CompactLabels />

@code {
    private readonly DateTime _target = DateTime.Now.AddDays(2).AddHours(9);
}
```

## Choosing Units

A hidden unit's time moves into the next shown one. With days hidden, this sale, 2 days and 5 hours away, reads as 53 hours.

```blazor-preview
<MokaCountdown TargetDate="_saleEnds" ShowDays="false" CompactLabels />

@code {
    private readonly DateTime _saleEnds = DateTime.Now.AddDays(2).AddHours(5).AddMinutes(42);
}
```

## Completion

At zero the countdown shows `CompletedText` and raises `OnComplete`. A new `TargetDate` starts it over, so the restart button only sets a new target.

```blazor-preview
<div style="display:flex;flex-direction:column;align-items:center;gap:12px">
    <MokaCountdown TargetDate="_target"
                   ShowDays="false"
                   ShowHours="false"
                   CountdownStyle="MokaCountdownStyle.Inline"
                   CompletedText="Deploy started"
                   OnComplete="() => _runs++" />
    <MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm" OnClick="Restart">Restart</MokaButton>
    <span style="font-size:var(--moka-font-size-xs);color:var(--moka-color-on-surface-variant)">Runs completed: @_runs</span>
</div>

@code {
    private DateTime _target = DateTime.Now.AddSeconds(10);
    private int _runs;

    private void Restart() => _target = DateTime.Now.AddSeconds(10);
}
```

## Behaviour

- A timer wakes just after each whole second of the remaining time, so a digit never lingers or skips because the timer drifted.
- The remaining time is worked out in UTC on the machine running the component: the server in Blazor Server, the browser in WebAssembly. A `Utc` target is used as is. A `Local` or `Unspecified` one is converted from that machine's time zone.
- Each shown unit counts what the larger shown units leave over. A hidden unit's time moves into the next shown one, so with `ShowDays="false"` a target 2 days and 3 hours away shows 51 hours, and with `ShowHours="false"` the hours go into the minutes. Units after the last shown one are dropped. Values are padded to two digits and grow past that when needed.
- At zero the component shows `CompletedText`, adds the `moka-countdown--complete` class, stops its timer and raises `OnComplete` once, after that render. A target that has already passed completes on the first render: `OnComplete` runs after it, never during initialization or prerendering.
- `OnComplete` is awaited. An exception from the handler goes to Blazor's error handling (the nearest error boundary, or the circuit), as one from a lifecycle method would.
- A new `TargetDate` shows at once. After completion it starts the countdown over, and a new target that has already passed raises `OnComplete` again.
- In `Flip`, a value that changes is drawn as a new card that flips in. The first render does not flip. When the user asks the OS to reduce motion, the reduced-motion rule in `moka.css` cuts the animation to nothing.
- The timer is disposed with the component.

## Accessibility

The numbers change every second with no live region, so screen readers do not announce them. When the deadline matters to the task, add `role="timer"` and an `aria-label` that names it, such as `aria-label="Time until the sale ends"`. Unmatched attributes go on the root element.
