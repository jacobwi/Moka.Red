---
title: Virtual List
description: Virtualized list that renders only the rows in view, with keyboard support for clickable items.
order: 92
---

# Virtual List

`MokaVirtualList<TItem>` renders a long list through Blazor's `Virtualize`, so only the items in view, plus a few either side, are in the page. Every item has the same fixed height, which the list needs to size its scroll range.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<TItem>` | `[]` | The full dataset. Required. Changes made to the list in place show up on the next render |
| `ItemTemplate` | `RenderFragment<TItem>` | -- | Template for one item. Required |
| `ItemHeight` | `float` | -- | Height of every item in pixels. Required |
| `Height` | `string` | `"400px"` | Height of the scrolling list |
| `OverscanCount` | `int` | `3` | Extra items rendered above and below the ones in view |
| `OnItemClick` | `EventCallback<TItem>` | -- | Raised for a click on an item, or Enter or Space on the active one. Makes the list a listbox the keyboard can reach |
| `Disabled` | `bool` | `false` | Dims the list and ignores clicks and keys |

## Basic Usage

```blazor-preview
<MokaVirtualList Items="_files" ItemHeight="32" Height="240px" TItem="string">
    <ItemTemplate Context="file">@file</ItemTemplate>
</MokaVirtualList>

@code {
    private readonly IReadOnlyList<string> _files =
        Enumerable.Range(1, 10000).Select(i => $"file-{i:D5}.cs").ToList();
}
```

## Clickable Items

```razor
<MokaVirtualList Items="_files" ItemHeight="32" Height="240px" TItem="string"
                 OnItemClick="Open" aria-label="Files">
    <ItemTemplate Context="file">@file</ItemTemplate>
</MokaVirtualList>
```

## Keyboard

With `OnItemClick` the list is a single tab stop. Focus stays on the list itself while the arrow keys move an active item, which `Virtualize` can drop from the page and bring back without focus getting lost. A tab stop per item would make Tab walk every item before it could leave the list.

| Key | Action |
|-----|--------|
| Tab | Move into the list. The first item in view becomes active |
| Down / Up | Move to the next or previous item. They stop at the ends |
| Page Down / Page Up | Move by the number of items that fit in view |
| Home / End | Move to the first or last item, scrolling as far as needed |
| Enter / Space | Raise `OnItemClick` for the active item |

The active item scrolls into view as it moves. A click on an item makes it the active one, so the keys carry on from there. Keys pressed on content inside an item are left alone.

## Accessibility

- With `OnItemClick` the list has `role="listbox"`, and each item has `role="option"`. The list points `aria-activedescendant` at the active item, which has `aria-selected="true"`.
- Only the items in view are in the page, so each one carries `aria-setsize` and `aria-posinset`, and a screen reader can say "3 of 10000".
- Name the list with an `aria-label` attribute, as in the example above.
- Keep buttons and links out of `ItemTemplate` in a clickable list. The content of a listbox option is read as plain text, so a control inside it cannot be reached.
- Without `OnItemClick` the list takes no focus and has no role.

Up to 0.1.12 a clickable list could only be used with the mouse, and a fractional `ItemHeight` became invalid CSS on a comma-decimal culture.
