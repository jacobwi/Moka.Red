---
title: Card
description: Versatile content container with elevation, slots, collapsible body, and accent color.
order: 5
---

# Card

`MokaCard` is a surface container for grouping related content. It supports named slots for `Header`, `Footer`, `Media`, and `HeaderActions`; elevation shadows (0 to 4); an outlined border variant; a collapsible body; a left-edge accent color bar; click interaction with optional navigation via `Href`; and filling its container with a scrolling body.

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
| `Clickable` | `bool` | `false` | A click anywhere on the card raises `OnClick`. The card's one tab stop is a real button: the title, or a hidden button named by `ActionLabel`. See [Clickable Card](#clickable-card) |
| `ActionLabel` | `string?` | -- | Names the card's action when the title isn't it: no `Title` shown, or a collapsible card. Defaults to `Title` |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Raised when a `Clickable` card or a card with `Href` is clicked. Toggling a collapsible card does not raise it, and neither does a control inside the card |
| `FullWidth` | `bool` | `false` | Fills container width |
| `Fill` | `bool` | `false` | Fills its container's height (a grid cell or a flex column), and the body scrolls inside the card. See [Filling a Container](#filling-a-container) |
| `Loading` | `bool` | `false` | Shows skeleton over body |
| `Collapsible` | `bool` | `false` | Header click toggles body. The title area is a toggle button for the keyboard |
| `Collapsed` | `bool` | `false` | Collapsed state (two-way bindable) |
| `CollapsedChanged` | `EventCallback<bool>` | -- | Notified when collapsed state changes |
| `AccentColor` | `MokaColor?` | -- | Left-edge accent bar color |
| `AccentWidth` | `string` | `"3px"` | Accent bar thickness |
| `NoPadding` | `bool` | `false` | Removes all internal padding |
| `Href` | `string?` | -- | A click anywhere on the card follows it. The link is the title, or a hidden link named by `ActionLabel`. A `javascript:`, `vbscript:` or `data:` URL is not rendered: the card stays a plain card |
| `Inner` | `bool` | `false` | A card inside a card: the panel color, no line or shadow, a smaller corner, and its own padding around the header, body and footer. With `Outlined` it gets a 1px outline |
| `Live` | `bool` | `false` | A green edge on an `Inner` card, for something that is running. Does nothing without `Inner` |
| `Dashed` | `bool` | `false` | A dashed edge on an `Inner` card, for something waiting to be filled. Does nothing without `Inner` |

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

The card itself is a plain element. Its action is a real `<button type="button">` (or an `<a href>` with `Href`), and that action is the title when the card shows one. The action stretches over the whole card, so a click anywhere on it runs `OnClick` or follows the link. The action is the card's one tab stop: Tab reaches it, the card shows its focus ring, and Enter or Space press the button the browser's own way (a link opens on Enter).

Controls inside the card are not inside the action. Links, buttons, inputs, labels and anything with a `tabindex` sit above it, so a switch or a link in a clickable card works on its own and doesn't click the card. Plain text, icons and pictures are under it, and a click on them runs the card's action.

```blazor-preview
@code { string _msg = ""; bool _on = true; }
<MokaCard Title="Backups" Clickable OnClick="@(() => _msg = "Opened")">
    <MokaSwitch @bind-Value="_on" Label="Nightly" />
    <p>@_msg</p>
</MokaCard>
```

With no `Title` shown (a custom `Header`, or no header at all) the action is a visually hidden button. Name it with `ActionLabel`. Without one it takes `Title`, and with neither it's named by the card's header and body.

```razor
<MokaCard Clickable ActionLabel="Open the backups" OnClick="OpenBackups">
    <Header><strong>Backups</strong> <MokaStatusDot Label="Running" /></Header>
    <ChildContent>Last run at 02:00</ChildContent>
</MokaCard>
```

A `target`, `rel` or `download` attribute on a card with `Href` goes on the link, and an `aria-label` names the action. Other attributes stay on the card.

A clickable card can be collapsible. Its title is then the collapse toggle, so the action is a hidden button named by `ActionLabel` or `Title`. A click on the header toggles the card, and a click anywhere else runs the action.

Up to 0.3.10 the whole card was the button (`role="button"` and a tab stop on the card, or the whole card inside an `<a>`), so switches and links inside it were nested in a button or a link, and their clicks also clicked the card.

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

## Filling a Container

`Fill` makes the card take the height of its container, a grid cell or a flex column, and only the body scrolls. The media, header and footer stay put. The card needs a container with a height: a grid row of fixed height, or a flex column that has one.

```razor
<div style="display:grid;grid-template-columns:repeat(2, 1fr);grid-template-rows:320px;gap:16px">
    <MokaCard Title="Log" Fill>
        @foreach (string line in _lines)
        {
            <div>@line</div>
        }
    </MokaCard>
    <MokaCard Title="Status" Fill>All good.</MokaCard>
</div>
```

On a card that is also clickable or a link, the scrolling body sits above the card's action, so the wheel and touch scroll it. A click in the body is then the body's own, and the header, footer and media still run the action.

## Inner Cards

`Inner` is for a card inside a card, such as a peer or a device tile. The inner card holds the padding and the gap, so its header, body and footer sit tight inside it with no lines between them. `Outlined`, `Live` and `Dashed` change only its edge.

```razor
<MokaCard Title="Peers">
    <MokaGrid MinColumn="200px" GapValue="var(--moka-spacing-lg)">
        <MokaCard Inner Title="Plain">A panel on the card.</MokaCard>
        <MokaCard Inner Outlined Title="Outlined">The same with an edge.</MokaCard>
        <MokaCard Inner Live Title="Live">Something is running.</MokaCard>
        <MokaCard Inner Dashed Title="Dashed">Waiting to be filled.</MokaCard>
    </MokaGrid>
</MokaCard>
```
