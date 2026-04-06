---
title: "Installation"
description: "Add Moka.Red to your Blazor project"
order: 1
---

# Installation

This page covers every step needed to add Moka.Red to an existing Blazor project: package installation, service registration, and theme setup.

## Requirements

- .NET 9 SDK or .NET 10 SDK
- A Blazor project (WebAssembly, Server, or Auto render mode)

## NuGet Packages

### Full Meta-package

The simplest approach is to install the single meta-package, which pulls in every Moka.Red component package:

```bash
dotnet add package Moka.Red
```

### Individual Packages

For fine-grained control — particularly useful in large projects or when bundle size matters — add only the packages you need:

```bash
# Core base classes and theming (required by all other packages)
dotnet add package Moka.Red.Core

# SVG icons
dotnet add package Moka.Red.Icons

# Foundational components: Button, Avatar, Badge, Chip, Typography, etc.
dotnet add package Moka.Red.Primitives

# Layout components: Card, Paper, Grid, Toolbar, AppBar, DockLayout, etc.
dotnet add package Moka.Red.Layout

# Form inputs: TextField, Select, DatePicker, Switch, Slider, etc.
dotnet add package Moka.Red.Forms

# Overlay and feedback: Dialog, Toast, Tooltip, Alert, Popover, etc.
dotnet add package Moka.Red.Feedback

# Data display: Table, Pagination, VirtualList
dotnet add package Moka.Red.Data

# Navigation: Tabs, Menu, Breadcrumb, Sidebar, Stepper
dotnet add package Moka.Red.Navigation

# Context menus
dotnet add package Moka.Red.ContextMenu

# Visual theme editor
dotnet add package Moka.Red.ThemeGen

# Development-time diagnostics overlay
dotnet add package Moka.Red.Diagnostics
```

## Service Registration

### Meta-package (recommended)

If you installed the `Moka.Red` meta-package, a single call registers all services (theming, toast, dialog, notification, command palette):

```csharp
// Program.cs
using Moka.Red.Extensions;

builder.Services.AddMokaRed();
```

### Individual packages

When using individual packages, register services for the packages you installed:

```csharp
// Program.cs
using Moka.Red.Feedback.Extensions;

// Toast, dialog, notification, command palette services
builder.Services.AddMokaFeedback();
```

### Diagnostics (development only)

The diagnostics overlay is a dev-time tool. Register it separately:

```csharp
using Moka.Red.Diagnostics.Extensions;

builder.Services.AddMokaDiagnostics();
```

### Full `Program.cs` Example

```csharp
using Moka.Red.Extensions;
using Moka.Red.Diagnostics.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registers theming + all feedback services
builder.Services.AddMokaRed();

// Dev-time diagnostics (optional)
builder.Services.AddMokaDiagnostics();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

## CSS Setup

**No manual CSS `<link>` tags are needed.** Moka.Red handles CSS automatically:

- **Global styles** (reset, tokens, text utilities) are injected by `MokaThemeProvider` via `<HeadContent>`.
- **Scoped component styles** are bundled by Blazor into your app's `{AppName}.styles.css`, which your project template already references.

Just make sure your `App.razor` (or `index.html`) includes the standard Blazor styles link:

```html
<link rel="stylesheet" href="MyApp.styles.css" />
```

> **Note:** Replace `MyApp` with your application's assembly name.

## Theme Setup

Wrap your layout content with `MokaThemeProvider`. This cascades the theme to all components and auto-injects the Moka CSS:

```razor
@* MainLayout.razor *@
<MokaThemeProvider>
    @Body
</MokaThemeProvider>
```

That's it. See [Theming](theming) for dark mode, custom palettes, and token customization.

## Namespace Imports

Add Moka.Red namespaces to `_Imports.razor` so components are available project-wide:

```razor
@* _Imports.razor *@
@using Moka.Red.Core.Enums
@using Moka.Red.Core.Theming
@using Moka.Red.Icons
@using Moka.Red.Primitives.Button
@using Moka.Red.Primitives.Typography
@using Moka.Red.Primitives.Icon
@using Moka.Red.Layout.Flexbox
@using Moka.Red.Layout.Grid
@using Moka.Red.Layout.Card
@using Moka.Red.Forms.TextField
@using Moka.Red.Feedback.Toast
@using Moka.Red.Feedback.Dialog
```

Add only the namespaces for the packages and components you use.

## Verification

Build your project to confirm everything is wired up correctly:

```bash
dotnet build
```

Then navigate to [Quick Start](quick-start) to render your first Moka.Red component.
