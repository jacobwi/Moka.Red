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
| `Items` | `IEnumerable<TItem>?` | — | Client-side data |
| `ServerData` | `Func<MokaTableState, Task<MokaTableResult<TItem>>>?` | — | Server-side data callback |
| `ChildContent` | `RenderFragment?` | — | `MokaColumn` definitions |
| `ItemKey` | `Func<TItem, object>?` | — | Key selector for `@key` optimization |

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
| `ShowPagination` | `bool` | `true` | Shows pagination bar |

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

    return new MokaTableResult<Order>(items, total);
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
