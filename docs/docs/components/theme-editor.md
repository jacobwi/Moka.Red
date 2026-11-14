---
title: Theme Tools
description: The parts of Moka.Red.ThemeGen besides the studio - a sample app drawn in any theme, preset cards, import and export in C#, JSON and CSS, and MokaThemeSerializer.
order: 104
---

# Theme Tools

`Moka.Red.ThemeGen` builds and stores themes. [`MokaThemeStudio`](/components/theme-studio) is the whole generator; the parts on this page are what it is made of, each usable alone, and `MokaThemeSerializer` does the conversions without any UI. [`MokaAppearancePanel`](/components/appearance-panel) is the panel for the people using an app.

ThemeGen is part of the `Moka.Red` meta-package. On its own:

```bash
dotnet add package Moka.Red.ThemeGen
```

It needs no service registration.

| Type | Namespace |
|------|-----------|
| `MokaThemeStudio`, `MokaThemePair` | `Moka.Red.ThemeGen` |
| `MokaThemePresets` | `Moka.Red.ThemeGen.Presets` |
| `MokaThemePreview` | `Moka.Red.ThemeGen.Preview` |
| `MokaThemeImportExport` | `Moka.Red.ThemeGen.ImportExport` |
| `MokaThemeSerializer` | `Moka.Red.ThemeGen.Serialization` |

Their buttons are `type="button"`, so a tool inside a form or `EditForm` never submits it, and every field has an accessible name.

## The Editor Is Retired

`MokaThemeEditor` and its palette, typography and spacing editors (`MokaPaletteEditor`, `MokaTypographyEditor`, `MokaSpacingEditor`) were removed in 0.6.0. They edited the palette, type and spacing records field by field, which bypassed the seed derivation and its contrast targets, and covered only part of the tokens. Use `MokaThemeStudio` instead:

- `<MokaThemeEditor @bind-Theme="_theme" />` becomes `<MokaThemeStudio @bind-Theme="_theme" />`.
- A palette color edited by hand becomes a seed (`MokaTheme.WithSeed`) or a pin (`MokaTheme.WithToken("--moka-color-primary", "#0277bd")`), which the studio's Tokens tab sets for any token.
- A type or spacing value becomes a style choice (`WithTextSize`, `WithDensity`, `WithCorners` and the rest) or a pin on its token.

## Presets

`MokaThemePresets` is a grid of preset cards, each with four swatches (primary, secondary, surface and background). A click raises `OnPresetSelected` with that theme.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OnPresetSelected` | `EventCallback<MokaTheme>` | -- | Raised with the preset the user clicked |

The eighteen presets are Red Dark and Red Light (`MokaTheme.Red` and `MokaTheme.RedLight`, the default), Gate Dark and Gate Light (`MokaTheme.Gate` and `MokaTheme.GateLight`), Crisp Dark and Crisp Light (`MokaTheme.Crisp` and `MokaTheme.CrispLight`), Matrix Dark and Matrix Light (`MokaTheme.Matrix` and `MokaTheme.MatrixLight`), Ember Dark and Ember Light (`MokaTheme.Ember` and `MokaTheme.EmberLight`, Matrix's colors with the Moka principles chosen as style), Original Dark and Original Light (`MokaTheme.Dark` and `MokaTheme.Light`), Ocean, Forest, Sunset, Rose and Monochrome (light), and Midnight (dark). Each of the six extra presets lists its 23 core colors and works out the rest from them:

- `SurfaceHover`, `Surface2` and `Surface3` step from `Surface` toward `OnSurface`, so they shade a light preset and lift a dark one.
- `OnSurfaceVariant`, `OnSurfaceTertiary` and `OnSurfaceQuaternary` fade from `OnSurface` toward `Surface`, each quieter than the last.
- The dim fills are the preset's error, warning, success and info colors at the alpha the built-in palettes use.
- The glow and border tokens come from `MokaTheme.WithAccent` with the preset's primary.

Up to 0.1.12 these colors kept their `MokaPalette` initializer values, which are the dark matrix ones: the light presets had near-black hover and surface colors, and every preset glowed red.

```blazor-preview
@code {
    MokaTheme _theme = MokaTheme.Light;
}

<div style="display:flex;flex-direction:column;gap:16px;width:100%">
    <MokaThemePresets OnPresetSelected="@(preset => _theme = preset)" />

    <MokaThemeProvider Theme="_theme">
        <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center;padding:16px;border-radius:8px;background:var(--moka-color-background)">
            <MokaButton Color="MokaColor.Primary">Primary</MokaButton>
            <MokaButton Variant="MokaVariant.Outlined" Color="MokaColor.Secondary">Secondary</MokaButton>
            <MokaChip Text="Chip" Color="MokaColor.Primary" />
            <MokaTag Text="Success" Color="MokaColor.Success" />
            <MokaTag Text="Warning" Color="MokaColor.Warning" />
        </div>
    </MokaThemeProvider>
</div>
```

## Live Preview

`MokaThemePreview` draws a small app made of the library's own components in a theme, through a real `MokaThemeProvider`: a page header, tabs, buttons in every variant, two fields (one in its error state), a select, a switch and a checkbox, alerts, cards, a table, a bar chart, label tags and a chip, a notification list drawn in place, a small dock layout, and a dialog and toasts that open inside it. Nested in an app's own provider, it writes only the tokens that differ from the app's theme.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `PreviewTheme` | `MokaTheme` | `MokaTheme.Default` | Theme to draw the sample in |
| `Text` | `MokaThemePreviewText` | `new()` | Every word of the sample |
| `Nonce` | `string?` | `null` | A content security policy nonce for its provider |

The dialog and the toasts stay inside the preview: its overlay area has layout containment, so their fixed positions are taken from it rather than the window. The Show toast button and the preview's toast host need the toast service (`AddMokaFeedback()`); while a preview is on the page its toast host also shows the app's own toasts, since the service is one per app.

Up to 0.5.x the preview was plain HTML styled to look like the components, with the tokens on its own element.

```blazor-preview
<div style="width:100%">
    <MokaThemePreview PreviewTheme="MokaTheme.GateLight.WithCorners(MokaCorners.Tight)" />
</div>
```

## Import and Export

`MokaThemeImportExport` shows a theme as C#, JSON or CSS in a highlighted `MokaCodeBlock` with its copy button, and takes pasted JSON to import. Only the format on show is written, and only when the theme or the format changes.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Theme` | `MokaTheme` | `MokaTheme.Default` | Theme to export |
| `Format` | `MokaThemeExportFormat` | `CSharp` | The format on show: `CSharp`, `Json` or `Css`. Two-way bindable |
| `FormatChanged` | `EventCallback<MokaThemeExportFormat>` | | Raised when the person picks another format |
| `OnImport` | `EventCallback<MokaTheme>` | | Raised with the theme read from the pasted JSON when the person presses Apply |
| `OnExport` | `EventCallback<string>` | | Raised with the theme as JSON when the person presses Export. The button shows only while this has a listener |
| `ShowImport` | `bool` | `true` | Whether the import field shows |
| `MaxHeight` | `string?` | `null` | How tall the code block grows before it scrolls |
| `Text` | `MokaThemeImportExportText` | `new()` | Every word it writes |

Apply with an empty field shows `ImportEmpty`. Text that is not a theme shows what is wrong and where under the field, such as `'palette' must be an object.` or the line and position of a JSON syntax error, and nothing is imported. Keys the theme does not have, such as tokens from a later version, are skipped.

Every value is checked before the theme is built, since the theme writes its values into a `<style>` element:

- Palette values must be CSS colors: hex, a keyword, or a color function such as `rgb()`, `oklch()` or `var()`.
- Font sizes and spacing values must be lengths, such as `8px` or `0.5rem`, or a `calc()`, `min()`, `max()`, `clamp()` or `var()` expression.
- Font families, weights and line heights cannot contain `;`, braces, `<`, `>` or `\`, or leave a quote, bracket or comment open.
- A pin must name a token and hold a value of the token's kind, and a seed needs an accent.

Up to 0.5.x the output was a read-only text box, the formats were plain buttons and the words were English only.

```blazor-preview
@code {
    MokaTheme _theme = MokaTheme.Red.WithDensity(MokaDensity.Compact);
}

<div style="width:100%">
    <MokaThemeImportExport Theme="_theme" OnImport="@(imported => _theme = imported)" MaxHeight="320px" />
</div>
```

## MokaThemeSerializer

Static conversions, with no UI:

```csharp
using Moka.Red.ThemeGen.Serialization;

string json = MokaThemeSerializer.ToJson(theme);
MokaTheme? restored = MokaThemeSerializer.FromJson(json); // null when the text is not a theme
string css = MokaThemeSerializer.ToCss(theme);
string code = MokaThemeSerializer.ToCSharp(theme);

if (!MokaThemeSerializer.TryFromJson(json, out MokaTheme? imported, out string? error))
{
    Console.WriteLine(error); // for example: 'palette' must be an object.
}
```

| Method | Returns | Description |
|--------|---------|-------------|
| `ToJson(theme)` | `string` | Indented JSON with camelCase names: every theme, palette, typography and spacing property, the style choices, the seed, the pins, the accent and the palette before high contrast |
| `FromJson(json)` | `MokaTheme?` | Reads JSON written by `ToJson`, as `TryFromJson` does. Returns null when the text is not a theme |
| `TryFromJson(json, out theme, out error)` | `bool` | Reads JSON written by `ToJson` and never throws. Names match in any case, and names it does not know are skipped. A value left out comes from the built-in theme of the same look, surfaces and scheme |
| `ToCss(theme)` | `string` | A stylesheet: a comment naming the root classes, then a `:root` rule with `color-scheme`, the page background on the new design and every token `MokaTheme.ToCssVariables()` writes, pins included |
| `ToCSharp(theme)` | `string` | C# that builds the theme back, written with the `With` API where it can be |

### The C# Export

`ToCSharp` writes a theme as a start and the calls that make the rest of it:

```csharp
MokaTheme.FromSeed(new MokaThemeSeed
{
	Accent = "#7C3AED",
	Neutral = "#1C1917",
	Contrast = MokaSeedContrast.More
}, dark: true)
	.WithDensity(MokaDensity.Compact)
	.WithCorners(MokaCorners.Tight)
	.WithToken("--moka-radius-card", "6px")
```

The start is a preset (`MokaTheme.Red`, `MokaTheme.CrispLight` and the rest), `MokaTheme.FromSeed(...)` for a theme from a seed, or a preset's `WithSeed(...)` when that is closer. The shortest chain wins, and every chain is built back before it is written, so the code always gives an equal theme. A theme no chain reaches, such as one with palette colors of its own, is written as a `new MokaTheme { ... }` initializer holding every value, with its pins as `.WithToken` calls after it.

Up to 0.5.x the export was always the initializer.

### The CSS Export

```css
/* Root classes: moka-root moka-dark moka-gate moka-popups-standard moka-buttons-standard */
:root {
	color-scheme: dark;
	background: var(--moka-color-background);
	--moka-color-primary: #EF5350;
	...
}
```

It is for a page that loads `moka.css` without a `MokaThemeProvider`. Put the root classes on the element that holds the app, as the provider does, since the components' rules read them. A provider sets its tokens on its own `.moka-root` element, and those win over `:root`.

### What Each Format Keeps

| Part of the theme | JSON and C# | CSS |
|-------------------|-------------|-----|
| `IsDark` | Yes | As `color-scheme` and the `moka-dark` class |
| Every palette color | Yes | Yes |
| Typography and spacing | Yes | Yes, with `FontScale` and `Density` applied |
| The style choices and component looks | Yes | As the tokens and root classes they write; a look read in code, such as `Toasts`, has none |
| The seed and the pins | Yes | As the tokens they give |
