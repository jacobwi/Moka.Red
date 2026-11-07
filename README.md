# Moka.Red

A lightweight, performance-focused Blazor UI component library with 218 components targeting .NET 9 and .NET 10.

## Packages

| Package | Description |
|---------|-------------|
| `Moka.Red` | Meta-package, install this to get everything |
| `Moka.Red.Core` | Base classes, theming engine, enums, utilities |
| `Moka.Red.Icons` | SVG icon set (Lucide-style) |
| `Moka.Red.Primitives` | Button, Icon, Avatar, Badge, Chip, Typography, etc. |
| `Moka.Red.Layout` | Grid, Flexbox, Card, Paper, Toolbar, AppBar, etc. |
| `Moka.Red.Forms` | TextField, Select, DatePicker, Switch, Slider, etc. |
| `Moka.Red.Feedback` | Dialog, Toast, Tooltip, CommandPalette, etc. |
| `Moka.Red.Data` | Table, Pagination, VirtualList |
| `Moka.Red.Navigation` | Tabs, Menu, Breadcrumb, Sidebar, Stepper |
| `Moka.Red.ContextMenu` | Context menu system |
| `Moka.Red.ThemeGen` | Visual theme editor and JSON/CSS/C# export |
| `Moka.Red.Diagnostics` | Dev-time diagnostics overlay (not in the meta-package, opt in explicitly) |

## Installation

```bash
dotnet add package Moka.Red
```

## Quick Start

**1. Register services** in `Program.cs`:

```csharp
using Moka.Red.Extensions;

builder.Services.AddMokaRed();
```

**2. Wrap your layout** with `MokaThemeProvider`:

```razor
@* In your MainLayout.razor *@
<MokaThemeProvider Theme="_theme">
    @Body

    <MokaToastHost />
    <MokaDialogHost />
    <MokaContextMenuHost />
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Default;
}
```

That's it. CSS is auto-injected by `MokaThemeProvider`, so no `<link>` tags are needed. Scoped component styles are bundled automatically by Blazor via your app's `.styles.css`.

`MokaTheme.Default` is the Red preset. `Gate`, `Crisp` and `Matrix` are the other colors, each with a `Light` twin, and `MokaTheme.Dark` and `Light` are the original design. Style is separate from color: `WithDensity`, `WithTextSize`, `WithCorners`, `WithMainButton`, `WithLetterCase`, `WithTitleFont` and `WithSelection` work on any of the new presets.

The presets are shared instances, so `Theme="MokaTheme.Default"` inline is fine. Hold a theme you build with `With...` calls in a field: each call allocates a new instance, which defeats the provider's change guard.

The overlay hosts are only needed for the services you use: `MokaToastHost` for `IMokaToastService`, `MokaDialogHost` for `IMokaDialogService`, `MokaContextMenuHost` for `IMokaContextMenuService`, and `MokaCommandPalette` for `IMokaCommandPaletteService`.

**3. (Optional)** Add usings to `_Imports.razor` for convenience:

```razor
@using Moka.Red.Core.Enums
@using Moka.Red.Primitives.Button
@using Moka.Red.Primitives.Typography
@using Moka.Red.Layout.Flexbox
@using Moka.Red.Forms.TextField
@using Moka.Red.Feedback.Toast
```

## Templates

```bash
dotnet new install Moka.Red.Templates
dotnet new mokared-server -n MyApp
```

Also `mokared-wasm`, `mokared-maui`, and `mokared-rcl`.

`mokared-server --interactivity WebAssembly` or `Auto` adds a `MyApp.Client` project inside the app's folder, for the components that run in the browser. `dotnet run` from the app's folder still starts it.

## Documentation

https://jacobwi.github.io/Moka.Red/

## License

MIT
