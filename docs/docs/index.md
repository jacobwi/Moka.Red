---
title: "Moka.Red"
description: "Lightweight Blazor UI Library"
order: 1
layout: default
---

# Moka.Red — A lightweight, performance-focused Blazor UI component library

Moka.Red is a modular Razor Class Library (RCL) suite delivering 120+ production-ready components for Blazor applications. Designed with a dense, compact aesthetic and zero-compromise performance, it targets .NET 9 and .NET 10 and works across all Blazor hosting models.

## Key Features

- **120+ components** across 13 focused packages
- **Dense/compact design** — 13 px base font, tight spacing, high information density
- **Zero memory leaks** — `IAsyncDisposable` throughout, idempotent disposal, safe JS interop
- **Multi-target** — `net9.0` and `net10.0` in every package
- **All hosting models** — WebAssembly, Server, and Auto render modes
- **Theming engine** — CSS custom properties with light/dark support and full customisation
- **No JavaScript dependencies** — collocated `.razor.js` modules loaded lazily, no bundler required
- **Accessibility** — ARIA attributes and keyboard navigation built in
- **Tree-shaking friendly** — install only the packages you need

## Quick Install

Install the full meta-package to get every component in one step:

```bash
dotnet add package Moka.Red
```

Or add individual packages as needed — see the [Package Overview](#package-overview) table below.

## Package Overview

| Package | Components | Description |
|---|---|---|
| `Moka.Red.Core` | — | Base classes, theming engine, enums, CSS tokens |
| `Moka.Red.Icons` | 60 icons | SVG icon definitions (Lucide-style, 24×24 viewBox) |
| `Moka.Red.Primitives` | Button, Avatar, Badge, Chip, Typography, … | Foundational UI building blocks |
| `Moka.Red.Layout` | Card, Paper, Grid, Toolbar, AppBar, DockLayout, … | Page structure and layout components |
| `Moka.Red.Forms` | TextField, Select, DatePicker, Switch, Slider, … | Form inputs and controls |
| `Moka.Red.Feedback` | Dialog, Toast, Tooltip, Alert, Popover, … | Overlays and user feedback |
| `Moka.Red.Data` | Table, Pagination, VirtualList | Data display and management |
| `Moka.Red.Navigation` | Tabs, Menu, Breadcrumb, Sidebar, Stepper | Navigation and wayfinding |
| `Moka.Red.ContextMenu` | ContextMenu, ContextMenuTrigger | Right-click and long-press context menus |
| `Moka.Red.ThemeGen` | ThemeEditor, PaletteEditor, … | Visual theme editor and generator |
| `Moka.Red.Diagnostics` | DiagnosticsOverlay | Development-time diagnostics panel |
| `Moka.Red` | — | Meta-package: installs everything above |

## Minimal Example

```razor
@* App.razor *@
<MokaThemeProvider>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
    </Router>
</MokaThemeProvider>
```

```blazor-preview
@* A page using Moka.Red components *@
@page "/demo"

<MokaCard>
    <MokaTextField Label="Name" @bind-Value="_name" />
    <MokaButton Color="MokaColor.Primary" OnClick="Submit">Submit</MokaButton>
</MokaCard>

@code {
    private string _name = string.Empty;

    private void Submit() { /* ... */ }
}
```

## Getting Started

Ready to get going?

1. [Installation](getting-started/installation) — add packages and register services
2. [Quick Start](getting-started/quick-start) — a working app in minutes
3. [Theming](getting-started/theming) — customise colours, typography and spacing
