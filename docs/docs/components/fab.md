---
title: Floating Action Button
description: Fixed-position action button for primary or promoted actions.
order: 43
---

# Floating Action Button

`MokaFloatingActionButton` renders a prominent circular button typically fixed to a corner of the viewport. It supports icon-only and extended (icon + label) variants, configurable positioning, and all standard color and size options.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Icon` | `MokaIconDefinition` | *(required)* | The icon displayed in the button |
| `OnClick` | `EventCallback` | -- | Callback when the button is clicked. Raised on a link too |
| `Href` | `string?` | -- | Makes the button a link, drawn as the button in the same tooltip. A `javascript:`, `vbscript:` or `data:` URL is not rendered: it stays a button. See [Link](#link) |
| `Target` | `string?` | -- | Where `Href` opens: `_blank` for a new tab, or a window's name. Such a link gets `rel="noopener noreferrer"`, and `NewTabText` joins its name while the tooltip shows `Label` alone |
| `NewTabText` | `string` | `"opens in a new tab"` | What screen readers hear after the name of a link whose `Target` opens another tab, after a comma |
| `Color` | `MokaColor` | `Primary` | Button color theme |
| `Size` | `MokaSize` | `Md` | Button size: `Sm`, `Md`, `Lg` |
| `Position` | `MokaFabPosition` | `BottomRight` | Corner position: `BottomRight`, `BottomLeft`, `TopRight`, `TopLeft` |
| `Extended` | `bool` | `false` | Shows the label alongside the icon |
| `Label` | `string?` | -- | What the button does. Shown beside the icon when `Extended` is `true`; otherwise it names the button and shows in a tooltip |
| `Disabled` | `bool` | `false` | Disables the button |
| `Loading` | `bool` | `false` | Busy: a spinner in the icon's place, focus kept, presses ignored. See [Busy](#busy) |
| `LoadingText` | `string` | `"Loading"` | What a screen reader hears when the action becomes busy |
| `Class` | `string?` | -- | Additional CSS classes on the element that holds the button in its corner |
| `Style` | `string?` | -- | Additional inline styles on that element, for example to move it |

An icon-only button shows `Label` in a `MokaTooltip` on hover and on keyboard focus, also while disabled. The tooltip
opens away from the button's corner: above it for the bottom corners and below it for the top ones, lined up with the
window's edge. Since 0.1.20 the fixed element is the component's root, with the button inside it in that tooltip:
`Class`, `Style` and the margin go on the root, so a style such as `position:absolute` or `bottom:96px` still places the
button, while `Id`, the padding, the rounding and other attributes go on the button.

## Basic FAB (Bottom Right)

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" OnClick="() => { }" />
</div>
```

## Top Left Position

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Edit" Position="MokaFabPosition.TopLeft" Color="MokaColor.Secondary" />
</div>
```

## Extended with Label

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add"
                               Extended="true"
                               Label="New Item"
                               OnClick="() => { }" />
</div>
```

## Colors

```blazor-preview
<div style="position:relative;height:250px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" Position="MokaFabPosition.TopLeft" Color="MokaColor.Primary" />
    <MokaFloatingActionButton Icon="MokaIcons.Action.Edit" Position="MokaFabPosition.TopRight" Color="MokaColor.Secondary" />
    <MokaFloatingActionButton Icon="MokaIcons.Status.CheckCircle" Position="MokaFabPosition.BottomLeft" Color="MokaColor.Success" />
    <MokaFloatingActionButton Icon="MokaIcons.Status.Error" Position="MokaFabPosition.BottomRight" Color="MokaColor.Error" />
</div>
```

## Disabled

```blazor-preview
<div style="position:relative;height:200px;border:1px dashed var(--moka-color-outline-variant);border-radius:var(--moka-radius-md)">
    <MokaFloatingActionButton Icon="MokaIcons.Action.Add" Disabled="true" />
</div>
```

## Busy

`Loading` works as it does on `MokaButton`: a spinner takes the icon's place, the button carries `aria-busy="true"` and
stays focusable with `aria-disabled="true"`, `OnClick` isn't raised until it's done, and `LoadingText` is announced. An
extended button keeps its label, so it keeps its width.

```razor
<MokaFloatingActionButton Icon="MokaIcons.Action.Add" Extended Label="New item"
                          Loading="_adding" LoadingText="Adding" OnClick="AddAsync" />
```

## Link

`Href` makes the button an `<a>` with the same look, in the same tooltip, for an action that goes to a page. `OnClick`
still runs on a click. `Target="_blank"` opens it in another tab with `rel="noopener noreferrer"`. The button is named by
its `aria-label`, so ", opens in a new tab" (`NewTabText`) joins that name, extended or not, while the tooltip shows
`Label` alone. While `Disabled` or `Loading` the link has no `href`, `target` or `rel`, so it goes nowhere, and says it is
a disabled link (`role="link"` with `aria-disabled="true"`).

```razor
<MokaFloatingActionButton Icon="MokaIcons.Status.HelpCircle" Label="Help" Href="https://example.com/help" Target="_blank" />
```
