---
title: Table
description: Feature-complete data table with sorting, filtering, pagination, selection, export, virtualization, and more.
order: 7
---

# Table

`MokaTable<TItem>` is a full-featured data table. Columns are declared as child `MokaColumn<TItem>` components. The table supports both **client-side** data (`Items`) and **server-side** data (`ServerData` callback). All features — sorting, pagination, search, filtering, selection — are handled transparently in both modes.

## Setup

```razor
<MokaTable Items="_items">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Email" Field="x => x.Email" />
    <MokaColumn Title="Role" Field="x => x.Role" />
</MokaTable>
```

## MokaTable Parameters

### Data

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IEnumerable<TItem>?` | `null` | Client-side data |
| `ServerData` | `Func<MokaTableState, Task<MokaTableResult<TItem>>>?` | `null` | Server-side data callback |
| `ChildContent` | `RenderFragment?` | `null` | `MokaColumn` definitions |
| `ItemKey` | `Func<TItem, object>?` | `null` | Row identity: the row's `@key` and its expanded state. See [Row keys](#row-keys) |

### Toolbar

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | — | Table title in toolbar |
| `Searchable` | `bool` | `false` | Shows global search input |
| `SearchPlaceholder` | `string` | `"Search..."` | Search box placeholder |
| `ToolbarContent` | `RenderFragment?` | — | Custom toolbar content |
| `ShowToolbar` | `bool` | `true` | Shows/hides the toolbar |

### Pagination

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `PageSize` | `int` | `10` | Rows per page |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10,25,50,100]` | Page size dropdown options |
| `ShowPagination` | `bool` | `true` | Pages the rows and shows the pager. `false` shows every row. See [Showing every row](#showing-every-row) |

### Selection

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Selectable` | `bool` | `false` | Row checkboxes |
| `SelectedItems` | `HashSet<TItem>?` | — | Two-way bindable selected set |
| `SingleSelect` | `bool` | `false` | Only one row at a time |
| `OnRowClick` | `EventCallback<TItem>` | — | Row click callback |

### Appearance

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Striped` | `bool` | `false` | Alternating row colors |
| `Hoverable` | `bool` | `true` | Row hover highlight |
| `Bordered` | `bool` | `false` | Cell borders |
| `Dense` | `bool` | `true` | Compact row spacing |
| `FixedHeader` | `bool` | `false` | Sticky column headers |
| `Height` | `string?` | — | Max height with scroll |
| `RowClass` | `Func<TItem, string?>?` | — | Dynamic row CSS class |
| `RowStyle` | `Func<TItem, string?>?` | — | Dynamic row inline style |

### Sorting

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `SortColumn` | `string?` | — | Active sort column title (two-way) |
| `SortDirection` | `MokaSortDirection` | `None` | Active sort direction (two-way) |
| `MultiSort` | `bool` | `false` | Shift+click to sort by multiple columns |

### Advanced Features

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Expandable` | `bool` | `false` | Expandable row detail rows |
| `DetailTemplate` | `RenderFragment<TItem>?` | — | Template for expanded row content |
| `Exportable` | `bool` | `false` | Shows export button in toolbar |
| `OnExport` | `EventCallback<MokaTableExportContext<TItem>>` | — | Export data callback |
| `FooterContent` | `RenderFragment?` | — | Custom footer row (`<tfoot>`) |
| `Responsive` | `bool` | `true` | Horizontal scroll on small screens |

## MokaColumn Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | — | Column header text |
| `Field` | `Func<TItem, object?>?` | — | Value selector for sorting and rendering |
| `CellTemplate` | `RenderFragment<TItem>?` | — | Custom cell render template |
| `HeaderTemplate` | `RenderFragment?` | — | Custom header template |
| `Sortable` | `bool` | `true` | Column is sortable |
| `Filterable` | `bool` | `false` | Per-column filter input |
| `FilterType` | `MokaColumnFilterType` | `Text` | `Text` or `Select` (distinct values) |
| `Width` | `string?` | — | CSS width (`200px`, `30%`) |
| `MinWidth` | `string?` | — | Minimum CSS width |
| `Align` | `MokaTextAlign` | `Left` | Cell text alignment |
| `Visible` | `bool` | `true` | Column visibility |
| `Sticky` | `bool` | `false` | Sticky column during horizontal scroll |
| `Resizable` | `bool` | `true` | Column resize handle |
| `Editable` | `bool` | `false` | Click-to-edit cells |
| `OnCellEdited` | `EventCallback<(TItem, object?)>` | — | Fired after cell edit |
| `Aggregate` | `MokaAggregateType` | `None` | Footer aggregate: `Sum`, `Avg`, `Min`, `Max`, `Count` |
| `Format` | `string?` | — | Value format string (`"C2"`, `"d"`) |
| `HideOnMobile` | `bool` | `false` | Hides column on small screens |

## Sorting

Click a column header to sort. Hold **Shift** and click additional headers to add multi-column sorts (requires `MultiSort="true"`).

```blazor-preview
@code {
    record Person(string Name, int Age, string City);
    IEnumerable<Person> _people = [
        new("Alice", 31, "London"),
        new("Bob", 27, "Paris"),
        new("Carol", 35, "Berlin"),
    ];
}

<MokaTable Items="_people" Title="People" MultiSort>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

## Filtering

```blazor-preview
<MokaTable Items="_people" Searchable>
    <MokaColumn Title="Name" Field="x => x.Name" Filterable />
    <MokaColumn Title="City" Field="x => x.City" Filterable FilterType="MokaColumnFilterType.Select" />
</MokaTable>
```

## Selection

```blazor-preview
@code {
    HashSet<Person> _selected = [];
}
<MokaTable Items="_people" Selectable @bind-SelectedItems="_selected">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" />
</MokaTable>
<p>Selected: @string.Join(", ", _selected.Select(p => p.Name))</p>
```

## Expandable Rows

```blazor-preview
<MokaTable Items="_people" Expandable>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="City" Field="x => x.City" />
    <DetailTemplate Context="person">
        <div style="padding:8px 16px">
            <strong>@person.Name</strong> is @person.Age years old and lives in @person.City.
        </div>
    </DetailTemplate>
</MokaTable>
```

## Custom Cell Template

```blazor-preview
<MokaTable Items="_people">
    <MokaColumn Title="Name" Field="x => x.Name">
        <CellTemplate Context="p">
            <div style="display:flex;align-items:center;gap:8px">
                <MokaAvatar Size="MokaSize.Sm">@p.Name[0]</MokaAvatar>
                @p.Name
            </div>
        </CellTemplate>
    </MokaColumn>
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
</MokaTable>
```

## Server-Side Data

```csharp
async Task<MokaTableResult<Order>> LoadOrders(MokaTableState state)
{
    var query = _db.Orders.AsQueryable();

    if (!string.IsNullOrEmpty(state.SearchTerm))
        query = query.Where(o => o.Reference.Contains(state.SearchTerm));

    if (state.SortColumn == "Amount")
        query = state.SortDirection == MokaSortDirection.Ascending
            ? query.OrderBy(o => o.Amount)
            : query.OrderByDescending(o => o.Amount);

    int total = await query.CountAsync();
    var items = await query
        .Skip((state.Page - 1) * state.PageSize)
        .Take(state.PageSize)
        .ToListAsync();

    return new MokaTableResult<Order> { Items = items, TotalItems = total };
}
```

```razor
<MokaTable ServerData="LoadOrders" Title="Orders">
    <MokaColumn Title="Reference" Field="x => x.Reference" />
    <MokaColumn Title="Amount" Field="x => x.Amount" Format="C2" Align="MokaTextAlign.Right"
                Aggregate="MokaAggregateType.Sum" AggregateFormat="C2" />
    <MokaColumn Title="Status" Field="x => x.Status" />
</MokaTable>
```

## Showing every row

`ShowPagination="false"` turns paging off. The pager is hidden, every row renders, and `PageSize` no longer limits anything. Sorting, search, column filters, select-all, keyboard navigation, row reordering and aggregates all work across the full set. Changing `ShowPagination` at runtime reloads the rows.

```razor
<MokaTable Items="_logEntries" ShowPagination="false" Virtualize Height="480px">
    <MokaColumn Title="Time" Field="x => x.Time" Format="HH:mm:ss" />
    <MokaColumn Title="Message" Field="x => x.Message" />
</MokaTable>
```

For long lists, add `Virtualize` and a fixed `Height` so only the rows in view are rendered.

With `ServerData`, the table asks for every row: `Page` is 1 and `PageSize` covers the whole result set, so a `Skip`/`Take` source like the one above works unchanged. The table sizes that request from the last `TotalItems` it saw, so the first load can take two calls: one that learns the total and one that fetches that many rows. A source that caps its page size returns only part of the set, and the table shows what it got. Keep the pager for sources like that.

## Row keys

`ItemKey` gives each row an identity. The table uses it as the row's `@key`, so when sorting, filtering or a reload moves a row, Blazor moves the row's DOM and component state with it instead of rebuilding it. It also records which rows are expanded. Without `ItemKey` the item itself is the key, and records compare by value.

```razor
<MokaTable Items="_orders" ItemKey="o => o.Id" Expandable>
    <MokaColumn Title="Reference" Field="x => x.Reference" />
    <MokaColumn Title="Status" Field="x => x.Status" />
</MokaTable>
```

Keys should be unique. A key that more than one row returns cannot tell those rows apart, so the table renders them without a key instead of letting Blazor throw its duplicate-key error. Blazor then matches those rows by position, as it does for a table without `ItemKey`, and every other row keeps its key. Rows that share a key also share their expanded state: expanding one expands them all.

## Export

```razor
<MokaTable Items="_people" Exportable OnExport="HandleExport">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" />
</MokaTable>

@code {
    void HandleExport(MokaTableExportContext<Person> ctx)
    {
        // ctx.Items — all items (not just current page)
        // ctx.Columns — column definitions with titles
        var csv = string.Join("\n",
            ctx.Items.Select(p => $"{p.Name},{p.Age}"));
        // trigger download...
    }
}
```
