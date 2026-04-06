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
| `ChildContent` | `RenderFragment?` | — | Label content |
| `Variant` | `MokaVariant` | `Filled` | `Filled`, `Outlined`, `Text`, `Soft` |
| `Color` | `MokaColor?` | `Primary` | `Primary`, `Secondary`, `Error`, `Warning`, `Success`, `Info`, `Surface` |
| `Size` | `MokaSize` | `Md` | `Xs`, `Sm`, `Md`, `Lg` |
| `StartIcon` | `MokaIconDefinition?` | — | Icon displayed before the label |
| `EndIcon` | `MokaIconDefinition?` | — | Icon displayed after the label |
| `Loading` | `bool` | `false` | Shows spinner and blocks interaction |
| `Disabled` | `bool` | `false` | Disables the button |
| `FullWidth` | `bool` | `false` | Stretches the button to fill its container |
| `Href` | `string?` | — | Renders as `<a>` when set |
| `Target` | `string?` | — | Link target (e.g., `_blank`) |
| `Type` | `string` | `"button"` | HTML button type attribute |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Click callback |
| `Class` | `string?` | — | Additional CSS classes |
| `Style` | `string?` | — | Additional inline styles |

### MokaButtonGroup

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | `MokaButton` elements |
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
