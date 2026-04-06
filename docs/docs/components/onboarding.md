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
| `ActiveStep` | `int` | `0` | Zero-based index of the current step |
| `Active` | `bool` | `false` | Whether the tour is currently active |
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
