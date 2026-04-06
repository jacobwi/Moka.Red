---
title: "Quick Start"
description: "Build a working Blazor app with Moka.Red in minutes"
order: 2
---

# Quick Start

This guide walks you through building a small but realistic Blazor page with Moka.Red: a themed layout, a form with validation, and a toast notification. By the end you will have seen the most common patterns used across the library.

Ensure you have completed the [Installation](installation) steps before continuing.

## Step 1 — Wrap Your App in MokaThemeProvider

`MokaThemeProvider` injects CSS custom properties into the DOM and cascades the active `MokaTheme` to every descendant component. Place it as high as possible — typically in `App.razor` or your root layout.

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase

<MokaThemeProvider>
    @Body
</MokaThemeProvider>
```

No manual `<link>` tags are needed — `MokaThemeProvider` auto-injects the Moka CSS via `<HeadContent>`. With no parameters it applies the built-in light theme. See [Theming](theming) for dark mode and custom themes.

## Step 2 — Add a Button

`MokaButton` supports four variants (`Filled`, `Outlined`, `Text`, `Soft`), all seven semantic colours, and three sizes.

```blazor-preview
@page "/demo"

<MokaButton>Default</MokaButton>
<MokaButton Variant="MokaVariant.Outlined">Outlined</MokaButton>
<MokaButton Variant="MokaVariant.Soft" Color="MokaColor.Success">Soft Green</MokaButton>
<MokaButton Variant="MokaVariant.Text" Color="MokaColor.Error">Danger</MokaButton>
<MokaButton Disabled>Disabled</MokaButton>
```

### Button with an Icon

```blazor-preview
<MokaButton Color="MokaColor.Primary">
    <MokaIcon Icon="MokaIcons.Action.Add" />
    New Item
</MokaButton>
```

## Step 3 — Build a Form

Moka.Red form components bind directly to C# properties and integrate with Blazor's `EditForm` / `EditContext` validation pipeline.

```blazor-preview
@page "/demo"

<EditForm Model="_model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />

    <MokaCard Style="max-width: 480px; gap: 1rem; display: flex; flex-direction: column;">
        <MokaTextField
            Label="Full Name"
            @bind-Value="_model.Name"
            Placeholder="Jane Smith" />

        <MokaTextField
            Label="Email"
            @bind-Value="_model.Email"
            Type="email"
            Placeholder="jane@example.com" />

        <MokaSelect
            Label="Role"
            @bind-Value="_model.Role"
            Items="_roles" />

        <MokaButton Type="submit" Color="MokaColor.Primary">
            Save
        </MokaButton>
    </MokaCard>
</EditForm>

@code {
    private readonly FormModel _model = new();

    private readonly List<string> _roles = ["Administrator", "Editor", "Viewer"];

    private void HandleSubmit()
    {
        // _model is valid here
    }

    private sealed class FormModel
    {
        [Required] public string Name  { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer";
    }
}
```

## Step 4 — Add Toast Notifications

`MokaToastHost` renders the toast stack; `IMokaToastService` is the injectable service for showing toasts from anywhere in the component tree.

### Register Services

If you used the meta-package, `AddMokaRed()` already registered toast/dialog services. Otherwise:

```csharp
// Program.cs
builder.Services.AddMokaFeedback();
```

### Add the Host

Place `MokaToastHost` once, near the root of your layout (outside any scroll containers):

```razor
@* Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase

<MokaThemeProvider>
    <div class="layout">
        @Body
    </div>

    <MokaToastHost />
</MokaThemeProvider>
```

### Show a Toast

```blazor-preview
@inject IMokaToastService Toast

<MokaButton OnClick="ShowToast">Notify Me</MokaButton>

@code {
    private void ShowToast()
    {
        Toast.ShowSuccess("Changes saved successfully.");
    }
}
```

## Full Working Example

The snippet below is a self-contained page demonstrating everything covered above.

```blazor-preview
@page "/quick-start"
@inject IMokaToastService Toast

<MokaCard Style="max-width: 520px; margin: 2rem auto; display: flex; flex-direction: column; gap: 1rem;">

    <h2 style="margin: 0;">Create Account</h2>

    <EditForm Model="_model" OnValidSubmit="HandleSubmit">
        <DataAnnotationsValidator />
        <ValidationSummary />

        <MokaTextField
            Label="Display Name"
            @bind-Value="_model.Name"
            Placeholder="Your name" />

        <MokaTextField
            Label="Email"
            @bind-Value="_model.Email"
            Type="email"
            Placeholder="you@example.com" />

        <MokaSelect
            Label="Plan"
            @bind-Value="_model.Plan"
            Items="_plans" />

        <div style="display: flex; gap: .5rem; justify-content: flex-end;">
            <MokaButton Variant="MokaVariant.Text" OnClick="Reset">Reset</MokaButton>
            <MokaButton Type="submit" Color="MokaColor.Primary">Create</MokaButton>
        </div>
    </EditForm>
</MokaCard>

<MokaToastHost />

@code {
    private readonly RegistrationModel _model = new();

    private readonly List<string> _plans = ["Free", "Pro — $9/month", "Team — $29/month"];

    private void HandleSubmit()
    {
        Toast.ShowSuccess($"Welcome, {_model.Name}!");
        Reset();
    }

    private void Reset()
    {
        _model.Name  = string.Empty;
        _model.Email = string.Empty;
        _model.Plan  = "Free";
    }

    private sealed class RegistrationModel
    {
        [Required, MinLength(2)] public string Name  { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        public string Plan { get; set; } = "Free";
    }
}
```

## Next Steps

- [Theming](theming) — switch to dark mode or build a custom colour palette
- Component Reference — browse all 120+ components with live examples
