---
title: Pagination
description: Standalone pager with page numbers, first and last, a page size menu and "1-10 of 200", or a compact previous and next, for counted and uncounted sources. Every word replaceable.
order: 164
---

# Pagination

`MokaPagination` moves through pages of anything: a list, a grid of cards, search results. It works on its own and doesn't need a table. `MokaTable` draws it as its numbered and compact pagers; for the table's other pagers see [Pagers](/components/table#pagers). It lives in `Moka.Red.Data.Pagination`.

```blazor-preview
@code {
    int _page = 1;
    int _size = 10;
}

<MokaPagination TotalItems="245" @bind-CurrentPage="_page" @bind-PageSize="_size" />
<MokaCaption>Page @_page, @_size a page</MokaCaption>
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `TotalItems` | `int` | `0` | How many items there are across every page, or the items known so far while `HasMore` is set |
| `HasMore` | `bool?` | -- | For a source that doesn't count its items: whether there is a page after the last one counted. Null means `TotalItems` is the whole count. See [Sources that don't count](#sources-that-dont-count) |
| `CurrentPage` | `int` | `1` | The page shown, from 1. Two-way bindable |
| `CurrentPageChanged` | `EventCallback<int>` | -- | Raised when someone moves to another page, and with 1 after a new page size |
| `PageSize` | `int` | `10` | Items per page. Two-way bindable. A parent that passes it and handles `PageSizeChanged` owns it |
| `PageSizeChanged` | `EventCallback<int>` | -- | Raised when someone picks a page size |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 25, 50, 100]` | The sizes in the page size menu. Include `PageSize`, or the menu shows the wrong size |
| `ShowPageSizeSelector` | `bool` | `true` | Shows the page size menu |
| `ShowFirstLast` | `bool` | `true` | Shows the first and last page buttons |
| `ShowPageInfo` | `bool` | `true` | Shows "1-10 of 245" |
| `MaxVisiblePages` | `int` | `5` | How many page numbers show at most. The first and last pages stay reachable either side of an ellipsis |
| `Compact` | `bool` | `false` | Only previous, "3 / 25" and next |
| `Sticky` | `bool` | `false` | Keeps the pager in view at its edge while its scroll container scrolls. The container is yours: put the pager inside the element that scrolls, or let the page scroll |
| `StickyPosition` | `MokaPaginationStickyPosition` | `Bottom` | `Bottom` or `Top`, the edge `Sticky` pins it to |
| `Text` | `MokaPaginationText?` | -- | Every word the pager writes. Null is `MokaPaginationText.Default`, in English |

`MokaPagination` inherits from `MokaComponentBase`: `Class`, `Style`, `Id` and extra HTML attributes go on its root, a navigation region named by `Text.Label`.

`CurrentPage` and `PageSize` are owned values. A parent that passes one and handles its change callback owns it: a page or size it doesn't take back, by passing the new one, goes back on screen once its handler has run, so the handler can refuse it. `@bind-CurrentPage` and `@bind-PageSize` take every pick. A parent that passes the value without listening, or listens without passing it, leaves it to the pager, which follows a new value the parent passes, so a parent that renders again without binding doesn't send the pager back to the page it started on. A new page size goes back to page 1.

Every button is `type="button"` and named. The page shown carries `aria-current="page"`. The arrows show their names ("First page", "Previous page", "Next page", "Last page") in a `MokaTooltip` above the pager, or below it when it is pinned to the top, and keep it while greyed out. The first and last page buttons and the ellipses keep their place while hidden, so the bar keeps its width as the visible pages move.

## Compact

`Compact` is previous, the page and the page count, and next, with no page size menu: for a narrow place, or the top of a list that has the full pager at its foot.

```blazor-preview
<MokaPagination TotalItems="245" PageSize="10" Compact />
```

## Sources that don't count

A source that pages without counting, such as a log read a page at a time, knows the items so far and whether there is more. Pass the items seen as `TotalItems` and whether a next page exists as `HasMore`. While `HasMore` is set the pager offers the next page while it is true, draws no last page or last page button, and says "21-30 of more" in place of a total. The compact pager says "Page 3". Once `HasMore` is false the pager stops at the page shown and gives the total it knows.

```razor
<MokaPagination TotalItems="_seen" HasMore="_hasMore" PageSize="20"
                CurrentPage="_page" CurrentPageChanged="LoadPageAsync" />

@code {
    int _page = 1;
    int _seen;
    bool _hasMore;

    async Task LoadPageAsync(int page)
    {
        IReadOnlyList<LogLine> lines = await Log.ReadAsync(skip: (page - 1) * 20, take: 20);
        _page = page;
        _seen = (page - 1) * 20 + lines.Count;
        _hasMore = lines.Count == 20;
    }
}
```

`MokaTable` does this by itself for a `ServerData` source that leaves `TotalItems` null; see [Sources that don't count](/components/table#sources-that-dont-count).

## Words

`Text` takes a `MokaPaginationText`. Replace any word with `with`. Phrases built from numbers are functions: `Range` writes "1-10 of 245" from the first item, the last and the total, and `PageOfPages` writes "3 / 25".

```razor
<MokaPagination TotalItems="245"
                Text="@(MokaPaginationText.Default with
                {
                    Label = "Seiten",
                    NextPage = "Nächste Seite",
                    PreviousPage = "Vorige Seite",
                    RowsPerPage = "Zeilen pro Seite",
                    Range = (first, last, total) => $"{first} bis {last} von {total}"
                })" />
```

| Property | Default | Where |
|----------|---------|-------|
| `Label` | `"Pagination"` | The navigation region's name |
| `FirstPage`, `PreviousPage`, `NextPage`, `LastPage` | `"First page"` and the rest | The arrows' names and tooltips |
| `RowsPerPage` | `"Rows per page"` | The page size menu's name |
| `Range` | `"1-10 of 245"` | The item range |
| `PageOfPages` | `"3 / 25"` | The compact pager's page |
| `RangeOpen` | `"21-30 of more"` | The item range while `HasMore` is true |
| `PageOpen` | `"Page 3"` | The compact pager's page while the count is open |
| `Show`, `EntriesWord`, `ShowingEntries`, `Previous`, `Next` | `"Show"`, `"entries"`, `"Showing 1 to 10 of 245 entries"`, `"Previous"`, `"Next"` | The table's entries pager |
| `ShowingEntriesOpen` | `"Showing 21 to 30 of more entries"` | The table's entries pager for a source that doesn't count |
| `PageWord`, `OfPages`, `GoToPage` | `"Page"`, `"of 25"`, `"Go to page"` | The table's jump pager |
| `OfMorePages` | `"of more"` | The table's jump pager for a source that doesn't count |
| `Newer`, `Older` | `"Newer"`, `"Older"` | The table's cursor pager |

A table's pager words are `MokaTableText.Pager`, so one `MokaTableText` covers the table and its pager.
