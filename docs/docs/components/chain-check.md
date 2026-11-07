---
title: Chain Check
description: A row of steps along a chain, where it breaks, and what to do about it.
order: 148
---

# Chain Check

`MokaChainCheck` shows the parts between someone and what they want, such as a phone, the Wi-Fi, the firewall, the router and the internet, each with a round mark in its tone. Below the row, a list says what's so for each step, with a fix where one helps. It's about where a chain breaks. For progress through steps, use [`MokaSteps`](/components/steps).

## Setup

```razor
@using Moka.Red.Primitives.ChainCheck
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Steps` | `IReadOnlyList<MokaChainStep>` | **required** | The steps, in the order someone passes them |
| `Label` | `string?` | - | Names the row of steps for screen readers: "From your phone to the internet" |
| `FindingsLabel` | `string?` | `"Findings"` | Names the list under the row for screen readers |
| `Class` | `string?` | - | Additional CSS classes on the root |
| `Style` | `string?` | - | Additional inline styles on the root |

Other attributes land on the root, and so do `Margin` and `Padding`. `Rounded` shapes the rows in the list, since the root draws nothing.

### MokaChainStep

A positional record: `new MokaChainStep(Icon, Label, Tone, Word, Sentence, Detail, Fix)`.

| Member | Type | Description |
|--------|------|-------------|
| `Icon` | `MokaIconDefinition` | Drawn in the step's mark |
| `Label` | `string` | The step's name, under its mark |
| `Tone` | `MokaChainTone` | How the step stands |
| `Word` | `string` | How it stands in a word or two, under the name. Hidden on a phone |
| `Sentence` | `string` | What's so, in the list below. Empty leaves the step out of the list |
| `Detail` | `string?` | What to do, or the numbers behind it, under the sentence |
| `Fix` | `RenderFragment?` | A button or link that puts it right, at the end of the row |

### MokaChainTone

| Value | Mark | Line into it | Row |
|-------|------|--------------|-----|
| `Ok` | Green, with a check | Green | Plain |
| `Warn` | Amber, with `!` | Green: the chain still holds | Amber tint |
| `Fail` | Red, with a cross | Red | Red tint |
| `Wait` | Grey | Dashed | Faint text |
| `Off` | Grey | Dashed | Faint text |

Steps after the first `Fail` can't be judged, so the line into them is dashed, and an `Ok`, `Warn` or `Fail` step there is drawn faded.

## Basic

```razor
<MokaChainCheck Steps="_steps" Label="From your phone to the internet" />

@code {
    private readonly MokaChainStep[] _steps =
    [
        new(MokaIcons.Status.CheckCircle, "Your phone", MokaChainTone.Ok, "Connected",
            "Your phone is connected over Wi-Fi."),
        new(MokaIcons.Status.Info, "Wi-Fi", MokaChainTone.Warn, "Weak",
            "The signal is weak here, which may slow it down."),
        new(MokaIcons.Toggle.Lock, "Firewall", MokaChainTone.Fail, "Blocked",
            "A rule on the guest network stops this phone reaching the internet."),
        new(MokaIcons.Navigation.Home, "Router", MokaChainTone.Wait, "Can't tell yet",
            "We can't check past the firewall until that's fixed."),
        new(MokaIcons.Content.Link, "Internet", MokaChainTone.Wait, "Can't tell yet",
            "The same goes for the connection out.")
    ];
}
```

## A fix

`Fix` goes at the end of the step's row. An outlined button there takes the row's tone, amber or red, so it reads as part of the problem. A filled button keeps its own look.

```razor
new(MokaIcons.Toggle.Lock, "Firewall", MokaChainTone.Fail, "Blocked",
    "A rule on the guest network stops this phone reaching the internet.",
    Detail: "Rule 4, made on 2 November.",
    Fix: @<MokaButton Variant="MokaVariant.Outlined" Size="MokaSize.Sm" OnClick="OpenRule">Open the rule</MokaButton>)
```

## Screen readers

The row is an ordered list, so each step is heard in order with its name and its word: "Firewall, Blocked". The marks are hidden from them. Each row in the list below starts with its step's name, which only screen readers hear. Give the row a `Label` so the list says what it's about.

## On a phone

Under 720 px the marks shrink to 36 px and the word under each name goes. Screen readers still hear it.
