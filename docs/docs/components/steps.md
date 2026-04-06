---
title: Steps
description: Lightweight numbered step indicator for multi-step workflows.
order: 32
---

# Steps

`MokaSteps` displays a horizontal numbered step indicator that highlights the current step and marks completed steps. Unlike `MokaStepper`, this is a lightweight visual indicator without content panels.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Steps` | `IReadOnlyList<string>` | -- | List of step labels |
| `CurrentStep` | `int` | `0` | Zero-based index of the active step |
| `Color` | `MokaColor` | `Primary` | Accent color for active and completed steps |
| `Size` | `MokaSize` | `Md` | Size of the step indicators |
| `Clickable` | `bool` | `false` | Whether steps can be clicked to navigate |
| `OnStepClick` | `EventCallback<int>` | -- | Callback when a clickable step is clicked, receives the step index |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Steps

```blazor-preview
<MokaSteps Steps="@steps" CurrentStep="1" />

@code {
    IReadOnlyList<string> steps = new[] { "Account", "Profile", "Review", "Confirm" };
}
```

## Colored

```blazor-preview
<div style="display:flex;flex-direction:column;gap:16px">
    <MokaSteps Steps="@steps" CurrentStep="2" Color="MokaColor.Success" />
    <MokaSteps Steps="@steps" CurrentStep="1" Color="MokaColor.Secondary" />
</div>

@code {
    IReadOnlyList<string> steps = new[] { "Upload", "Process", "Verify", "Done" };
}
```

## Clickable

```blazor-preview
<MokaSteps Steps="@steps" CurrentStep="@current" Clickable OnStepClick="i => current = i" />
<MokaParagraph>Current step: @steps[current]</MokaParagraph>

@code {
    int current = 0;
    IReadOnlyList<string> steps = new[] { "Billing", "Shipping", "Payment", "Summary" };
}
```

## Sizes

```blazor-preview
<div style="display:flex;flex-direction:column;gap:16px">
    <MokaSteps Steps="@steps" CurrentStep="1" Size="MokaSize.Sm" />
    <MokaSteps Steps="@steps" CurrentStep="1" Size="MokaSize.Md" />
    <MokaSteps Steps="@steps" CurrentStep="1" Size="MokaSize.Lg" />
</div>

@code {
    IReadOnlyList<string> steps = new[] { "Step 1", "Step 2", "Step 3" };
}
```
