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
