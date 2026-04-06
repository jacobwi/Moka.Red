---
title: Stagger
description: Staggered list animation that reveals items one by one with configurable timing.
order: 68
---

# Stagger

`MokaStagger<TItem>` renders a collection of items with staggered entrance animations. Each item appears after a configurable delay relative to the previous one, creating a cascading reveal effect.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<TItem>` | -- | Collection of items to render |
| `ItemTemplate` | `RenderFragment<TItem>` | -- | Template for each item |
| `StaggerDelay` | `int` | `100` | Delay between each item's animation start, in milliseconds |
| `Animation` | `MokaStaggerAnimation` | `FadeIn` | Animation type: `FadeIn`, `SlideUp`, `SlideLeft`, `ScaleIn` |
| `Duration` | `int` | `400` | Duration of each item's animation in milliseconds |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Fade In List

Items fade in one after another with a 100ms stagger.

```blazor-preview
<MokaStagger Items="_fruits" Context="fruit">
    <ItemTemplate>
        <MokaCallout Type="MokaCalloutType.Info" Title="@fruit">A delicious fruit.</MokaCallout>
    </ItemTemplate>
</MokaStagger>

@code {
    private static readonly string[] _fruits = ["Apple", "Banana", "Cherry", "Date"];
}
```

## Slide Up Cards

Use `SlideUp` animation for a more dynamic entrance.

```blazor-preview
<MokaGrid Columns="2" Gap="MokaSpacingScale.Sm">
    <MokaStagger Items="_features" Animation="MokaStaggerAnimation.SlideUp" StaggerDelay="150" Context="feature">
        <ItemTemplate>
            <MokaCard Title="@feature" Outlined>
                <ChildContent>
                    <MokaText Size="MokaSize.Sm">Feature description goes here.</MokaText>
                </ChildContent>
            </MokaCard>
        </ItemTemplate>
    </MokaStagger>
</MokaGrid>

@code {
    private static readonly string[] _features = ["Fast", "Reliable", "Secure", "Scalable"];
}
```
