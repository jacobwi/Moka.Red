---
title: "Installation"
description: "Add Moka.Red to your Blazor project"
order: 1
---

# Installation

This page covers every step needed to add Moka.Red to an existing Blazor project: package installation, service registration, and theme setup.

## Requirements

- .NET 10 SDK
- A Blazor project (WebAssembly, Server, or Auto render mode)

## NuGet Packages

### Full Meta-package

The simplest approach is to install the single meta-package, which pulls in every Moka.Red component package:

```bash
dotnet add package Moka.Red
```

A published Blazor WebAssembly app trims the code it doesn't use, so the meta-package doesn't make it download every component's code. Its CSS is another matter: see below. `Moka.Red.Diagnostics` isn't part of the meta-package and stays whole if you add it.

### Individual Packages

Add only the packages whose components you use. The reason is CSS: an app's `{AppName}.styles.css` imports the scoped stylesheet of every Moka.Red package it references, directly or through another package, and the browser loads them all with the first page, whether that page uses those components or not. Trimming removes unused code from a WebAssembly download, and a Blazor Server app sends no code at all, but nothing removes unused CSS. So on Blazor Server, and on WebAssembly too, the packages you reference decide how much CSS every visitor loads.

Measured on a Release publish, as `MapStaticAssets` serves the minified stylesheets:

| The app references | Scoped CSS | gzip | Brotli |
|--------------------|-----------:|-----:|-------:|
| `Moka.Red` (every package) | 1,602 KB | 185 KB | 150 KB |
| `Moka.Red.Data` (every package but ThemeGen) | 1,534 KB | 178 KB | 143 KB |
| `Moka.Red.Forms` and `Moka.Red.Layout` | 1,098 KB | 127 KB | 101 KB |
| `Moka.Red.Layout` (brings Feedback) | 715 KB | 85 KB | 69 KB |
| `Moka.Red.Primitives` | 413 KB | 48 KB | 38 KB |

`moka.css` and the fonts come from `MokaThemeProvider` and are the same whichever packages you reference.

A package brings the packages it builds on, and their CSS with them:

| Package | Brings |
|---------|--------|
| `Moka.Red.Core` | nothing |
| `Moka.Red.Icons` | Core |
| `Moka.Red.Primitives` | Core, Icons |
| `Moka.Red.Forms`, `Moka.Red.Feedback`, `Moka.Red.ContextMenu` | Core, Icons, Primitives |
| `Moka.Red.Layout`, `Moka.Red.Navigation` | Core, Icons, Primitives, Feedback |
| `Moka.Red.Data` | every package but ThemeGen and Diagnostics |
| `Moka.Red.ThemeGen` | every package but Diagnostics |
| `Moka.Red.Diagnostics` | Core, Icons, Primitives |

So an app with a table or a chart gains little from individual packages. An app of forms, cards and dialogs loads about two thirds of the CSS, and one of cards and dialogs less than half. Then register the services your components need (see [Service Registration](#service-registration) below).

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

If you installed the `Moka.Red` meta-package, a single call registers every service the components use:

```csharp
// Program.cs
using Moka.Red.Extensions;

builder.Services.AddMokaRed();
```

`AddMokaRed(o => o.Theme = MokaTheme.Gate)` picks the theme it registers, and `o.Feedback` sets the Feedback options below.

### Individual packages

`AddMokaRed()` is these six calls, each from the package that owns the service. Without the meta-package, make the calls for the components you use:

```csharp
// Program.cs
using Moka.Red.ContextMenu.Extensions;
using Moka.Red.Core.Extensions;
using Moka.Red.Feedback.Extensions;

builder.Services.AddMokaRedCore();      // Moka.Red.Core
builder.Services.AddMokaAnnouncer();    // Moka.Red.Core
builder.Services.AddMokaFeedback();     // Moka.Red.Feedback
builder.Services.AddMokaContextMenu();  // Moka.Red.ContextMenu
builder.Services.AddMokaHotkeys();      // Moka.Red.Core
builder.Services.AddMokaPageTitles();   // Moka.Red.Core
```

| Call | Registers | Needed by |
|------|-----------|-----------|
| `AddMokaRedCore(theme)` | The theme (`MokaTheme.Default` unless you pass one) as a root-level cascading value | Components outside a `MokaThemeProvider`, and interactive components under a statically rendered provider: a cascading value doesn't cross a render mode boundary, a root-level one does. Under an interactive provider everything gets the theme from the provider |
| `AddMokaAnnouncer()` | `IMokaAnnouncer`, what the app says to screen readers | `MokaAnnouncerHost`, `MokaToastHost` and `MokaBannerHost`. `AddMokaFeedback()` calls it too |
| `AddMokaFeedback()` | `IMokaToastService`, `IMokaDialogService`, `IMokaNotificationService`, `IMokaCommandPaletteService`, `IMokaBannerService`, browser storage for dismissed banners (unless you registered an `IMokaBannerStorage`), `MokaFeedbackOptions` and the announcer | `MokaToastHost` and `MokaToast`, `MokaDialogHost`, `MokaNotificationCenter`, `MokaCommandPalette` and `MokaBannerHost`. `MokaTable`'s `ConfirmDelete` asks through the dialog service and deletes without asking when it isn't registered |
| `AddMokaContextMenu()` | `IMokaContextMenuService`, the one shared menu | `MokaContextMenuHost` and `MokaRowMenuColumn`. `MokaContextMenuTrigger`, `MokaRadialMenuTrigger` and `MokaFileBrowser` use the shared menu when it is registered and a host is in the layout, and draw their own otherwise |
| `AddMokaHotkeys()` | `IMokaHotkeyService`, the app's shortcut registry | `MokaHotkeyHost`. Without it `MokaCheatsheet` shows only the groups you pass it, and `MokaAppShell` has no `[` shortcut for its rail |
| `AddMokaPageTitles()` | `IMokaPageTitleService` | `MokaPageTitle` and `MokaPageTitleHost` |

A component that injects a service fails to render without it ("There is no registered service of type ..."), and `MokaRowMenuColumn` fails when its menu opens. A host such as `MokaToastHost` goes in the layout once, as with the meta-package.

`AddMokaFeedback(f => ...)`, or `MokaRedOptions.Feedback` with the meta-package, sets app-wide toast defaults and a
limit on stored notifications (`MokaFeedbackOptions`, see [Toast](/components/toast) and
[Notification](/components/notification)).

Some services aren't part of `AddMokaRed()` with either setup, and you add them when you use what needs them:

| Call | Namespace | For |
|------|-----------|-----|
| `AddMokaTabs<TValue>()` | `Moka.Red.Navigation.Extensions` | `MokaTabContainer<TValue>` |
| `AddMokaSystemNotifier()` | `Moka.Red.Feedback.Extensions` | Notifications outside the page, through the browser's Notification API |
| `AddMokaFilePicker<TPicker>()` | `Moka.Red.Core.Extensions` | An app's own file picker (see below) |
| `AddMokaDockWindows()`, `AddMokaDockStorage<TStorage>()` | `Moka.Red.Layout.DockLayout` | `MokaDockLayout` panels in their own window on Blazor Server, and dock layouts saved somewhere other than the browser |
| `AddMokaDiagnostics()` | `Moka.Red.Diagnostics.Extensions` | The diagnostics overlay (see below) |

In `_Imports.razor`, the hosts live in `Moka.Red.Core.Announcer`, `Moka.Red.Core.Hotkeys`, `Moka.Red.Core.PageTitle`, `Moka.Red.Feedback.Toast`, `Moka.Red.Feedback.Dialog`, `Moka.Red.Feedback.Banner`, `Moka.Red.Feedback.Notification`, `Moka.Red.Feedback.CommandPalette` and `Moka.Red.ContextMenu`. [Namespace Imports](imports) lists the namespaces of every package, with a minimal set for individual packages.

### File picker (Blazor Hybrid)

`MokaFileUpload`, `MokaFileBrowser`'s Upload and `MokaQRScanner`'s image button open the browser's file dialog. A MAUI app on Windows should register its own picker instead, because WebView2's file dialog can end the app. `AddMokaRed()` doesn't do this; with no picker the browser's dialog is used.

```csharp
using Moka.Red.Core.Extensions;

builder.Services.AddMokaFilePicker<MauiFilePicker>();
```

[File Picker](/components/file-picker) has a complete `MauiFilePicker` over `FilePicker.Default`.

### Diagnostics (development only)

The diagnostics overlay is a dev-time tool. Register it separately:

```csharp
using Moka.Red.Diagnostics.Extensions;

builder.Services.AddMokaDiagnostics();
```

It runs only in the `Development` environment: elsewhere the overlay and its page render nothing, unless you set `EnableOutsideDevelopment`. See [Diagnostics](/components/diagnostics#outside-development).

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

- **Global styles** (reset, tokens, text utilities) come from `MokaThemeProvider`, which renders the `moka.css` link and the theme's `:root` tokens itself. They don't depend on `<HeadContent>` or a `HeadOutlet`, so they also work in MAUI Blazor Hybrid.
- **Scoped component styles** are bundled by Blazor into your app's `{AppName}.styles.css`, which your project template already references.

Just make sure your `App.razor` (or `index.html`) includes the standard Blazor styles link:

```html
<link rel="stylesheet" href="MyApp.styles.css" />
```

> **Note:** Replace `MyApp` with your application's assembly name.

### Minified stylesheets

The packages ship their CSS minified: `moka.css`, `moka-fonts.css`, `moka-flags.css`, `moka-boot.css` and each package's scoped bundle, the one your `{AppName}.styles.css` imports. Only comments and whitespace are removed, so every rule is the one in the source, and an app sees the same files at the same paths. About a third less CSS reaches the browser (scoped styles of every package together: 277 KB gzipped before, 184 KB after).

Nothing changes in an app. `MapStaticAssets` fingerprints, hashes and compresses the minified files like any other static web asset. To read a stylesheet in the browser, use the dev tools' pretty-print, or the source on GitHub.

Building Moka.Red from source, a Debug build keeps the stylesheets as written and a Release build minifies them. `-p:MokaMinifyCss=false` turns minifying off.

## Theme Setup

Wrap your layout content with `MokaThemeProvider`. It cascades the theme to all components and renders the Moka stylesheet link:

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
