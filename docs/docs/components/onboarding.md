---
title: Onboarding
description: Spotlight tour overlay that guides users through key UI elements step by step.
order: 62
---

# Onboarding

`MokaOnboarding` provides a guided spotlight tour that highlights target elements on the page with an overlay, tooltip descriptions, and step-by-step navigation. It is useful for first-time user experiences and feature discovery.

## Parameters

### MokaOnboarding

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Steps` | `IReadOnlyList<MokaOnboardingStep>` | -- | The list of tour steps |
| `ActiveStep` | `int` | `0` | Zero-based index of the current step, and the step a tour starts on (two-way bindable) |
| `ActiveStepChanged` | `EventCallback<int>` | -- | Callback when the user moves to another step |
| `Active` | `bool` | `false` | Whether the tour is currently active (two-way bindable) |
| `ActiveChanged` | `EventCallback<bool>` | -- | Callback when the tour ends, finished or skipped |
| `OnComplete` | `EventCallback` | -- | Callback when the user finishes all steps |
| `OnSkip` | `EventCallback` | -- | Callback when the user skips the tour |
| `ShowSkipButton` | `bool` | `true` | Shows a Skip button to exit the tour early |
| `ShowStepCount` | `bool` | `true` | Shows "Step X of Y" indicator |
| `OverlayOpacity` | `double` | `0.5` | Opacity of the backdrop overlay (0.0 to 1.0) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaOnboardingStep

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | -- | Step title shown in the tooltip |
| `Description` | `string` | -- | Step description text |
| `TargetSelector` | `string` | -- | CSS selector for the element to highlight (e.g., `"#my-button"`) |
| `Position` | `MokaPopoverPosition` | `Bottom` | Tooltip position relative to the target element |

## Basic Three-Step Tour

```blazor-preview
<MokaButton id="onboard-btn-1" OnClick="() => tourActive = true">Start Tour</MokaButton>
<MokaButton id="onboard-btn-2" Variant="MokaVariant.Outlined">Settings</MokaButton>
<MokaButton id="onboard-btn-3" Variant="MokaVariant.Soft" Color="MokaColor.Success">Save</MokaButton>

<MokaOnboarding Active="tourActive"
                Steps="tourSteps"
                OnComplete="() => tourActive = false"
                OnSkip="() => tourActive = false" />

@code {
    bool tourActive;

    static readonly MokaOnboardingStep[] tourSteps =
    [
        new() { Title = "Welcome", Description = "Click here to start the tour anytime.", TargetSelector = "#onboard-btn-1" },
        new() { Title = "Settings", Description = "Configure your preferences here.", TargetSelector = "#onboard-btn-2" },
        new() { Title = "Save", Description = "Don't forget to save your changes!", TargetSelector = "#onboard-btn-3" }
    ];
}
```

## Starting and restarting

- Each time the parent turns the tour on (`Active` from `false` to `true`) it starts on `ActiveStep`. A tour that was finished or skipped opens on its first step again, or on the step you pass, even when `ActiveStep` isn't bound and is `0` every time.
- A tour that is on from the first render measures its first target right after that render and shows the spotlight then.
- While the tour runs, a parent render that passes the same `Active` and `ActiveStep` again leaves the user on their step. Use `@bind-ActiveStep` to follow the step from the parent.

Up to 0.1.12 a finished tour started again on its last step, and a tour that was on from the first render stayed dark, with no card and no way out, until something else rendered it.

## Keyboard and behaviour

- The step card is a modal dialog named by the step title. Focus moves into it when a step shows, Tab stays inside it, and focus returns to where it was when the tour ends.
- Escape skips the tour, the same as the skip button, and does nothing while `ShowSkipButton` is false.
- A target outside the viewport is scrolled into view before the spotlight is drawn, and the spotlight follows it while the page scrolls or the window resizes.
- A step whose `TargetSelector` matches nothing shows its card in the middle of the screen without a spotlight, so the user can still move on or skip.

## Without Skip Button

Hide the skip option to encourage users to complete the full tour.

```blazor-preview
<MokaButton id="onboard-no-skip" OnClick="() => noSkipActive = true" Color="MokaColor.Secondary">Start Guided Tour</MokaButton>

<MokaOnboarding Active="noSkipActive"
                ShowSkipButton="false"
                Steps="noSkipSteps"
                OnComplete="() => noSkipActive = false" />

@code {
    bool noSkipActive;

    static readonly MokaOnboardingStep[] noSkipSteps =
    [
        new() { Title = "Step 1", Description = "This is the first and only highlighted element.", TargetSelector = "#onboard-no-skip" }
    ];
}
```
