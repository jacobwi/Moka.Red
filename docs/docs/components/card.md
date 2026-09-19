---
title: Card
description: Versatile content container with elevation, slots, collapsible body, and accent color.
order: 5
---

# Card

`MokaCard` is a surface container for grouping related content. It supports named slots for `Header`, `Footer`, `Media`, and `HeaderActions`; elevation shadows (0 to 4); an outlined border variant; a collapsible body; a left-edge accent color bar; and click interaction with optional navigation via `Href`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Card body content |
| `Header` | `RenderFragment?` | -- | Custom header. Overrides `Title` and `Subtitle` |
| `Footer` | `RenderFragment?` | -- | Footer slot |
| `Media` | `RenderFragment?` | -- | Media area rendered above the header |
| `HeaderActions` | `RenderFragment?` | -- | Right-aligned actions in the header. Their clicks neither toggle a collapsible card nor click a clickable one |
| `Title` | `string?` | -- | Simple text title |
| `Subtitle` | `string?` | -- | Secondary text below the title |
| `Elevation` | `int` | `1` | Box shadow depth 0 to 4 |
| `Outlined` | `bool` | `false` | Border instead of shadow |
| `Clickable` | `bool` | `false` | Makes the card a button: hover effect, pointer cursor, tab stop, Enter and Space |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Raised when a `Clickable` card or a card with `Href` is clicked. Toggling a collapsible card does not raise it |
| `FullWidth` | `bool` | `false` | Fills container width |
| `Loading` | `bool` | `false` | Shows skeleton over body |
| `Collapsible` | `bool` | `false` | Header click toggles body. The title area is a toggle button for the keyboard |
| `Collapsed` | `bool` | `false` | Collapsed state (two-way bindable) |
| `CollapsedChanged` | `EventCallback<bool>` | -- | Notified when collapsed state changes |
| `AccentColor` | `MokaColor?` | -- | Left-edge accent bar color |
| `AccentWidth` | `string` | `"3px"` | Accent bar thickness |
| `NoPadding` | `bool` | `false` | Removes all internal padding |
| `Href` | `string?` | -- | Makes the whole card a link. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the card stays a plain card |

## Basic Card

```blazor-preview
<MokaCard Title="Project Alpha" Subtitle="Due March 2026">
    Everything is on track. Three tasks remain before the milestone.
</MokaCard>
```

## Elevation

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaCard Title="Elevation 0" Elevation="0">Flat surface</MokaCard>
    <MokaCard Title="Elevation 1" Elevation="1">Default</MokaCard>
    <MokaCard Title="Elevation 2" Elevation="2">Raised</MokaCard>
    <MokaCard Title="Elevation 3" Elevation="3">Floating</MokaCard>
    <MokaCard Title="Elevation 4" Elevation="4">Max shadow</MokaCard>
</div>
```

## Outlined

```blazor-preview
<MokaCard Title="Outlined Card" Outlined>
    Uses a border instead of a box shadow.
</MokaCard>
```

## Header and Footer Slots

```blazor-preview
<MokaCard>
    <Header>
        <div style="display:flex;align-items:center;gap:8px">
            <MokaIcon Icon="MokaIcons.Status.Info" Color="MokaColor.Info" />
            <strong>Custom Header</strong>
        </div>
    </Header>
    <ChildContent>
        Body content goes here.
    </ChildContent>
    <Footer>
        <div style="display:flex;justify-content:flex-end;gap:8px">
            <MokaButton Variant="MokaVariant.Text">Cancel</MokaButton>
            <MokaButton>Confirm</MokaButton>
        </div>
    </Footer>
</MokaCard>
```

## HeaderActions

```blazor-preview
<MokaCard Title="Card with Actions">
    <HeaderActions>
        <MokaButton Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit" Size="MokaSize.Sm">Edit</MokaButton>
        <MokaButton Variant="MokaVariant.Text" StartIcon="MokaIcons.Navigation.MoreVertical" Size="MokaSize.Sm" />
    </HeaderActions>
    <ChildContent>Card body.</ChildContent>
</MokaCard>
```

## Clickable Card

```blazor-preview
@code { string _msg = ""; }
<MokaCard Title="Click Me" Clickable OnClick="@(() => _msg = "Clicked!")">
    Hover to see the effect. @_msg
</MokaCard>
```

A clickable card is a button to the keyboard and to screen readers. It joins the tab order with the focus ring, and Enter or Space activate it, the same as a click. Keys pressed in a control inside it are left to that control, and Space doesn't scroll the page. A card with `Href` stays a link: the browser gives it focus and follows it on Enter.

A click on a control inside a clickable card also reaches the card, as it does for any clickable container. Add `@onclick:stopPropagation` to that control when it should not click the card as well; `HeaderActions` already do this. Avoid combining `Clickable` with `Collapsible`: the header toggle would sit inside the card's own button, which screen readers do not handle well.

## Collapsible

```blazor-preview
<MokaCard Title="Collapsible Section" Collapsible>
    This body can be shown or hidden by clicking the header.
</MokaCard>
```

The title area (`Title` and `Subtitle`, or your `Header`) is the toggle: a button in the tab order that reports `aria-expanded` as `"true"` or `"false"` and, while expanded, points at the body and footer with `aria-controls`. Enter or Space toggle it, the same as a click on the header. `HeaderActions` sit outside the toggle, so their buttons stay separate controls. A header with only `HeaderActions` has no title to name the toggle, so it is called "Toggle section".

A toggle from the header sticks even when `Collapsed` isn't bound, and a re-render of the parent doesn't undo it. The card follows `Collapsed` again when the parent passes a different value. Use `@bind-Collapsed` to keep both in step:

```razor
<MokaCard Title="Details" Collapsible @bind-Collapsed="_detailsCollapsed">
    ...
</MokaCard>
```

Up to 0.1.12 neither the clickable card nor the collapsible header could be reached with the keyboard.

## Accent Color

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaCard Title="Primary" AccentColor="MokaColor.Primary">Accent bar on left edge.</MokaCard>
    <MokaCard Title="Success" AccentColor="MokaColor.Success">All systems operational.</MokaCard>
    <MokaCard Title="Warning" AccentColor="MokaColor.Warning">Review required.</MokaCard>
    <MokaCard Title="Error" AccentColor="MokaColor.Error">Attention needed.</MokaCard>
</div>
```

## NoPadding

Use `NoPadding` when you want full-bleed content (e.g., an image that fills the card).

```blazor-preview
<MokaCard NoPadding style="width:280px">
    <Media>
        <img src="https://picsum.photos/280/140" alt="sample" style="display:block;width:100%" />
    </Media>
    <ChildContent>
        <div style="padding:12px">
            <strong>No Padding Card</strong>
            <p style="margin:4px 0 0">Media bleeds to the card edge.</p>
        </div>
    </ChildContent>
</MokaCard>
```
