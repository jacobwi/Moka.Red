---
title: Job Progress
description: A long job in words, with a percent, a round bar and facts such as size, speed and time so far.
order: 149
---

# Job Progress

`MokaJobProgress` shows a long job the way a person wants to hear about it: what it's doing now in a sentence, how far it has got, a round bar, and facts in small print such as "148 of 240 MB", "12.4 MB/s" and "12 s so far". With no number yet, a short bar slides along the track. For the bar on its own, use [`MokaProgress`](/components/progress).

## Setup

```razor
@using Moka.Red.Feedback.JobProgress
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string?` | - | What the job is doing now, in a sentence. It names the bar and is announced when it changes |
| `Percent` | `double?` | - | How much is done, 0 to 100. Null slides a bar along the track instead |
| `Bytes` | `long?` | - | Bytes done so far |
| `TotalBytes` | `long?` | - | Bytes the whole job takes, when known |
| `BytesPerSecond` | `double?` | - | How fast it's going. Left out when null or 0 |
| `Elapsed` | `TimeSpan?` | - | How long it has been going. The app moves it on |
| `Facts` | `IReadOnlyList<string>?` | - | More facts in the app's own words, after the ones above |
| `BytesFormat` | `string` | `"{0} of {1}"` | How done and whole read together. `{0}` is done, `{1}` the whole |
| `ElapsedFormat` | `string` | `"{0} so far"` | How the time reads. `{0}` is the time |
| `Class` | `string?` | - | Additional CSS classes on the root |
| `Style` | `string?` | - | Additional inline styles on the root |

Other attributes land on the root, and so do `Margin` and `Padding`. `Rounded` shapes the track, and the fill follows it.

## Basic

```razor
<MokaJobProgress Text="Downloading the update"
                 Percent="_percent"
                 Bytes="_bytes"
                 TotalBytes="_total"
                 BytesPerSecond="_rate"
                 Elapsed="_elapsed" />
```

The percent shows rounded down, so it never says 100% before the job is done, and the fill moves to each new value over 0.4 s. The fill uses the accent, like the main button.

## No number yet

Leave `Percent` null while there's nothing to measure, such as checking what changed. A bar 30% as wide as the track slides along it, and a dot takes the percent's place. With reduced motion asked for, the bar stands still across the whole track instead, faint.

```razor
<MokaJobProgress Text="Checking what changed" Elapsed="_elapsed" />
```

## Facts

Sizes read in the largest unit that fits, in powers of 1000: B, KB, MB, GB, TB. When done and whole share a unit it's said once, "148 of 240 MB", and otherwise each says its own, "900 KB of 1.2 GB". Time reads as "42 s", "3 min 12 s" or "1 h 4 min". Numbers follow the current culture.

Anything else goes in `Facts`, after these. To word them for another language, change `BytesFormat` and `ElapsedFormat`.

```razor
<MokaJobProgress Text="Installing the plugin"
                 Percent="40"
                 Elapsed="_elapsed"
                 Facts="@(["Step 2 of 5"])"
                 ElapsedFormat="seit {0}" />
```

## Screen readers

The sentence is a polite status, so a screen reader hears each new step, and doesn't hear every new percent. The bar is a `progressbar` named by the sentence, with `aria-valuenow` as the whole percent and `aria-valuetext` saying the percent and the amount done, "62%, 148 of 240 MB". The speed and the time are left out of it, since they change on every tick.
