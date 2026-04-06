---
title: Infinite Scroll
description: Scroll-to-load-more sentinel that triggers data fetching at the bottom of a list.
order: 52
---

# Infinite Scroll

`MokaInfiniteScroll` places an invisible sentinel element at the bottom of its content. When the sentinel scrolls into view, `OnLoadMore` fires so you can fetch the next page of data.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | The scrollable list content |
| `OnLoadMore` | `EventCallback` | -- | Fires when the sentinel becomes visible |
| `Loading` | `bool` | `false` | Whether a load is currently in progress |
| `HasMore` | `bool` | `true` | Set to `false` when all items have been loaded |
| `Threshold` | `string` | `"200px"` | Intersection observer root margin (how early to trigger) |
| `LoadingTemplate` | `RenderFragment?` | `null` | Custom template shown while loading |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic Item List

```blazor-preview
<div style="height: 200px; overflow-y: auto;">
    <MokaInfiniteScroll OnLoadMore="LoadMore" Loading="_loading" HasMore="_hasMore">
        @foreach (var item in _items)
        {
            <MokaText Style="padding: var(--moka-spacing-xs) var(--moka-spacing-sm);">@item</MokaText>
        }
    </MokaInfiniteScroll>
</div>

@code {
    private List<string> _items = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToList();
    private bool _loading;
    private bool _hasMore = true;

    private async Task LoadMore()
    {
        _loading = true;
        await Task.Delay(500);
        var next = _items.Count;
        _items.AddRange(Enumerable.Range(next + 1, 10).Select(i => $"Item {i}"));
        _hasMore = _items.Count < 50;
        _loading = false;
    }
}
```

## Custom Loading Template

```blazor-preview
<div style="height: 200px; overflow-y: auto;">
    <MokaInfiniteScroll OnLoadMore="LoadMore2" Loading="_loading2" HasMore="_hasMore2">
        <ChildContent>
            @foreach (var item in _items2)
            {
                <MokaText Style="padding: var(--moka-spacing-xs) var(--moka-spacing-sm);">@item</MokaText>
            }
        </ChildContent>
        <LoadingTemplate>
            <MokaFlexbox Justify="MokaJustify.Center" Padding="MokaSpacingScale.Md">
                <MokaSpinner Size="MokaSize.Sm" />
                <MokaText Size="MokaSize.Sm" Style="margin-left: var(--moka-spacing-xs);">Fetching more...</MokaText>
            </MokaFlexbox>
        </LoadingTemplate>
    </MokaInfiniteScroll>
</div>

@code {
    private List<string> _items2 = Enumerable.Range(1, 10).Select(i => $"Row {i}").ToList();
    private bool _loading2;
    private bool _hasMore2 = true;

    private async Task LoadMore2()
    {
        _loading2 = true;
        await Task.Delay(800);
        var next = _items2.Count;
        _items2.AddRange(Enumerable.Range(next + 1, 10).Select(i => $"Row {i}"));
        _hasMore2 = _items2.Count < 40;
        _loading2 = false;
    }
}
```

## No More Items

```blazor-preview
<MokaInfiniteScroll HasMore="false">
    <MokaText Style="padding: var(--moka-spacing-sm);">All items loaded. Nothing more to fetch.</MokaText>
</MokaInfiniteScroll>
```
