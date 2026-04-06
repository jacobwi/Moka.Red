---
title: Wizard
description: Multi-step form wizard with navigation, validation, and step indicators.
order: 60
---

# Wizard

`MokaWizard` guides users through a multi-step process with built-in step indicators, back/next/finish navigation, and optional linear validation that prevents skipping ahead until each step is valid.

## Parameters

### MokaWizard

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | `MokaWizardStep` children defining each step |
| `ActiveStep` | `int` | `0` | Zero-based index of the current step |
| `ShowStepIndicator` | `bool` | `true` | Shows the step progress indicator at the top |
| `ShowNavigation` | `bool` | `true` | Shows the Previous / Next / Finish buttons |
| `Linear` | `bool` | `false` | When `true`, prevents advancing until the current step's `IsValid` is `true` |
| `OnFinish` | `EventCallback` | -- | Callback invoked when the user clicks Finish on the last step |
| `FinishText` | `string` | `"Finish"` | Label for the finish button |
| `NextText` | `string` | `"Next"` | Label for the next button |
| `PreviousText` | `string` | `"Previous"` | Label for the previous button |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaWizardStep

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | -- | Step title shown in the indicator |
| `Icon` | `MokaIconDefinition?` | -- | Optional icon for the step indicator |
| `ChildContent` | `RenderFragment?` | -- | Content rendered when this step is active |
| `IsValid` | `bool` | `true` | Whether this step passes validation (used with `Linear`) |

## Basic Three-Step Wizard

```blazor-preview
<MokaWizard OnFinish="() => { }">
    <MokaWizardStep Title="Account">
        <MokaTextField Label="Email" Placeholder="you@example.com" />
        <MokaPasswordField Label="Password" Style="margin-top: var(--moka-spacing-sm);" />
    </MokaWizardStep>
    <MokaWizardStep Title="Profile">
        <MokaTextField Label="Display Name" Placeholder="Jane Doe" />
        <MokaTextArea Label="Bio" Placeholder="Tell us about yourself..." Rows="3" Style="margin-top: var(--moka-spacing-sm);" />
    </MokaWizardStep>
    <MokaWizardStep Title="Confirm">
        <MokaCallout Type="MokaCalloutType.Success" Title="Ready to go!">
            Review your details and click Finish to create your account.
        </MokaCallout>
    </MokaWizardStep>
</MokaWizard>
```

## Linear Validation

When `Linear` is set, the Next button is disabled until `IsValid` is `true` on the current step.

```blazor-preview
<MokaWizard Linear OnFinish="() => { }">
    <MokaWizardStep Title="Terms" IsValid="termsAccepted">
        <MokaCheckbox @bind-Value="termsAccepted" Label="I accept the terms and conditions" />
    </MokaWizardStep>
    <MokaWizardStep Title="Details">
        <MokaTextField Label="Full Name" Placeholder="Jane Doe" />
    </MokaWizardStep>
    <MokaWizardStep Title="Done">
        <MokaCallout Type="MokaCalloutType.Info" Title="All set">
            Click Finish to complete the process.
        </MokaCallout>
    </MokaWizardStep>
</MokaWizard>

@code {
    bool termsAccepted;
}
```

## Custom Button Text

Override the default button labels to match your workflow.

```blazor-preview
<MokaWizard PreviousText="Back" NextText="Continue" FinishText="Submit" OnFinish="() => { }">
    <MokaWizardStep Title="Step 1">
        <MokaParagraph>First step content.</MokaParagraph>
    </MokaWizardStep>
    <MokaWizardStep Title="Step 2">
        <MokaParagraph>Second step content.</MokaParagraph>
    </MokaWizardStep>
    <MokaWizardStep Title="Step 3">
        <MokaParagraph>Final step. Click Submit to finish.</MokaParagraph>
    </MokaWizardStep>
</MokaWizard>
```
