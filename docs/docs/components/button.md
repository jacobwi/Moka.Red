---
title: Button
description: Accessible, performant button with variants, colors, sizes, icons, and loading state.
order: 1
---

# Button

`MokaButton` is the primary interactive element. It renders as a `<button>` element by default and automatically switches to `<a>` when an `Href` is provided. All visual states are expressed via CSS modifier classes.

`MokaButtonGroup` connects multiple buttons with shared border radius and no gaps.

## Parameters

### MokaButton

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | Label content |
| `Variant` | `MokaVariant` | `Filled` | `Filled`, `Outlined`, `Text`, `Soft` |
| `Color` | `MokaColor?` | `Primary` | `Primary`, `Secondary`, `Error`, `Warning`, `Success`, `Info`, `Surface` |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `StartIcon` | `MokaIconDefinition?` | - | Icon displayed before the label |
| `EndIcon` | `MokaIconDefinition?` | - | Icon displayed after the label |
| `Loading` | `bool` | `false` | Shows spinner and blocks interaction |
| `Disabled` | `bool` | `false` | Disables the button |
| `FullWidth` | `bool` | `false` | Stretches the button to fill its container |
| `Quiet` | `bool` | `false` | The quiet look for an icon-only button. `Variant` is ignored and `Size` changes only the icon |
| `Look` | `MokaButtonLook?` | `null` | How this button is drawn, ahead of the theme's `Buttons`. `null` follows the theme. New design only, see [Looks](#looks) |
| `Href` | `string?` | - | Renders as `<a>` when set. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the button stays a `<button>` |
| `Target` | `string?` | - | Link target (e.g., `_blank`) |
| `Type` | `string` | `"button"` | HTML button type attribute |
| `OnClick` | `EventCallback<MouseEventArgs>` | - | Click callback |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

### MokaButtonGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | `MokaButton` elements |
| `Orientation` | `MokaDirection` | `Row` | `Row` or `Column` layout |

## Variants

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Variant="MokaVariant.Filled">Filled</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Outlined</MokaButton>
    <MokaButton Variant="MokaVariant.Text">Text</MokaButton>
    <MokaButton Variant="MokaVariant.Soft">Soft</MokaButton>
</div>
```

## Colors

```blazor-preview
<div style="display:flex;gap:8px;flex-wrap:wrap">
    <MokaButton Color="MokaColor.Primary">Primary</MokaButton>
    <MokaButton Color="MokaColor.Secondary">Secondary</MokaButton>
    <MokaButton Color="MokaColor.Success">Success</MokaButton>
    <MokaButton Color="MokaColor.Warning">Warning</MokaButton>
    <MokaButton Color="MokaColor.Error">Error</MokaButton>
    <MokaButton Color="MokaColor.Info">Info</MokaButton>
</div>
```

## Sizes

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaButton Size="MokaSize.Xs">Xs</MokaButton>
    <MokaButton Size="MokaSize.Sm">Sm</MokaButton>
    <MokaButton Size="MokaSize.Md">Md</MokaButton>
    <MokaButton Size="MokaSize.Lg">Lg</MokaButton>
</div>
```

## Icons

Use `StartIcon` and `EndIcon` to place icons adjacent to the label. Omit `ChildContent` for an icon-only button.

```blazor-preview
<div style="display:flex;gap:8px;align-items:center">
    <MokaButton StartIcon="MokaIcons.Action.Save">Save</MokaButton>
    <MokaButton EndIcon="MokaIcons.Navigation.ArrowRight">Next</MokaButton>
    <MokaButton StartIcon="MokaIcons.Action.Add" Variant="MokaVariant.Outlined" />
    <MokaButton StartIcon="MokaIcons.Action.Delete" Color="MokaColor.Error" Variant="MokaVariant.Soft" />
</div>
```

## Quiet

`Quiet` is for an icon-only button in a row or a list: a fixed square with no border or fill and a faint icon that brightens on hover. With `Color="MokaColor.Error"` the hover turns red.

It has no text, so whoever uses it has to name it: give it an `aria-label` and put it in a `MokaTooltip` that shows the same name.

```razor
<MokaTooltip Text="Rename">
    <MokaButton Quiet StartIcon="MokaIcons.Action.Edit" aria-label="Rename" />
</MokaTooltip>
<MokaTooltip Text="Delete">
    <MokaButton Quiet Color="MokaColor.Error" StartIcon="MokaIcons.Action.Delete" aria-label="Delete" />
</MokaTooltip>
```

## Looks

On the new design (`Red`, `Gate`, `Crisp` and `Matrix`) buttons have seven looks besides `Standard`. They work with any
colors, corners, density, text size and letter case.

| Look | What changes |
|------|--------------|
| `Raised` | A light top edge and a faint shadow, pressed in on click. Secondary buttons get a soft top to bottom shade |
| `Soft` | Secondary and outlined buttons are filled grey with no border. A colored filled button (Delete in red) turns into its soft tint |
| `Pill` | Fully round ends whatever the corners setting, with a little more room each side. A row in a `MokaButtonGroup` becomes one pill |
| `Glow` | Everything outlined. The main button is an accent outline and glows on hover, the colored ones glow in their own color. With uppercase letter case it is the original Moka button |
| `Offset` | A hard outline in the text color and a solid offset shadow that the button lifts from on hover and presses into on click |
| `Gradient` | The main button and the colored filled buttons run from their color to a warmer shade of it. The rest stays flat |
| `Ghost` | Secondary, outlined and colored filled buttons show no border or fill until hovered. The main button stays filled |

The main button keeps the theme's `WithMainButton` fill under every look but `Glow`, which outlines it, and `Gradient`, which
runs from that fill. Text and `Quiet` buttons stay as they are, except that `Pill` rounds them too. `Raised` and `Offset` don't
move for people who ask for reduced motion.

Set one for the whole app on the theme:

```razor
<MokaThemeProvider Theme="MokaTheme.Red.WithButtons(MokaButtonLook.Raised)">
    @Body
</MokaThemeProvider>
```

`MokaTheme.WithComponentStyle` sets it along with the toasts, notifications, popups, lists and dialogs: `Soft` gives soft
buttons, and `Terminal` glowing ones.

Or on one button, with `Look`, which wins over the theme. `MokaButtonLook.Standard` keeps a button standard in an app that
picked another look:

```razor
<MokaButton Look="MokaButtonLook.Pill">Get started</MokaButton>
<MokaButton Look="MokaButtonLook.Standard" Variant="MokaVariant.Outlined">Cancel</MokaButton>
```

`MokaTheme.Dark` and `MokaTheme.Light` are the original design and ignore both: `WithButtons` does nothing on them, and a
button's `Look` changes nothing under them.

## Loading State

While `Loading` is true the button shows an inline spinner and ignores clicks.

```blazor-preview
@code {
    bool _saving;
    async Task Save() {
        _saving = true;
        await Task.Delay(2000);
        _saving = false;
    }
}
<MokaButton Loading="_saving" OnClick="Save" StartIcon="MokaIcons.Action.Save">
    @(_saving ? "Saving…" : "Save")
</MokaButton>
```

## Disabled

```blazor-preview
<div style="display:flex;gap:8px">
    <MokaButton Disabled>Disabled Filled</MokaButton>
    <MokaButton Disabled Variant="MokaVariant.Outlined">Disabled Outlined</MokaButton>
</div>
```

## Full Width

```blazor-preview
<MokaButton FullWidth>Full Width Button</MokaButton>
```

## In a Row

A button keeps its whole label in a flex row: the text beside it wraps first. When a row is too narrow for its buttons, give
it `flex-wrap: wrap`, as `MokaDialog`'s actions and `MokaWizard`'s buttons have. A button given a fixed width ends its label
with an ellipsis.

```blazor-preview
<div style="display: flex; align-items: center; gap: 8px; width: 320px;">
    <span>localhost:5080, server 0.1.0</span>
    <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Open a vault</MokaButton>
    <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Change server</MokaButton>
</div>
```

## Link Button

When `Href` is set the component renders as an `<a>` element while keeping button styling.

```blazor-preview
<MokaButton Href="https://github.com" Target="_blank" EndIcon="MokaIcons.Content.Link">
    Open GitHub
</MokaButton>
```

## ButtonGroup

```blazor-preview
<MokaButtonGroup>
    <MokaButton Variant="MokaVariant.Outlined" StartIcon="MokaIcons.Navigation.ChevronLeft" />
    <MokaButton Variant="MokaVariant.Outlined">Page 1</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronRight" />
</MokaButtonGroup>
```

```blazor-preview
<MokaButtonGroup Orientation="MokaDirection.Column">
    <MokaButton Variant="MokaVariant.Outlined">Top</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Middle</MokaButton>
    <MokaButton Variant="MokaVariant.Outlined">Bottom</MokaButton>
</MokaButtonGroup>
```
