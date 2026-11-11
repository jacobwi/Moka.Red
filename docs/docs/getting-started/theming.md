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

By default the provider applies `MokaTheme.Default`, dark graphite with a red accent. The provider also renders the global stylesheet (`_content/Moka.Red.Core/moka.css`) and a `:root` block with the theme's tokens. Without a provider, link `moka.css` yourself: every `--moka-*` variable has a fallback there, so components still work.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `Theme` | `MokaTheme` | `MokaTheme.Default` | The active theme record |
| `AutoDetectColorScheme` | `bool` | `false` | Follow the OS light or dark setting, including changes while the app runs |
| `DarkTheme` | `MokaTheme` | `MokaTheme.Default` | Theme used when the OS asks for dark |
| `LightTheme` | `MokaTheme` | `MokaTheme.DefaultLight` | Theme used when the OS asks for light |
| `Nonce` | `string?` | - | Content security policy nonce for the style elements the provider writes |
| `LoadFonts` | `bool` | `true` | Link `moka-fonts.css`, which holds Inter, Space Grotesk and JetBrains Mono |
| `ChildContent` | `RenderFragment` | - | Content to wrap |

### Where the Styles Go

The provider renders the stylesheet link and the `:root` tokens in its own markup rather than through `<HeadContent>`. The head shows only one `HeadContent` at a time, so a page with its own `HeadContent` used to remove them, and hosts without a `HeadOutlet` (a MAUI `BlazorWebView`) never got them. To have the stylesheet before the first paint in an app without prerendering, also link it in `index.html`; the second copy is harmless.

### Fonts

Inter, Space Grotesk and JetBrains Mono ship with `Moka.Red.Core` (SIL Open Font License) and the outermost provider links them, so nothing calls a third party and an app works offline. The browser fetches a font file only when text on the page uses that font, and only the Latin or Latin Extended file that text needs. If your app brings its own fonts, turn the link off:

```razor
<MokaThemeProvider LoadFonts="false">
    @Body
</MokaThemeProvider>
```

### Nested Providers

A nested provider stays inside its parent. Only the outermost one fills the window, so a provider can theme a small preview in a card.

A provider inside another one themes its own subtree: its tokens go on its `.moka-root` element. The page-wide stylesheet and `:root` tokens come from the outermost provider only, so a dark sidebar inside a light app leaves overlays rendered elsewhere on the page alone.

### Following the OS Colour Scheme

`AutoDetectColorScheme` picks `DarkTheme` or `LightTheme` from the OS setting and switches when the setting changes. The detected theme is kept apart from `Theme`, so a parent re-render does not undo it.

```razor
<MokaThemeProvider AutoDetectColorScheme="true" DarkTheme="MyThemes.Dark" LightTheme="MyThemes.Light">
    @Body
</MokaThemeProvider>
```

### Content Security Policy

The provider needs no `'unsafe-eval'`. By default the subtree's tokens go in an inline `style` attribute, which a policy without `'unsafe-inline'` for styles blocks. Pass the nonce your host puts in its `Content-Security-Policy` header, and every style element the provider writes carries it, with the subtree's tokens moved into a nonced style element scoped to the provider:

```razor
<MokaThemeProvider Theme="MokaTheme.Dark" Nonce="@CspNonce">
    @Body
</MokaThemeProvider>
```

The nonce covers what the provider writes. Component stylesheets (`{AssemblyName}.styles.css` and `moka.css`) load under `style-src 'self'`, but some components set inline `style` attributes for computed sizes and positions, which a policy without `'unsafe-inline'` also blocks. Allow those with `style-src-attr 'unsafe-inline'` if you use them.

## Colors and Style

A theme is two separate choices: its **colors**, and its **style**. Pick the colors from a preset, then change any part of the style. Changing one leaves the other alone.

### Colors

| Preset | Light partner | Colors |
|---|---|---|
| `MokaTheme.Red` (also `MokaTheme.Default`) | `MokaTheme.RedLight` (also `MokaTheme.DefaultLight`) | Neutral graphite with a red accent |
| `MokaTheme.Gate` | `MokaTheme.GateLight` | Blue-grey with a blue accent |
| `MokaTheme.Crisp` | `MokaTheme.CrispLight` | The same as Gate, with the style choices set tight (below) |
| `MokaTheme.Matrix` | `MokaTheme.MatrixLight` | Near black with red-tinted borders and a red accent |

```razor
<MokaThemeProvider Theme="MokaTheme.Gate">
    @Body
</MokaThemeProvider>
```

Colors mean something on all of these. Green says allowed or good, amber limited, red blocked or wrong, blue your own or automatic, and the accent is for primary actions and what's selected. `WithDark` stays on the theme's colors, so `MokaTheme.Red.WithDark(false)` gives `RedLight`'s palette.

To change only the colors of a theme you've already styled, use `WithSurfaces` and `WithAccent`:

```csharp
MokaTheme theme = styled.WithSurfaces(MokaSurfaces.NearBlack).WithAccent(MokaAccent.Red);
```

### Style

| Choice | Method | Values (first is the default) |
|---|---|---|
| Density | `WithDensity(MokaDensity)` | `Comfortable`, `Compact` |
| Text size | `WithTextSize(MokaTextSize)` | `Normal` (14 px), `Small` (13 px) |
| Corners | `WithCorners(MokaCorners)` | `Soft` (20 px cards), `Medium` (14 px), `Tight` (10 px) |
| Main button | `WithMainButton(MokaMainButton)` | `Solid`, `Tinted`, `Outline` |
| Buttons and titles | `WithLetterCase(MokaLetterCase)` | `Sentence`, `Uppercase` |
| Title font | `WithTitleFont(MokaTitleFont)` | `SpaceGrotesk`, `Inter`, `JetBrainsMono` |
| Selected and focused | `WithSelection(MokaSelection)` | `Ring`, `Flat`, `Glow` |
| Current page | `WithCurrentPlace(MokaCurrentPlace)` | `Ring` (drawn like a selected row), `Solid` (filled with the accent, with a soft glow). Where you are in the app: the current page in the rail and side menu, the account card when one of its pages shows, and a settings list's current section. Rows picked in tables and lists keep `WithSelection` |
| Status bar | `WithStatusBar(MokaStatusBarLook)` | `Standard`, `Accent`, `Hairline`, `Floating`, `Chips`, `Segments`, `Terminal`. For bars that don't set a `Look` of their own |
| Descriptions | `WithDescriptions(bool)` | Shows or hides text saying what a thing is for, such as `MokaPageHeader.Lead` and anything with the `moka-about` class |
| Label colors | `WithLabelTone(MokaLabelTone)` | `Vivid`, `Muted`, `Pastel`. How strong the named label colors are (`MokaLabelColor`, see [Label colors](/getting-started/label-colors)) |
| Toasts | `WithToasts(MokaToastLook)` | `Standard`, `ToneEdge`, `Tinted`, `IconTile`, `Snackbar`, `Pill`, `Terminal`. For toast hosts that don't set a `Look` of their own |
| Toast stack | `WithToastStacking(MokaToastStacking)` | `List`, `Pile` (newer toasts cover older ones and the pile fans out on hover or focus) |
| Notifications | `WithNotifications(MokaNotificationLook)` | `Standard`, `Inbox`, `Cards`, `Compact`, `Grouped`, `Timeline`, `Terminal`. For notification centers and bells |
| Popups | `WithPopups(MokaPopupLook)` | `Standard`, `Flat`, `Elevated`, `Glass`, `AccentTint`, `Contrast`, `Terminal`. Menus, context menus, popovers, hover cards and the box around lists that open from a field |
| Lists | `WithLists(MokaListLook)` | `Standard`, `Check`, `Solid`, `Attached`, `Radio`, `Compact`, `Terminal`. The rows of lists that open from a field, such as `MokaSelect`'s |
| Dialogs | `WithDialogs(MokaDialogLook)` | `Standard`, `Bands`, `IconLead`, `Centered`, `Terminal` |
| Buttons | `WithButtons(MokaButtonLook)` | `Standard`, `Raised`, `Soft`, `Pill`, `Glow`, `Offset`, `Gradient`, `Ghost`. The main button, corners and letter case still apply on top |
| Component style | `WithComponentStyle(MokaComponentStyle)` | `Standard`, `Soft`, `Terminal`. Sets toasts, notifications, popups, lists, dialogs and buttons in one step, leaving the toast stack alone. `Soft` is icon tile toasts, an inbox, elevated popups, check lists, icon lead dialogs and soft buttons; `Terminal` is every one of them `Terminal`, with glowing buttons |

```csharp
MokaTheme theme = MokaTheme.Red
    .WithDensity(MokaDensity.Compact)
    .WithCorners(MokaCorners.Medium)
    .WithTitleFont(MokaTitleFont.Inter)
    .WithSelection(MokaSelection.Flat);
```

The component looks work the same way. A component style sets six of them, and a later call changes one:

```razor
<MokaThemeProvider Theme="_theme">
    @Body
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Red
        .WithComponentStyle(MokaComponentStyle.Soft)
        .WithPopups(MokaPopupLook.Glass)
        .WithToastStacking(MokaToastStacking.Pile);
}
```

A component's own `Look` parameter wins over the theme's choice for that one component. `MokaAppearancePanel` offers all of these to the person using the app, with `MokaAppearanceRows.ComponentStyle` and `MokaAppearanceRows.Components` (see [Appearance Panel](/components/appearance-panel)).

`MokaTheme.Crisp` is Gate's colors with `Compact`, `Small`, `Tight`, `Inter` and `Flat` already chosen, for tools that need to show a lot at once. Each of those can still be changed.

### The Original Design

`MokaTheme.Dark` and `MokaTheme.Light` are the original design, and they render exactly as they always have: near-black or light surfaces, glow rings, uppercase, small, outlined buttons. An app that names them keeps that look when it upgrades. The style choices above do nothing on them. For those colors with the style choices, use `MokaTheme.Matrix`.

`MokaTheme.Look` says which design a theme belongs to: `MokaLook.Matrix` for the original, `MokaLook.Gate` for everything else.

> A provider nested inside another one takes its colors and style tokens, but a `MokaLook.Matrix` theme nested inside any other theme still gets the new design's component rules. Nest the other way round if you need both on a page.

### Toggling at Runtime

Bind to a boolean state field to support a user-controlled toggle:

```razor
<MokaThemeProvider Theme="_isDark ? MokaTheme.Default : MokaTheme.DefaultLight">
    <MokaToolbar>
        <MokaButton OnClick="() => _isDark = !_isDark">Toggle Theme</MokaButton>
    </MokaToolbar>
</MokaThemeProvider>

@code {
    private bool _isDark = true;
}
```

### Accents

Seven presets ship as `MokaAccent.Blue`, `Violet`, `Cyan`, `Orchid`, `Pink`, `Silver` and `Red`. Red is the default theme's accent, and on it a primary button and an error share a color, so errors stay apart by their icons and words.

```csharp
MokaTheme theme = MokaTheme.Gate.WithAccent(MokaAccent.Cyan);
```

Any other color works too. It's read as a hue and a saturation, and each shade is found by its contrast, so every hue stays readable in both schemes. That means `Palette.Primary` can differ from the color you pass. On `MokaTheme.Dark` and `Light` the color you pass becomes the primary as it is.

```csharp
MokaTheme theme = MokaTheme.Gate.WithAccent("#3cc8e6");
MokaAccent accent = MokaAccent.FromHue(250, saturation: 0.6);
```

### Contrast

`WithHighContrast()` swaps in stronger secondary text and borders, and `WithHighContrast(false)` puts them back. It has no effect on `MokaTheme.Dark` or `Light`.

`WithDensity(0.75)` also still works: it scales the spacing tokens as well as swapping the layout tokens for their compact values.

## Custom Themes

A theme is composed of three records:

```csharp
MokaTheme
  ├── MokaPalette - colour tokens
  ├── MokaTypography - font family, sizes, weights, line heights
  └── MokaSpacing - spacing scale and border radius
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

### Theme Values From Outside the App

The provider writes the tokens into a `<style>` element, where a value holding `}` could end the `:root` rule and style the whole page. `ToCssVariables()` therefore leaves out any token whose value holds one of `; { } < > \`, and the value from `moka.css`, or from an outer provider, applies instead. This matters when theme values come from outside your code, such as an imported theme file or a color a user picked. A font family name written with CSS escapes (`\5FAE\8F6F`) is left out too: write the characters themselves. To reject a bad color up front, check it with `CssValues.IsColor` from `Moka.Red.Core.Utilities`.

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
| `--moka-color-outline-strong` | Border that must show, such as on buttons and key caps |
| `--moka-color-outline-hover` | A border under the pointer |
| `--moka-color-surface-raised` | Row hover and floating bars |
| `--moka-color-neutral-bg` | Neutral pills and the selected segment |
| `--moka-color-on-surface-2` | Text a step quieter than the main colour, such as field labels |
| `--moka-color-icon` | Icons that are not the point of what they sit in |
| `--moka-color-primary-bg` | The tint behind something selected |
| `--moka-color-primary-text` | Text on that tint |
| `--moka-color-primary-row` | The active row |
| `--moka-color-primary-focus` | The focus ring's colour |

Success, warning, error and info each have a tint and three colours to use on it, here for success: `--moka-color-success-bg` (the tint), `--moka-color-success-ink` (short text), `--moka-color-success-text` (longer text) and `--moka-color-success-line` (a border). `--moka-color-error-soft` is for counts and field errors that sit directly on a surface.

### Typography Tokens

| Property | Description |
|---|---|
| `--moka-font-family` | Base font stack |
| `--moka-font-family-display` | Page titles and big numbers |
| `--moka-font-size-root` | The size text gets when nothing else sets one |
| `--moka-font-size-heading`, `--moka-font-size-card-title`, `--moka-font-size-dialog` | A level 1 heading, a card title and a dialog title |
| `--moka-font-size-menu`, `--moka-font-size-mono` | A row in a side menu, and an address or key among other text |
| `--moka-font-weight-heading` | Headings at levels 1 to 3 |
| `--moka-font-size-display` | Page titles (default `30px`) |
| `--moka-font-size-base` | Base font size (default `13px`) |
| `--moka-font-size-sm` | Small text |
| `--moka-font-size-lg` | Large text |
| `--moka-font-weight-normal` | Regular weight (400) |
| `--moka-font-weight-medium` | Medium weight (500) |
| `--moka-font-weight-bold` | Bold weight (700) |
| `--moka-line-height-base` | Base line height |
| `--moka-control-case`, `--moka-control-tracking` | `text-transform` and `letter-spacing` of buttons, chips and tags |
| `--moka-title-case`, `--moka-title-tracking` | The same for card and dialog titles |

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
| `--moka-radius-card` | The corners of a card |
| `--moka-radius-popup` | The corners of dialogs, popovers and menus |

### Layout Tokens

| Property | Description |
|---|---|
| `--moka-page-gap` | Gap between the blocks of a page |
| `--moka-card-padding`, `--moka-card-gap` | A card's padding and the gap between its parts |
| `--moka-grid-gap` | Gap between the cells of a grid of cards |
| `--moka-panel-padding` | Padding of a panel inside a card |
| `--moka-inner-padding` | Padding of an inner card |
| `--moka-row-padding`, `--moka-item-padding` | Padding of a list row and of a small item |
| `--moka-control-height` | Height of a medium control |
| `--moka-main-padding` | Padding of the main area of a page |
| `--moka-main-padding-top`, `--moka-main-padding-bottom` | Its top and bottom, read from it the way CSS reads a padding shorthand, for a `calc()` |
| `--moka-menu-row` | Height of a row in a side menu |
| `--moka-button-height`, `--moka-button-height-sm`, `--moka-button-height-lg` | Height of a button at each size |
| `--moka-button-padding`, `--moka-button-padding-sm` | Side padding of a button |
| `--moka-input-padding` | Side padding of a text input |
| `--moka-segment-height` | Height of a segment in a segmented control |
| `--moka-cell-padding` | Padding of a table cell |
| `--moka-pill-padding` | Padding of a tag, chip or pill |
| `--moka-button-primary-bg`, `-color`, `-border`, `-hover-bg`, `-hover-border` | How a primary button is drawn, set by `WithMainButton` |
| `--moka-selected-glow`, `--moka-selected-bar` | How a selected item or table row is marked, set by `WithSelection` |
| `--moka-current-bg`, `--moka-current-color`, `--moka-current-icon`, `--moka-current-shadow`, `--moka-current-edge`, `--moka-current-pill-ring` | How the place you are in the app is drawn, set by `WithCurrentPlace`. On the dark scheme a solid fill's text is the accent's dark ink, since white on the dark accent is about 3:1 |
| `--moka-about-display` | `block` or `none` for text saying what a thing is for, set by `WithDescriptions` |
| `--moka-label-{hue}`, `--moka-label-{hue}-bg`, `--moka-label-{hue}-ink` | The named label colors, one set per `MokaLabelColor` (17): the solid for dots and icons, the tint behind a tag, and the text on that tint. Set by `WithLabelTone` |
| `--moka-heading-tracking` | The `letter-spacing` of page titles and big numbers |

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

On `MokaTheme.Dark` the shadow tokens are glow rings in the accent. On the Gate look they are drop shadows, and on its dark scheme only for things that float.

### Component Height Tokens

These follow the theme's density, and come from `MokaSpacing.StatusBarHeight`, `ToolbarHeight` and `ToolbarHeightDense`.

| Property | Comfortable | Compact | `MokaTheme.Dark`, `Light` | Description |
|---|---|---|---|---|
| `--moka-height-statusbar` | `28px` | `24px` | `22px` | Status bar height |
| `--moka-height-toolbar` | `52px` | `44px` | `40px` | Toolbar, command bar and app bar height |
| `--moka-height-toolbar-dense` | `44px` | `36px` | `32px` | The same when dense, and a tab strip |

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

<MokaThemeProvider Theme="_isDark ? MokaTheme.Default : MokaTheme.DefaultLight">
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
| The Gate look | `Theme="MokaTheme.Gate"` or `MokaTheme.GateLight` |
| Runtime toggle | `Theme="_isDark ? MokaTheme.Default : MokaTheme.DefaultLight"` |
| Custom brand colours | `MokaPalette.Light with { Primary = "..." }` |
| Your own fonts | `LoadFonts="false"` |
| Use tokens in CSS | `var(--moka-color-primary)` etc. inside `.moka-root` |
