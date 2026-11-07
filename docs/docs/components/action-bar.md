---
title: Action Bar
description: A floating bar for something waiting on the person, with a countdown and room for a list of changes.
order: 142
---

# Action Bar

`MokaActionBar` floats at the bottom of the page while something waits on the person: a change to keep or undo, an update to install. For a count of picked items, use [`MokaSelectionBar`](/components/selection-bar).

## Setup

```razor
@using Moka.Red.Feedback.ActionBar
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Visible` | `bool` | `false` | Shows the bar |
| `Icon` | `RenderFragment?` | - | At the start of the bar. `Countdown` takes its place |
| `Title` | `RenderFragment?` | - | What's waiting, in a strong line. Announced to screen readers when the bar shows |
| `Detail` | `RenderFragment?` | - | More about it, under the title |
| `Actions` | `RenderFragment?` | - | The buttons, at the end of the bar |
| `Body` | `RenderFragment?` | - | Above the row, for a list of what's waiting. It scrolls past 340 px |
| `Countdown` | `TimeSpan?` | - | Time left, drawn as a ring |
| `Duration` | `TimeSpan?` | - | What a full ring stands for. Left out, it's the longest `Countdown` since the bar was shown |
| `OnCountdownEnd` | `EventCallback` | - | Raised once when the bar's own count reaches zero |
| `Label` | `string` | `"Waiting on you"` | The bar's name for screen readers |
| `CountdownLabel` | `string` | `"Time left"` | Said before the time for screen readers |
| `Class` | `string?` | - | Additional CSS classes on the bar |
| `Style` | `string?` | - | Additional inline styles on the bar |

`Margin`, `Padding` and `Rounded` land on the bar too.

## Basic

```razor
<MokaActionBar Visible="_waiting">
    <Icon><MokaIcon Icon="MokaIcons.Status.Warning" SizeValue="20px"/></Icon>
    <Title>A new version is ready</Title>
    <Detail>It restarts the router for about a minute.</Detail>
    <Actions>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary" OnClick="Later">Later</MokaButton>
        <MokaButton Color="MokaColor.Primary" OnClick="InstallAsync">Install</MokaButton>
    </Actions>
</MokaActionBar>
```

## A countdown

The ring counts down by itself from each new value it's given, and from the start again each time the bar is shown. It turns amber for the last 15 seconds.

```razor
<MokaActionBar Visible="_waiting"
               Countdown="TimeSpan.FromSeconds(60)"
               OnCountdownEnd="UndoAsync">
    <Title>3 firewall changes are live</Title>
    <Detail>Keep them, or they undo themselves.</Detail>
    <Actions>
        <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary" OnClick="UndoAsync">Undo</MokaButton>
        <MokaButton Color="MokaColor.Primary" OnClick="KeepAsync">Keep</MokaButton>
    </Actions>
</MokaActionBar>
```

If the deadline lives elsewhere, pass a new `Countdown` whenever you know better, along with a fixed `Duration` so the ring stays honest. The bar starts counting from each value.

```razor
<MokaActionBar Visible="_waiting" Countdown="_server.TimeLeft" Duration="_server.Timeout">
    ...
</MokaActionBar>
```

## A list of changes

`Body` sits above the row and scrolls past 340 px. Leave it out until someone asks for it.

```razor
<MokaActionBar Visible="_waiting" Countdown="_left" Body="@(_showChanges ? Changes : null)">
    <Title>3 changes are live</Title>
    <Actions>
        <MokaButton Variant="MokaVariant.Text" OnClick="() => _showChanges = !_showChanges">Show changes</MokaButton>
    </Actions>
</MokaActionBar>
```

## Layout

It's centered with flexbox on a strip that lets clicks through beside the bar, never with a `transform`, so a `MokaTooltip` on a button in it isn't captured. It's at most 860 px wide, and on a narrow window its buttons wrap onto their own row.

It covers the end of the page, so leave room there. `--moka-main-padding` is usually enough for the bar without a `Body`.
