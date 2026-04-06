---
title: "Theming"
description: "Customise colours, typography, and spacing in Moka.Red"
order: 3
---

# Theming

Moka.Red's theming system is built entirely on CSS custom properties. `MokaThemeProvider` generates a set of `--moka-*` variables as an inline style on a wrapping `<div class="moka-root">` element, making every token available to child components without any runtime style injection or JavaScript.

## MokaThemeProvider

Wrap your application (or a subtree) in `MokaThemeProvider` to activate theming:

```blazor-preview
<MokaThemeProvider>
    <MokaButton>Hello from themed context</MokaButton>
</MokaThemeProvider>
```

By default the provider applies the built-in **light** theme. Every `--moka-*` variable falls back to a value defined in `moka.css` when `MokaThemeProvider` is absent, so components remain functional without it.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Theme` | `MokaTheme` | `MokaTheme.Light` | The active theme record |
| `ChildContent` | `RenderFragment` | — | Content to wrap |

## Light and Dark Themes

### Built-in Dark Theme

Pass `Theme="MokaTheme.Dark"` to switch the built-in palette to its dark variant:

```blazor-preview
<MokaThemeProvider Theme="MokaTheme.Dark">
    <MokaButton>Dark themed button</MokaButton>
</MokaThemeProvider>
```

The dark palette swaps the primary colour from `#d32f2f` to `#ef5350` and adjusts all surface, background, and text tokens accordingly.

### Toggling at Runtime

Bind to a boolean state field to support a user-controlled toggle:

```blazor-preview
<MokaThemeProvider Theme="_isDark ? MokaTheme.Dark : MokaTheme.Light">
    <MokaToolbar>
        <MokaButton OnClick="() => _isDark = !_isDark">Toggle Theme</MokaButton>
    </MokaToolbar>
</MokaThemeProvider>

@code {
    private bool _isDark;
}
```

## Custom Themes

A theme is composed of three records:

```csharp
MokaTheme
  ├── MokaPalette   — colour tokens
  ├── MokaTypography — font family, sizes, weights, line heights
  └── MokaSpacing   — spacing scale and border radius
```

### Creating a Custom Palette

`MokaPalette` exposes a static factory method and `with`-expression support:

```csharp
// Use the built-in light palette as a starting point
var palette = MokaPalette.Light with
{
    Primary     = "#1565c0",   // deep blue
    PrimaryDark = "#003c8f",
    Secondary   = "#6a1b9a",   // purple
    Success     = "#2e7d32",
    Error       = "#c62828",
};
```

### Creating a Custom Theme

```csharp
var myTheme = new MokaTheme
{
    Palette    = palette,
    Typography = MokaTypography.Default with { FontSizeBase = "14px" },
    Spacing    = MokaSpacing.Default,
};
```

### Applying the Theme

Pass the theme instance to `MokaThemeProvider`:

```blazor-preview
<MokaThemeProvider Theme="_theme">
    <MokaButton Color="MokaColor.Primary">Custom Blue Theme</MokaButton>
</MokaThemeProvider>

@code {
    private static readonly MokaTheme _theme = new()
    {
        Palette = MokaPalette.Light with
        {
            Primary     = "#1565c0",
            PrimaryDark = "#003c8f",
        },
    };
}
```

### Storing Themes in a Service

For larger apps, define themes centrally and inject them where needed:

```csharp
// Services/AppThemeService.cs
public sealed class AppThemeService
{
    public MokaTheme Light { get; } = new()
    {
        Palette = MokaPalette.Light with { Primary = "#1565c0" },
    };

    public MokaTheme Dark { get; } = new()
    {
        Palette = MokaPalette.Dark with { Primary = "#42a5f5" },
    };
}
```

```csharp
// Program.cs
builder.Services.AddSingleton<AppThemeService>();
```

```razor
@inject AppThemeService Themes

<MokaThemeProvider Theme="_isDark ? Themes.Dark : Themes.Light">
    @Body
</MokaThemeProvider>

@code {
    private bool _isDark;
}
```

## CSS Custom Properties

Every token `MokaThemeProvider` emits follows a `--moka-{category}-{name}` naming convention. You can consume these properties in your own CSS anywhere inside `.moka-root`.

### Colour Tokens

| Property | Description |
|---|---|
| `--moka-color-primary` | Primary brand colour |
| `--moka-color-primary-dark` | Darker shade of primary (hover states) |
| `--moka-color-on-primary` | Text colour on primary backgrounds |
| `--moka-color-secondary` | Secondary brand colour |
| `--moka-color-error` | Error / destructive colour |
| `--moka-color-warning` | Warning colour |
| `--moka-color-success` | Success / confirmation colour |
| `--moka-color-info` | Informational colour |
| `--moka-color-surface` | Card and panel background |
| `--moka-color-background` | Page background |
| `--moka-color-on-surface` | Primary text colour |
| `--moka-color-on-surface-variant` | Secondary/muted text colour |
| `--moka-color-outline` | Border and divider colour |
| `--moka-color-outline-variant` | Subtle border colour |

### Typography Tokens

| Property | Description |
|---|---|
| `--moka-font-family` | Base font stack |
| `--moka-font-size-base` | Base font size (default `13px`) |
| `--moka-font-size-sm` | Small text |
| `--moka-font-size-lg` | Large text |
| `--moka-font-weight-normal` | Regular weight (400) |
| `--moka-font-weight-medium` | Medium weight (500) |
| `--moka-font-weight-bold` | Bold weight (700) |
| `--moka-line-height-base` | Base line height |

### Spacing Tokens

| Property | Description |
|---|---|
| `--moka-spacing-xs` | Extra-small spacing unit |
| `--moka-spacing-sm` | Small spacing unit |
| `--moka-spacing-md` | Medium spacing unit (base) |
| `--moka-spacing-lg` | Large spacing unit |
| `--moka-spacing-xl` | Extra-large spacing unit |
| `--moka-radius-sm` | Small border radius |
| `--moka-radius-md` | Medium border radius |
| `--moka-radius-lg` | Large border radius |
| `--moka-radius-full` | Pill / fully-rounded radius |

### Elevation / Shadow Tokens

| Property | Description |
|---|---|
| `--moka-shadow-0` | No shadow (elevation 0) |
| `--moka-shadow-1` | Subtle lift (elevation 1) |
| `--moka-shadow-2` | Medium lift (elevation 2) |
| `--moka-shadow-3` | High lift (elevation 3) |
| `--moka-shadow-4` | Highest lift (elevation 4) |
| `--moka-shadow-popup` | Dropdown/popup shadow |
| `--moka-shadow-popup-lg` | Large popup shadow (context menu, popover) |
| `--moka-shadow-modal` | Dialog/modal shadow |
| `--moka-shadow-subtle` | Toolbar/appbar shadow |

Shadow tokens automatically adjust intensity for dark themes (stronger shadows on dark backgrounds).

### Component Height Tokens

| Property | Default | Description |
|---|---|---|
| `--moka-height-statusbar` | `22px` | StatusBar height |
| `--moka-height-toolbar` | `40px` | Toolbar height |
| `--moka-height-toolbar-dense` | `32px` | Dense toolbar height |

### Using Tokens in Your Own CSS

```css
/* MyComponent.razor.css */
.my-panel {
    background: var(--moka-color-surface);
    border: 1px solid var(--moka-color-outline);
    border-radius: var(--moka-radius-md);
    padding: var(--moka-spacing-md);
    font-size: var(--moka-font-size-base);
    color: var(--moka-color-on-surface);
}

.my-panel__title {
    font-weight: var(--moka-font-weight-medium);
    color: var(--moka-color-primary);
}
```

## Theme Toggle Example

A complete light/dark toggle with theme persistence via `localStorage`:

```blazor-preview
@inject IJSRuntime JS

<MokaThemeProvider Theme="_isDark ? MokaTheme.Dark : MokaTheme.Light">
    <MokaCard Style="max-width: 400px; margin: 2rem auto;">
        <div style="display: flex; align-items: center; justify-content: space-between;">
            <span>Dark Mode</span>
            <MokaButton OnClick="ToggleTheme">Toggle</MokaButton>
        </div>

        <MokaButton Color="MokaColor.Primary" Style="width: 100%;">Primary Action</MokaButton>
        <MokaButton Color="MokaColor.Secondary" Variant="MokaVariant.Outlined" Style="width: 100%;">Secondary Action</MokaButton>
    </MokaCard>
</MokaThemeProvider>

@code {
    private bool _isDark;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var stored = await JS.InvokeAsync<string>("localStorage.getItem", "moka-theme");
            _isDark = stored == "dark";
            StateHasChanged();
        }
    }

    private async Task ToggleTheme()
    {
        _isDark = !_isDark;
        await JS.InvokeVoidAsync("localStorage.setItem", "moka-theme", _isDark ? "dark" : "light");
    }
}
```

## Summary

| Scenario | Approach |
|---|---|
| Default light theme | `<MokaThemeProvider>` with no parameters |
| Built-in dark theme | `Theme="MokaTheme.Dark"` |
| Runtime toggle | `Theme="_isDark ? MokaTheme.Dark : MokaTheme.Light"` |
| Custom brand colours | `MokaPalette.Light with { Primary = "..." }` |
| Use tokens in CSS | `var(--moka-color-primary)` etc. inside `.moka-root` |
