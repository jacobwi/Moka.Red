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
| `ShowStepIndicator` | `bool` | `true` | Shows the step progress indicator at the top, or the rail with `Rail` |
| `ShowNavigation` | `bool` | `true` | Shows the Previous / Next / Finish buttons |
| `Linear` | `bool` | `false` | When `true`, prevents advancing until the current step's `IsValid` is `true` |
| `Rail` | `bool` | `false` | Draws the steps in a rail down the side and the buttons in a bar along the bottom |
| `ShowSkip` | `bool` | `false` | Shows a Skip button in the bar, on every step but the last. Only with `Rail` |
| `SkipText` | `string` | `"Skip"` | Label for the skip button |
| `OnSkip` | `EventCallback` | -- | Callback invoked when Skip is clicked, before the wizard moves on |
| `RailLabel` | `string` | `"Steps"` | The name of the rail's step list and of the progress bar |
| `ProblemText` | `string` | `"Problem"` | The word a screen reader hears before the title of a step with a problem |
| `StepCountText` | `Func<int, int, string>?` | -- | Writes the count over the progress bar from the step's number and the number of steps. Defaults to "Step 3 of 5" |
| `OnFinish` | `EventCallback` | -- | Callback invoked when the user clicks Finish on the last step |
| `FinishText` | `string` | `"Finish"` | Label for the finish button |
| `NextText` | `string?` | `"Next"`, or `"Continue"` with `Rail` | Label for the next button |
| `PreviousText` | `string?` | `"Previous"`, or `"Back"` with `Rail` | Label for the previous button |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaWizardStep

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string` | -- | Step title shown in the indicator |
| `Icon` | `MokaIconDefinition?` | -- | Optional icon for the step indicator |
| `ChildContent` | `RenderFragment?` | -- | Content rendered when this step is active |
| `IsValid` | `bool` | `true` | Whether this step passes validation (used with `Linear`) |
| `Decided` | `string?` | -- | What this step decided so far, under its title in a rail |
| `HasProblem` | `bool` | `false` | Puts a mark on this step in a rail, for a problem to come back to |

## A Rail of Steps

For a longer setup, `Rail` puts the steps down the side, each with what it decided so far and a mark on a problem. Every step reached can be clicked, so a person can go back and change an answer and then return to where they were. The step showing keeps its number even with a problem.

```razor
<MokaWizard Rail ShowSkip OnSkip="NoteSkip" OnFinish="ApplyAsync" Style="min-height: 100vh">
    <MokaWizardStep Title="Your account" Decided="BC, owner">
        ...
    </MokaWizardStep>
    <MokaWizardStep Title="Name and time" Decided="The domain looks wrong" HasProblem="true">
        ...
    </MokaWizardStep>
    <MokaWizardStep Title="Internet" Decided="@(_port ?? "Choosing a port")" IsValid="_port is not null">
        ...
    </MokaWizardStep>
</MokaWizard>
```

Back, Skip and Continue sit in a bar along the bottom. Skip passes a step that isn't valid yet, after raising `OnSkip`. To offer it on some steps only, bind `ShowSkip` to the step.

The bar is sticky rather than fixed, so it stays inside the wizard on a page with a sidebar and stays in view while a tall wizard scrolls. On a short step it sits at the bottom of the wizard, so give the wizard a height to fill, as `min-height: 100vh` does above.

Under 900 px a thin progress bar with "Step 3 of 5" and the step's title replaces the rail.

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
        <MokaCallout Type="MokaCalloutType.Tip" Title="Ready to go!">
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
        <MokaCallout Type="MokaCalloutType.Note" Title="All set">
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
