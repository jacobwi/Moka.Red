---
title: Card
description: Versatile content container with elevation, slots, collapsible body, and accent color.
order: 5
---

# Card

`MokaCard` is a surface container for grouping related content. It supports named slots for `Header`, `Footer`, `Media`, and `HeaderActions`; elevation shadows (0–4); an outlined border variant; a collapsible body; a left-edge accent color bar; and click interaction with optional navigation via `Href`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Card body content |
| `Header` | `RenderFragment?` | — | Custom header — overrides `Title`/`Subtitle` |
| `Footer` | `RenderFragment?` | — | Footer slot |
| `Media` | `RenderFragment?` | — | Media area rendered above the header |
| `HeaderActions` | `RenderFragment?` | — | Right-aligned actions in the header |
| `Title` | `string?` | — | Simple text title |
| `Subtitle` | `string?` | — | Secondary text below the title |
| `Elevation` | `int` | `1` | Box shadow depth 0–4 |
| `Outlined` | `bool` | `false` | Border instead of shadow |
| `Clickable` | `bool` | `false` | Hover effect and pointer cursor |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Click callback |
| `FullWidth` | `bool` | `false` | Fills container width |
| `Loading` | `bool` | `false` | Shows skeleton over body |
| `Collapsible` | `bool` | `false` | Header click toggles body |
| `Collapsed` | `bool` | `false` | Collapsed state (two-way bindable) |
| `CollapsedChanged` | `EventCallback<bool>` | — | Notified when collapsed state changes |
| `AccentColor` | `MokaColor?` | — | Left-edge accent bar color |
| `AccentWidth` | `string` | `"3px"` | Accent bar thickness |
| `NoPadding` | `bool` | `false` | Removes all internal padding |
| `Href` | `string?` | — | Makes the whole card a link |

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

## Collapsible

```blazor-preview
<MokaCard Title="Collapsible Section" Collapsible>
    This body can be shown or hidden by clicking the header.
</MokaCard>
```

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
