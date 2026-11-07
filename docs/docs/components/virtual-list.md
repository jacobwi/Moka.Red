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
| `Items` | `IReadOnlyList<TItem>` | `[]` | The full dataset. Set this or `ItemsProvider`. Changes made to the list in place show up on the next render |
| `ItemsProvider` | `ItemsProviderDelegate<TItem>?` | -- | Loads the items in view on demand. Set this or `Items` |
| `Placeholder` | `RenderFragment<PlaceholderContext>?` | a skeleton line | Drawn for an item `ItemsProvider` hasn't answered for yet |
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

## Loading Items on Demand

When the list is too long to hold in memory, give it an `ItemsProvider` instead of `Items`. It's asked for only the items in view, plus the overscan, and it says how many there are in all, which sizes the scroll range.

```razor
@using Microsoft.AspNetCore.Components.Web.Virtualization

<MokaVirtualList @ref="_list" TItem="Order" ItemsProvider="LoadOrders" ItemHeight="36" Height="400px"
                 OnItemClick="Open" aria-label="Orders">
    <ItemTemplate Context="order">@order.Number</ItemTemplate>
</MokaVirtualList>

@code {
    private MokaVirtualList<Order> _list = default!;
    private string? _customer;

    private async ValueTask<ItemsProviderResult<Order>> LoadOrders(ItemsProviderRequest request)
    {
        await using AppDbContext db = await DbFactory.CreateDbContextAsync(request.CancellationToken);
        IQueryable<Order> orders = db.Orders.Where(o => _customer == null || o.Customer == _customer);

        int total = await orders.CountAsync(request.CancellationToken);
        List<Order> page = await orders
            .OrderBy(o => o.Id)
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToListAsync(request.CancellationToken);

        return new ItemsProviderResult<Order>(page, total);
    }

    private async Task FilterBy(string customer)
    {
        _customer = customer;
        await _list.RefreshDataAsync();
    }
}
```

- **Pass on the `CancellationToken`.** The list cancels a request when the person has scrolled past it, and a fast scroll would otherwise leave a queue of queries nobody will see. Let the resulting `OperationCanceledException` go, the list expects it.
- **Sort by something stable.** `Skip` and `Take` over an unordered query can return the same row twice.
- **Call `RefreshDataAsync` when the data changes**, such as a new filter. A new `ItemsProvider` isn't enough, since the list can't tell it from the one before it. The list stays where it's scrolled to.
- **On Blazor Server, use a new `DbContext` per request**, from `IDbContextFactory`. A scoped one lives as long as the circuit, and requests can overlap on it.
- **An exception goes to the nearest error boundary**, like any failure while rendering. Catch what you can recover from in the provider.

Enter and Space raise `OnItemClick` only for an item that has loaded. The active item has almost always loaded, because moving to it scrolled it into view, but while it's still a placeholder they do nothing.

Setting both `Items` and `ItemsProvider` throws, as Blazor's own `Virtualize` does.

### How long a list can be

A list is one tall scrolling element, and browsers cap how tall an element can be: about 33.5 million pixels in Chromium browsers, and about 17.8 million in Firefox. Past that the items can't be scrolled to, whatever the data source. So `ItemHeight` times the number of items has to stay under the cap, which at a 36px height is around 900,000 items in Chromium and 490,000 in Firefox. For more than that, narrow the list with a search or filter first.

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
