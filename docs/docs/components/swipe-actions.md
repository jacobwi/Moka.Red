---
title: Swipe Actions
description: Swipe-to-reveal actions on list items for touch and pointer interactions.
order: 50
---

# Swipe Actions

`MokaSwipeActions` wraps content and reveals action buttons when the user swipes (or drags) left or right. Commonly used in list UIs for delete, archive, or quick-action patterns.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | The main content that can be swiped |
| `LeftActions` | `RenderFragment?` | `null` | Actions revealed when swiping right |
| `RightActions` | `RenderFragment?` | `null` | Actions revealed when swiping left |
| `Threshold` | `string` | `"80px"` | Minimum swipe distance to reveal actions |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Swipe Left to Delete

```blazor-preview
<MokaSwipeActions>
    <RightActions>
        <MokaButton Color="MokaColor.Error" Variant="MokaVariant.Filled">Delete</MokaButton>
    </RightActions>
    <ChildContent>
        <MokaCard Outlined Style="padding: var(--moka-spacing-md);">
            <MokaText>Swipe me left to reveal delete</MokaText>
        </MokaCard>
    </ChildContent>
</MokaSwipeActions>
```

## Swipe Right to Archive

```blazor-preview
<MokaSwipeActions>
    <LeftActions>
        <MokaButton Color="MokaColor.Success" Variant="MokaVariant.Filled">Archive</MokaButton>
    </LeftActions>
    <ChildContent>
        <MokaCard Outlined Style="padding: var(--moka-spacing-md);">
            <MokaText>Swipe me right to archive</MokaText>
        </MokaCard>
    </ChildContent>
</MokaSwipeActions>
```

## Both Sides

```blazor-preview
<MokaSwipeActions Threshold="60px">
    <LeftActions>
        <MokaButton Color="MokaColor.Success" Variant="MokaVariant.Filled">Archive</MokaButton>
    </LeftActions>
    <RightActions>
        <MokaButton Color="MokaColor.Error" Variant="MokaVariant.Filled">Delete</MokaButton>
    </RightActions>
    <ChildContent>
        <MokaCard Outlined Style="padding: var(--moka-spacing-md);">
            <MokaText>Swipe left or right for actions</MokaText>
        </MokaCard>
    </ChildContent>
</MokaSwipeActions>
```
