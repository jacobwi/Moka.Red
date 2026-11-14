---
title: Theme Studio
description: A theme generator laid out as a studio. Start from a preset, derive the colors from a few seeds, pick every style choice, pin any token, check contrast and export the theme as C#, JSON or CSS.
order: 163
---

# Theme Studio

`MokaThemeStudio` builds a theme with the library's own parts, in three columns that each carry a heading:

- **Theme settings**, a sidebar panel, picks where to start, the seed colors and every style choice.
- **Preview**, the stage, draws a sample app made of real components in the dark scheme, the light one or both. The previews sit on a dotted well, each captioned with its scheme, so a card in a preview never reads as one of the controls. The heading bar holds the scheme switch and the **Show what changed** switch, and stays in view while the previews scroll.
- **Inspector**, a panel like the settings, holds the theme's contrast report, every token with a pin, and its C#, JSON and CSS.

Below 75rem of the studio's own width the inspector moves under the other two, and below 48rem everything is one column.

It is in `Moka.Red.ThemeGen`, part of the `Moka.Red` meta-package, and needs no service. The sample app's toast button shows only when `AddMokaFeedback()` (or `AddMokaRed()`) registered the toast service.

```blazor-preview
@code {
    MokaThemePair _themes = MokaThemePair.From(MokaTheme.Default);
}

<div style="width:100%">
    <MokaThemeStudio @bind-Themes="_themes" View="MokaThemeStudioView.Dark" />
</div>
```

## Binding

Bind `Themes` to get both schemes at once, in the shape `MokaThemeProvider` takes them:

```razor
<MokaThemeProvider AutoDetectColorScheme DarkTheme="_themes.Dark" LightTheme="_themes.Light">
    <MokaThemeStudio @bind-Themes="_themes" />
</MokaThemeProvider>

@code {
    MokaThemePair _themes = MokaThemePair.From(MokaTheme.Default);
}
```

`MokaThemePair` is one value rather than two parameters, so each change reaches the app once and the two schemes never disagree in between. `MokaThemePair.From(theme)` makes one from either scheme of a design, and `Select(change)` makes the same change to both.

Bind `Theme` for one theme instead. `ThemeChanged` then carries the design in the scheme the bound theme was in, whatever the previews show.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Themes` | `MokaThemePair?` | `null` | Both schemes. Two-way bindable, and wins over `Theme` |
| `ThemesChanged` | `EventCallback<MokaThemePair>` | | Raised with both schemes after every change |
| `Theme` | `MokaTheme?` | `null` | One theme. Two-way bindable. Neither set starts from `MokaTheme.Default` |
| `ThemeChanged` | `EventCallback<MokaTheme>` | | Raised with the theme after every change, in the bound theme's scheme |
| `View` | `MokaThemeStudioView` | `Both` | Which schemes the middle shows: `Both`, `Dark` or `Light`. Two-way bindable |
| `ViewChanged` | `EventCallback<MokaThemeStudioView>` | | Raised when the person picks other schemes to show |
| `ShowChanges` | `bool` | `false` | Marks what each change touched in the previews and names it under the preview heading. Two-way bindable. See [Showing What Changed](#showing-what-changed) |
| `ShowChangesChanged` | `EventCallback<bool>` | | Raised when the person turns it on or off |
| `Starts` | `IReadOnlyList<MokaAppearancePalette>` | `MokaAppearancePalette.Presets` | The designs to start from |
| `Text` | `MokaThemeStudioText` | `new()` | Every word the studio writes, with `Appearance`, `Preview` and `Export` for the style rows, the sample app and the export tab |
| `Nonce` | `string?` | `null` | A content security policy nonce for the previews' theme providers |
| `Class`, `Style`, `Id` | `string?` | `null` | On the studio's root element, with any other attribute |

## Start From

The start list is the appearance panel's palette row: Red, Gate, Crisp, Matrix, Ember and the original design, each in both schemes. A theme passed in that matches none of them shows first as the current theme. Picking a start keeps the style choices and pins already made and drops the seed colors, since the start is a set of colors.

## Colors

On the new design every other color is worked out from a few seeds (`MokaTheme.WithSeed`, see [Themes from a seed](/getting-started/theming#themes-from-a-seed)): the accent, the neutral whose hue tints the surfaces, and the good, warning, error and info colors. Until a seed is touched the fields show the start's colors and the theme is the preset itself; the first change derives it from seeds. A meaning seed can be put back to the start's color.

- **Contrast** is the seed's level: AA holds text to 4.5:1, AAA to 7:1.
- **High contrast** turns on `WithHighContrast` for both schemes, on a preset or a seeded theme.
- **Neutral strength** is the seed's `NeutralChroma`, how strongly the surfaces take the neutral's hue, up to 0.1. Until it is moved it follows the neutral's own strength.

The original design keeps its own colors, so a start on it shows a line in place of the seeds.

## Style

Every style choice `MokaTheme` takes, as the appearance panel's rows in its compact form: density, text size, corners, depth, main button, text buttons, letter case, title and number fonts, page titles, selection, current place, status bar, dock layouts, tables, table rows, descriptions and label colors; the component style and every component look, tooltips included; and the chart look, colors and texture. A choice left alone keeps the start's, and the reset button puts them all back.

## The Sample App

The previews draw `MokaThemePreview`: a page header, tabs, buttons in every variant, fields, a select, a switch and a checkbox, alerts, stat cards, a table with a selected row, a bar chart, tags and labels, a raised card holding a side menu with the current project, a settings section with its icon tile and status line, a notification list drawn in place (`MokaNotificationBell` with `Inline`), a small dock layout, a dialog and toasts that open inside the preview, and a status bar whose sync item has a tooltip. Between them every style choice shows somewhere. Dialogs, toasts, list rows, popups and tooltips show once they open, and chart print only in print.

Only an app's outermost `MokaThemeProvider` writes the button looks, so the sample's own buttons take the previewed theme's `Buttons` and `DisabledButtons` through `MokaButton.Look` and `DisabledLook`.

## Showing What Changed

Turn on **Show what changed** at the preview heading, or bind `ShowChanges`. After each change from the settings, once it has drawn, the elements in the previews whose look changed get an outline in the studio's own accent that fades out, and the line under the heading says what changed and how many elements it touched: "Corners: 42 elements changed." Screen readers hear the same line, politely, through `IMokaAnnouncer` when one is registered (`AddMokaRed()` registers it).

- A slider is shown once, after it has stood still for 400 ms, not on every frame of a drag. A color picker reports when it lets go.
- A choice that changes nothing on screen says so: "Notifications: nothing in the preview uses this choice." One whose look shows only once something opens says what to open, such as the dialog or the owner list.
- A new start and an imported theme change nearly everything, so they aren't marked.
- Switching the schemes shown or the inspector's tabs isn't a change.
- With reduced motion the outline holds still for its moment rather than fading.

How it works: a collocated script reads the computed colors, backgrounds, borders, corners, shadows, padding, gaps, fonts, letter case and opacity of every element in the previews just before the change, and again after the render that applied it, with the transitions the change started finished first. An element counts when one of its own values changed, not one it only inherited, and a part drawn anew is marked once at its top. Nothing listens or runs between changes, and while the switch is off the script isn't even loaded. Reading both previews, about 760 elements, takes about 8 to 10 ms, and comparing them after a change about 16 ms, most of it the style work the browser does for the change anyway (Chromium, 2026-11-14).

## Inspector

While both schemes show, a switch over the tabs picks the scheme they inspect.

- **Contrast** lists `CheckContrast()`, the pairs that miss their target first. Each shows both colors as a sample, the two tokens, the WCAG ratio, APCA Lc and what it needs.
- **Tokens** lists every token in `MokaTokens.All` by group, with a search. Pin opens a field (a color input for a color token), checked against the token's kind, and Reset lets it go. A color pin holds in the scheme being inspected, since a color that reads on dark surfaces rarely reads on light ones. Sizes, fonts and timings hold in both.
- **Export** is [`MokaThemeImportExport`](/components/theme-editor#import-and-export) for the scheme inspected: C# written with the `With` API, JSON and CSS, each highlighted with a copy button, and a field to import a theme from JSON, which replaces the studio's.

## How It Stays Fast

- The color pickers report when a drag lets go, so dragging one builds the theme once.
- The neutral strength slider fires on every step. Its values are queued without a render (`IHandleEvent`, see `blazor/performance/rendering.md`) and applied together at most once a frame, 16 ms, from the registered `TimeProvider`.
- A click, pick or color applies a waiting slider value first, so changes keep their order, and builds the theme once.
- The previews, the style rows and the inspector's lists draw again only when what they show changes, so a tab switch or a field being typed in leaves the previews alone.
- Each preview is a `MokaThemeProvider` nested in the app's own, which writes only the tokens that differ from the app's theme.
- The contrast report and the exports are worked out only while their tab shows, once per theme.
- Showing what changed reads the previews once before a change and once after it, and nothing in between.

## Words

`MokaThemeStudioText` holds every word, with functions for the ones that hold a number or a name, such as `ContrastSummary(failing, total)`, `PinLabel(token)`, `ChangeSummary(what, count)` and `ShownLater(what, row)`. Its `Appearance`, `Preview` and `Export` hold the words of the parts it is made of.

```razor
<MokaThemeStudio @bind-Themes="_themes"
                 Text="new MokaThemeStudioText { StartHeading = "Base", Preview = new MokaThemePreviewText { Title = "Dashboard" } }" />
```
