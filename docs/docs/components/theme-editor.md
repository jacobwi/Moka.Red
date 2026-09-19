---
title: Theme Editor
description: Visual editor for MokaTheme palettes, typography and spacing, with presets, a live preview, and JSON, CSS and C# export.
order: 104
---

# Theme Editor

`Moka.Red.ThemeGen` is a visual editor for `MokaTheme`. `MokaThemeEditor` puts it all in one tabbed panel: palette, typography and spacing editors, a preset gallery and an import and export tab, next to a live preview. Each of those parts is also a component you can use alone, and `MokaThemeSerializer` does the conversions without any UI.

ThemeGen is part of the `Moka.Red` meta-package. On its own:

```bash
dotnet add package Moka.Red.ThemeGen
```

It needs no service registration.

| Type | Namespace |
|------|-----------|
| `MokaThemeEditor` | `Moka.Red.ThemeGen` |
| `MokaPaletteEditor`, `MokaTypographyEditor`, `MokaSpacingEditor` | `Moka.Red.ThemeGen.Editors` |
| `MokaThemePresets` | `Moka.Red.ThemeGen.Presets` |
| `MokaThemePreview` | `Moka.Red.ThemeGen.Preview` |
| `MokaThemeImportExport` | `Moka.Red.ThemeGen.ImportExport` |
| `MokaThemeSerializer` | `Moka.Red.ThemeGen.Serialization` |

The ThemeGen components inherit plain `ComponentBase`, so they take no `Class`, `Style` or extra attributes. Wrap one in an element to size or place it.

Their buttons are `type="button"`, so an editor inside a form or `EditForm` never submits it, and every field has an accessible name. Up to 0.1.12 the buttons submitted an enclosing form and the fields had no names.

## MokaThemeEditor

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Theme` | `MokaTheme` | `MokaTheme.Light` | Theme to edit. Two-way bindable. The editor works on its own copy, and a different theme from the parent replaces it |
| `ThemeChanged` | `EventCallback<MokaTheme>` | -- | Raised with the new theme after every edit, preset or import |
| `OnExport` | `EventCallback<string>` | -- | Raised with the theme as JSON when the user clicks Export. The Export button only shows when this is set |
| `ShowPreview` | `bool` | `true` | Shows the live preview panel |
| `ShowImportExport` | `bool` | `true` | Shows the Import/Export tab |
| `Compact` | `bool` | `false` | One column, with the preview under the editor instead of in a 280px column beside it |

The tabs are Palette, Typography, Spacing, Presets and Import/Export.

### Example

```blazor-preview
@code {
    MokaTheme _theme = MokaTheme.Light;
}

<div style="width:100%">
    <MokaThemeEditor @bind-Theme="_theme" Compact />
</div>
```

### Applying the Edited Theme

Give the bound theme to `MokaThemeProvider`, and the app restyles as the user edits:

```razor
<MokaThemeProvider Theme="_theme">
    <MokaThemeEditor @bind-Theme="_theme" OnExport="SaveAsync" />
    @Body
</MokaThemeProvider>

@code {
    MokaTheme _theme = MokaTheme.Dark;

    // OnExport hands over the theme as JSON, ready to store.
    async Task SaveAsync(string json) => await Settings.SaveThemeAsync(json);
}
```

`Settings` stands for your own storage. Load the theme back with `MokaThemeSerializer.FromJson`.

Up to 0.1.12 the editor wrote each edit into its own `Theme` parameter, so with a one-way `Theme` the next parent render threw the edits away.

## Presets

`MokaThemePresets` is a grid of preset cards, each with four swatches (primary, secondary, surface and background). A click raises `OnPresetSelected` with that theme.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OnPresetSelected` | `EventCallback<MokaTheme>` | -- | Raised with the preset the user clicked |

The eight presets are Moka Light and Moka Dark (the built-in `MokaTheme.Light` and `MokaTheme.Dark`), Ocean, Forest, Sunset, Rose and Monochrome (light), and Midnight (dark). Each of the six extra presets lists its 23 core colors and works out the rest from them:

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

## Palette, Typography and Spacing Editors

Each editor edits one part of the theme and reports a new record through its `...Changed` callback. They keep no copy of their own, so bind them two-way (`@bind-Palette`) or write the new value back yourself. A field applies its change when it loses focus or on Enter, and a color picker when it closes.

| Component | Parameter | Type | Default | Callback |
|-----------|-----------|------|---------|----------|
| `MokaPaletteEditor` | `Palette` | `MokaPalette` | `MokaPalette.Light` | `PaletteChanged` (`EventCallback<MokaPalette>`) |
| `MokaTypographyEditor` | `Typography` | `MokaTypography` | `MokaTypography.Default` | `TypographyChanged` (`EventCallback<MokaTypography>`) |
| `MokaSpacingEditor` | `Spacing` | `MokaSpacing` | `MokaSpacing.Default` | `SpacingChanged` (`EventCallback<MokaSpacing>`) |

- **Palette** covers the 23 core colors in five groups: Primary, Secondary, Surface, Semantic (error, warning, success, info and their `On` colors) and Outline. Each row has a swatch, a native color picker and a text field. The text field takes `#rgb`, `#rgba`, `#rrggbb` or `#rrggbbaa`. Anything else is dropped and the field shows the current color again. The picker has no alpha: it shows a hex or `rgb()` color as `#rrggbb` and any other value as black, and a picked color replaces the value with an opaque `#rrggbb`. Up to 0.1.12 the text field stored any text, and a `#` value shorter than `#rrggbb` other than `#rgb` (such as `#abcd`) crashed the render.
- **Typography** covers both font families, the font sizes from `Xs` to `Xxl` (each with an "Aa" sample), the three line heights and the five weights, all as CSS strings.
- **Spacing** covers the spacing scale from `Xxs` to `Xxl`, drawn as bars, and the radii from `None` to `Full`, drawn as sample corners.

The glow, border, dim and extended surface colors (such as `PrimaryGlow`, `PrimaryBorder` or `SurfaceHover`) are not in the palette editor, and changing `Primary` there leaves them as they were. `MokaTheme.WithAccent(color)` recomputes them from one accent color.

```blazor-preview
@code {
    MokaSpacing _spacing = MokaSpacing.Default;

    MokaTheme SpacedTheme => MokaTheme.Light with { Spacing = _spacing };
}

<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(260px,1fr));gap:16px;width:100%;align-items:start">
    <MokaSpacingEditor @bind-Spacing="_spacing" />
    <MokaThemePreview PreviewTheme="SpacedTheme" />
</div>
```

## Live Preview

`MokaThemePreview` shows sample buttons, a card, a text field, the semantic colors and the type scale in a given theme. It sets the theme's tokens on its own root element, so it needs no provider and leaves the rest of the page alone. The samples are plain HTML styled by the preview, not Moka.Red components.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `PreviewTheme` | `MokaTheme` | `MokaTheme.Light` | Theme to show |

## Import and Export

`MokaThemeImportExport` shows the theme as JSON, CSS or C# in a read-only box with a Copy button, and takes pasted JSON to import.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Theme` | `MokaTheme` | `MokaTheme.Light` | Theme to export |
| `OnImport` | `EventCallback<MokaTheme>` | -- | Raised with the parsed theme when the user clicks Apply |
| `OnExport` | `EventCallback<string>` | -- | Raised with the theme as JSON when the user clicks Export. The Export button only shows when this is set |

Apply with an empty box shows "Please paste theme JSON." Text that is not a theme shows what is wrong and where, such as `'palette' must be an object.`, `'isDark' must be true or false.` or the line and position of a JSON syntax error, and the theme stays as it was. Keys the theme does not have, such as tokens from a later version, are skipped, and missing ones take the built-in light or dark value. Copy uses the browser's clipboard API.

Every value is checked before the theme is built, since the theme writes its values into a `<style>` element:

- Palette values must be CSS colors: hex, a keyword, or a color function such as `rgb()`, `oklch()` or `var()`. Otherwise: `'palette.primary' must be a CSS color, like #ef5350 or rgb(239 83 80).`
- Font sizes and spacing values must be lengths, such as `8px` or `0.5rem`, or a `calc()`, `min()`, `max()`, `clamp()` or `var()` expression.
- Font families, weights and line heights cannot contain `;`, braces, `<`, `>` or `\`, or leave a quote, bracket or comment open.

```blazor-preview
@code {
    MokaTheme _theme = MokaTheme.Dark;
    int _exportedLength;

    void OnExported(string json) => _exportedLength = json.Length;
}

<div style="display:flex;flex-direction:column;gap:8px;width:100%">
    <MokaThemeImportExport Theme="_theme" OnImport="@(imported => _theme = imported)" OnExport="OnExported" />
    <MokaCaption>@(_exportedLength == 0 ? "Click Export to raise OnExport." : $"OnExport received {_exportedLength} characters of JSON.")</MokaCaption>
</div>
```

## MokaThemeSerializer

Static conversions, with no UI:

```csharp
using Moka.Red.ThemeGen.Serialization;

string json = MokaThemeSerializer.ToJson(theme);
MokaTheme? restored = MokaThemeSerializer.FromJson(json); // null when the text is not a theme
string css = MokaThemeSerializer.ToCss(theme);            // :root { --moka-color-primary: ...; }
string code = MokaThemeSerializer.ToCSharp(theme);        // new MokaTheme { ... }

if (!MokaThemeSerializer.TryFromJson(json, out MokaTheme? imported, out string? error))
{
    Console.WriteLine(error); // for example: 'palette' must be an object.
}
```

| Method | Returns | Description |
|--------|---------|-------------|
| `ToJson(theme)` | `string` | Indented JSON with camelCase names and every theme, palette, typography and spacing property |
| `FromJson(json)` | `MokaTheme?` | Reads JSON written by `ToJson`, as `TryFromJson` does. Returns null when the text is not a theme |
| `TryFromJson(json, out theme, out error)` | `bool` | Reads JSON written by `ToJson` and never throws. Names match in any case, and names it does not know are skipped. A value left out comes from `MokaTheme.Dark` or `MokaTheme.Light`, whichever `isDark` says. A `null`, an empty string, a value of the wrong type or a value the theme cannot use (see Import and Export above) fails, and `error` says which, ready to show to a user |
| `ToCss(theme)` | `string` | A `:root { }` block with every token `MokaTheme.ToCssVariables()` writes |
| `ToCSharp(theme)` | `string` | A `new MokaTheme { ... }` expression to paste into code, with every property set. Strings are escaped C# literals |

### What Each Format Keeps

| Part of the theme | JSON and C# | CSS |
|-------------------|-------------|-----|
| `IsDark` | Yes | No (dark shadow values are in the tokens) |
| Every palette color, the glow, border, dim and extended surface colors included | Yes | Yes |
| Typography and spacing | Yes | Yes, with `FontScale` and `Density` applied |
| `Density`, `FontScale` | Yes | As `--moka-density` and `--moka-font-scale` |

Up to 0.1.12 JSON and C# kept only the 23 core palette colors and dropped `Density` and `FontScale`, and the C# export did not escape quotes. Reading an older JSON export back fills the missing colors from the built-in theme that matches its `isDark`.

The CSS export is for pages without a `MokaThemeProvider`. A provider sets its tokens on its `.moka-root` element, and those win over `:root`.
