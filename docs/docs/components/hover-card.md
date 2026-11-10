---
title: Hover Card
description: Rich content card that appears on hover over a trigger element.
order: 58
---

# Hover Card

`MokaHoverCard` displays a floating card with rich content when the user hovers over a trigger element. Ideal for user profile previews, link previews, and contextual information.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | The trigger element that activates the hover card |
| `CardContent` | `RenderFragment` | **required** | Content rendered inside the floating card |
| `Position` | `MokaPopoverPosition` | `Bottom` | Preferred position of the card |
| `Delay` | `int` | `300` | Delay in milliseconds before the card appears |
| `MaxWidth` | `string?` | `"320px"` | Maximum width of the card |
| `Look` | `MokaPopupLook?` | `null` | How the card is drawn. Null takes the theme's popup look. See [Looks](#looks) |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic User Card

```blazor-preview
<MokaHoverCard>
    <ChildContent>
        <MokaLink Href="#">@("Jane Doe")</MokaLink>
    </ChildContent>
    <CardContent>
        <div style="padding: var(--moka-spacing-md);">
            <strong>Jane Doe</strong>
            <p style="font-size: var(--moka-font-size-sm); opacity: 0.7; margin-top: var(--moka-spacing-xxs);">Senior Developer at Acme Corp</p>
            <p style="font-size: var(--moka-font-size-xs); opacity: 0.5; margin-top: var(--moka-spacing-xs);">Joined March 2024</p>
        </div>
    </CardContent>
</MokaHoverCard>
```

## With Avatar

```blazor-preview
<MokaHoverCard>
    <ChildContent>
        <MokaLink Href="#">@("View Profile")</MokaLink>
    </ChildContent>
    <CardContent>
        <div style="padding: var(--moka-spacing-md); display: flex; gap: var(--moka-spacing-sm); align-items: center;">
            <MokaAvatar Initials="JD" Size="MokaSize.Lg" />
            <div>
                <strong>Jane Doe</strong>
                <p style="font-size: var(--moka-font-size-sm); opacity: 0.7;">jane.doe@example.com</p>
            </div>
        </div>
    </CardContent>
</MokaHoverCard>
```

## Custom Delay

```blazor-preview
<MokaHoverCard Delay="600">
    <ChildContent>
        <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm">Hover (600ms delay)</MokaButton>
    </ChildContent>
    <CardContent>
        <div style="padding: var(--moka-spacing-md);">
            <p style="font-size: var(--moka-font-size-sm);">This card has a longer delay before appearing.</p>
        </div>
    </CardContent>
</MokaHoverCard>
```

## Above the page

The open card sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the trigger as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.

## Looks

The card follows the theme's popup look, `MokaTheme.WithPopups(MokaPopupLook)`, like every other floating panel. [Popover](/components/popover#looks) lists the seven looks.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithPopups(MokaPopupLook.Glass)">
    @Body
</MokaThemeProvider>
```

`Look` on one hover card wins over the theme for its card:

```razor
<MokaHoverCard Look="MokaPopupLook.Contrast">
    <ChildContent><MokaLink Href="#">Ada</MokaLink></ChildContent>
    <CardContent>Ada Lovelace, first programmer.</CardContent>
</MokaHoverCard>
```

The looks are drawn only on the new design. On `MokaTheme.Dark` and `Light` the card looks the same whatever the theme or `Look` says. `Contrast` turns the card's own text light on a light page; content that sets its own colors keeps them.
