---
title: Table
description: Data table over client-side or server-side rows, with sorting, search and filters, paging, selection, inline editing, reordering, export and virtualization.
order: 7
---

# Table

`MokaTable<TItem>` shows rows from `Items`, a collection in memory, or from a `ServerData` callback that loads one page at a time. Columns are child `MokaColumn<TItem>` components. Sorting, paging, search, filters and selection work in both modes: with `Items` the table does the work itself, and with `ServerData` it passes the state to your callback.

## Setup

```razor
<MokaTable Items="_items">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Email" Field="x => x.Email" />
    <MokaColumn Title="Role" Field="x => x.Role" />
</MokaTable>
```

Both components live in `Moka.Red.Data.Table`. `TItem` is inferred from `Items`. Set it yourself, as in `TItem="Order"`, when you pass a method group to a callback such as `OnRowClick`, because the type can't be inferred from a method group.

Once you set a template parameter as an element, such as `DetailTemplate`, `RowActions` or `EmptyContent`, the columns have to go inside `<ChildContent>` too.

## MokaTable Parameters

### Data

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IEnumerable<TItem>?` | -- | Rows held in memory. Passing a different collection reloads the table. After changing the same collection in place, call `ReloadAsync()` |
| `ServerData` | `Func<MokaTableState, Task<MokaTableResult<TItem>>>?` | -- | Loads rows on demand. Use it instead of `Items`; it wins when both are set. See [Server-side data](#server-side-data) |
| `ChildContent` | `RenderFragment?` | -- | The `MokaColumn` definitions |
| `ItemKey` | `Func<TItem, object>?` | -- | Row identity: the row's `@key` and its expanded state. See [Row keys](#row-keys) |

### Toolbar

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Title at the left of the toolbar |
| `Searchable` | `bool` | `false` | Shows the search box. See [Search and filters](#search-and-filters) |
| `SearchPlaceholder` | `string` | `"Search..."` | Placeholder of the search box |
| `ShowColumnToggle` | `bool` | `false` | Button that lists the columns with a checkbox each, to hide and show them. See [Arranging columns](#arranging-columns) |
| `ShowDensityToggle` | `bool` | `false` | Button that switches between dense and comfortable rows |
| `ShowRefresh` | `bool` | `false` | Refresh button. It reloads the rows, or raises `OnRefresh` when that is set |
| `OnRefresh` | `EventCallback` | -- | Runs in place of the reload when the refresh button is clicked |
| `Exportable` | `bool` | `false` | Export button. See [Export](#export) |
| `OnExport` | `EventCallback<MokaTableExportContext<TItem>>` | -- | Receives the rows to export. Without it the button downloads a CSV file |
| `ToolbarContent` | `RenderFragment?` | -- | Your own content, after the built-in buttons |
| `ShowToolbar` | `bool` | `true` | `false` hides the toolbar. It only renders when one of the parameters above gives it something to show |

### Pagination

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `PageSize` | `int` | `10` | Rows per page. Two-way bindable. No limit while `ShowPagination` is `false` |
| `PageSizeChanged` | `EventCallback<int>` | -- | Raised when the user picks a page size |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 25, 50, 100]` | Choices in the page size menu. Include `PageSize`, or the menu shows the wrong size |
| `ShowPagination` | `bool` | `true` | Pages the rows and shows the pager. `false` shows every row. See [Showing every row](#showing-every-row) |
| `PaginationPosition` | `MokaTablePaginationPosition` | `Bottom` | `Bottom`, `Top` or `Both`. See [Pager placement](#pager-placement) |
| `StickyPagination` | `bool` | `false` | Keeps the pager in view while the page scrolls past the table |

### Selection

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Selectable` | `bool` | `false` | Adds a checkbox to each row. See [Selecting rows](#selecting-rows) |
| `SelectedItems` | `HashSet<TItem>?` | -- | The selected rows. Two-way bindable |
| `SelectedItemsChanged` | `EventCallback<HashSet<TItem>>` | -- | Raised with a new set after every change |
| `SingleSelect` | `bool` | `false` | One row at a time. Hides the select-all checkbox |
| `SelectionActions` | `RenderFragment<HashSet<TItem>>?` | -- | Bar that takes the toolbar's place while rows are selected. Its context is a copy of the selection. See [Row and selection actions](#row-and-selection-actions) |

### Rows

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OnRowClick` | `EventCallback<TItem>` | -- | Raised when a row is clicked, or when Enter is pressed on one of its cells that isn't editable. See [Keyboard](#keyboard) |
| `OnRowContextMenu` | `EventCallback<MokaItemContextMenuArgs<TItem>>` | -- | Raised on right-click of a row. While it is set, the browser's menu stays closed. See [Context menu](#context-menu) |
| `RowActions` | `RenderFragment<TItem>?` | -- | Content of an extra column at the end of each row |
| `ActionsColumnTitle` | `string` | `"Actions"` | Header of the actions column |
| `ActionsColumnWidth` | `string` | `"auto"` | CSS width of the actions column |
| `Expandable` | `bool` | `false` | Adds a button that opens a detail row. See [Expandable rows](#expandable-rows) |
| `DetailTemplate` | `RenderFragment<TItem>?` | -- | Content of the detail row |
| `RowReorderable` | `bool` | `false` | Adds a drag handle to each row. The handles are off while a sort, search or filter is active. See [Row reordering](#row-reordering) |
| `OnRowReordered` | `EventCallback<(TItem Item, int OldIndex, int NewIndex)>` | -- | Raised when a row is dropped on another row, with positions in `Items`. The table doesn't move it |
| `RowClass` | `Func<TItem, string?>?` | -- | CSS class per row |
| `RowStyle` | `Func<TItem, string?>?` | -- | Inline style per row |

### Columns and cells

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ShowFilters` | `bool` | `false` | Adds a filter row under the header, with a control for each `Filterable` column |
| `OnCellEdit` | `EventCallback<MokaTableCellEditResult<TItem>>` | -- | Raised after an edit in any `Editable` column. See [Inline editing](#inline-editing) |
| `ColumnReorderable` | `bool` | `false` | Lets the user drag a header onto another one to move the column |
| `OnColumnReordered` | `EventCallback<(int OldIndex, int NewIndex)>` | -- | Raised after a column moves, with its old and new positions among the visible columns. The table has already moved it |

### Sorting

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `SortColumn` | `string?` | -- | `Title` of the sorted column. Two-way bindable. A new value replaces the current sort |
| `SortColumnChanged` | `EventCallback<string?>` | -- | Raised after every header click that sorts |
| `SortDirection` | `MokaSortDirection` | `None` | `None`, `Ascending` or `Descending`. Two-way bindable. A new value replaces the current sort |
| `SortDirectionChanged` | `EventCallback<MokaSortDirection>` | -- | Raised after every header click that sorts |
| `MultiSort` | `bool` | `false` | Shift+click sorts by more than one column |

### Appearance

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Dense` | `bool` | `true` | Compact rows. Also the starting state of the density toggle, which doesn't write back |
| `Striped` | `bool` | `false` | Alternating row backgrounds |
| `Hoverable` | `bool` | `true` | Highlights the row under the pointer |
| `Bordered` | `bool` | `false` | Borders between cells |
| `Height` | `string?` | -- | Maximum height of the table area, such as `"400px"`. A taller table scrolls inside it; the toolbar and pager stay outside |
| `FixedHeader` | `bool` | `false` | Keeps the header row in view while the rows scroll. Use it with `Height` |
| `Responsive` | `bool` | `true` | Scrolls the table sideways when it's wider than its container |

### Templates

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `EmptyContent` | `RenderFragment?` | -- | Shown when there are no rows. Without it the table says "No data available". See [Loading and empty states](#loading-and-empty-states) |
| `LoadingContent` | `RenderFragment?` | -- | Shown in one full-width row while `ServerData` loads |
| `SkeletonRows` | `int` | `5` | Placeholder rows while `ServerData` loads and `LoadingContent` isn't set. `0` shows a spinner |
| `FooterContent` | `RenderFragment?` | -- | Your own `<tfoot>` rows. Replaces the aggregate row |

### Virtualization

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Virtualize` | `bool` | `false` | Renders only the rows in view. Needs `Height`. Pair it with `ShowPagination="false"` |
| `VirtualRowHeight` | `float` | `36` | Row height in pixels that virtualization assumes |

`MokaTable` inherits from `MokaVisualComponentBase` (see [Base Classes](../api/base-classes)). `Class`, `Style`, `Id`, `Margin`, `MarginValue`, `Padding`, `PaddingValue`, `Rounded`, `RoundedValue` and extra HTML attributes apply to the wrapper `div`, which draws the table's border and clips its corners. `Size`, `SizeValue`, `Color`, `Variant` and `Disabled` have no effect on the table.

## Methods

| Method | Description |
|--------|-------------|
| `ReloadAsync()` | Loads the rows again: calls `ServerData`, or reapplies search, filters, sorting and paging to `Items`. Stays on the current page, or moves to the last page when the current one no longer exists |

The table reloads by itself after its own sort, page, search and filter changes, and when you pass a different `Items` collection. It can't see changes inside the same collection, or in whatever `ServerData` reads, so call `ReloadAsync()` after those, or pass a new collection. A LINQ query written in the markup, such as `Items="_orders.Where(o => o.Open)"`, is a new collection on every render, so the table reloads every time the parent renders.

```razor
<MokaTable @ref="_table" TItem="Order" Items="_orders">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
</MokaTable>

@code {
    MokaTable<Order>? _table;
    List<Order> _orders = [];

    async Task AddOrderAsync(Order order)
    {
        _orders.Add(order);
        if (_table is not null)
            await _table.ReloadAsync();
    }
}
```

## MokaColumn Parameters

A `MokaColumn` renders nothing itself: it registers with the table around it. Sorting and filters name a column by its `Title`, in the table and in the state `ServerData` receives, so give each column a unique title. A renamed column keeps its sort and filter; a column whose title goes away loses them. The column toggle, column moves and resized widths follow the column itself, so they work for columns without a title too. Columns take no `Class` or `Style`; use `CellClass`.

A new value the parent passes to a column, such as a new `Title`, `Width` or `Align`, shows in the same render. A renamed column keeps its sort and its filter.

### Content

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Header text, and the column's name in sorting and filters |
| `Field` | `Func<TItem, object?>?` | -- | The cell value. Read for the cell text, sorting, search, filters, export and aggregates |
| `Format` | `string?` | -- | Format string such as `"N2"` or `"d"`, for values that implement `IFormattable`. Applies to the cell text and the CSV export, with the current culture |
| `CellTemplate` | `RenderFragment<TItem>?` | -- | Your own cell content. Receives the row item |
| `HeaderTemplate` | `RenderFragment?` | -- | Your own header content, shown in place of `Title`. Sorting and filters still use `Title`, and a sortable column puts its sort button, named "Sort by" and the title, after the template |
| `CellClass` | `string?` | -- | CSS class on this column's data cells |

### Sorting and filtering

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Sortable` | `bool` | `true` | Clicking the header sorts by this column. Needs a `Title`, and with `Items` a `Field` or a `SortComparer`. A header that can't sort shows no sort icon |
| `SortComparer` | `Func<TItem, TItem, int>?` | -- | Compares two rows in place of their `Field` values, in single and multi-column sorts. Needs no `Field` |
| `Filterable` | `bool` | `false` | Adds a control to the filter row. Needs `ShowFilters` on the table and a `Title` |
| `FilterType` | `MokaColumnFilterType` | `Text` | `Text`, a text box that matches on contains, or `Select`, a dropdown of distinct values that matches on equals |

### Layout

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Width` | `string?` | -- | CSS width, such as `"200px"` or `"30%"`. A width the user drags replaces it |
| `MinWidth` | `string?` | -- | CSS minimum width |
| `Align` | `MokaTextAlign` | `Left` | Text alignment of the header, the cells and the aggregate |
| `Visible` | `bool` | `true` | Whether the column starts out shown. The column toggle can change it afterwards, and a new value replaces the user's choice. See [Arranging columns](#arranging-columns) |
| `Sticky` | `bool` | `false` | Keeps the column at the left edge when the table scrolls sideways. Sticky columns all stick at the same spot, so set it on one column |
| `Resizable` | `bool` | `true` | Adds a resize handle to the header |
| `HideOnMobile` | `bool` | `false` | Hides the column's header and all its cells, filter and aggregate cells included, at viewport widths of 768px and below |

### Editing

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Editable` | `bool` | `false` | The cells can be edited in place. See [Inline editing](#inline-editing) |
| `OnCellEdited` | `EventCallback<(TItem Item, object? NewValue)>` | -- | Raised after an edit that changed the text in this column, before the table's `OnCellEdit`. `NewValue` is the entered text, a `string` |

### Footer

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Aggregate` | `MokaAggregateType` | `None` | Footer value: `Sum`, `Average`, `Count`, `Min` or `Max`. See [Footer and aggregates](#footer-and-aggregates) |
| `AggregateFormat` | `string?` | -- | Format of the footer value. `N2` when not set, and no format for `Count` |

## Sorting

Click a header to sort by that column: ascending, then descending, then back to the original order. Every sort goes back to page 1. Each sortable header holds a button with the column's title, so Tab reaches it and Enter or Space sorts like a click. The sorted column's header carries `aria-sort`.

With `Items`, rows sort by the column's `Field` value, which has to implement `IComparable`. Strings, numbers and dates do. `SortComparer` compares two rows instead, in single and multi-column sorts, and needs no `Field`. A column with neither can't be sorted with `Items`, so its header shows no sort icon. `Sortable="false"` turns sorting off for a column.

With `MultiSort`, Shift+click adds a column to the sort. Shift+click the same header again to reverse it, and a third time to remove it. While more than one column is sorted, each sorted header shows its priority. A click without Shift sorts by that column alone.

`SortColumn` and `SortDirection` are two-way bindable. In a multi-column sort they hold the first column. A new value from the parent replaces the current sort, a multi-column one included, goes back to page 1 and shows on the header. Passing the same value again changes nothing, so a parent that renders again without binding doesn't undo a header click. `@bind-` handing back the sort the table just reported changes nothing either, and doesn't load the rows a second time.

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

## Search and filters

`Searchable` adds a search box to the toolbar. A row matches when the `Field` value of any column contains the text, ignoring case. Hidden columns count too. The table searches 300 ms after the last key press, goes back to page 1 and shows the number of matches next to the box.

`ShowFilters` adds a filter row under the header, with a control for each column that has `Filterable` and a `Title`:

- `FilterType="MokaColumnFilterType.Text"`, the default, is a text box. A row matches when the column's value contains the text, ignoring case.
- `MokaColumnFilterType.Select` is a dropdown of the column's distinct values, plus **All**. A row matches when its value equals the choice, ignoring case.

Search and filters compare the value's `ToString()`, not the text that `Format` or a `CellTemplate` shows. A row has to pass the search and every filter. Filters wait 300 ms like the search box, and go back to page 1. A column that loses its filter control, because it is hidden (with the column toggle or `Visible`), removed or no longer `Filterable`, drops its filter: nothing on screen could show it or clear it any more. Turning `ShowFilters` off drops every filter for the same reason.

```blazor-preview
<MokaTable Items="_people" Searchable ShowFilters>
    <MokaColumn Title="Name" Field="x => x.Name" Filterable />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" Filterable FilterType="MokaColumnFilterType.Select" />
</MokaTable>
```

With `ServerData` the table filters nothing itself. It passes the search text as `SearchTerm` and the filter values as `ColumnFilters`, keyed by column title, and your callback applies them (see [Server-side data](#server-side-data)). A `Select` filter lists the values of the rows the table has loaded. With `ServerData` that's the current page, or every row once the pager is off. With `Items` it's every item, whatever the other filters.

## Selecting rows

`Selectable` adds a checkbox to each row. The header checkbox selects or clears the rows on the current page (every row when the pager is off), and shows a partial state when only some of them are selected. `SingleSelect` removes the header checkbox, picking a row replaces the selection, and unticking the selected row clears it. Space on a focused cell toggles its row. A click on the row itself doesn't select it.

Bind the selection with `@bind-SelectedItems`. The table works on its own copy and hands a new set to `SelectedItemsChanged` after each change. It notices a new set from the parent by reference, so to change the selection from code, assign a new set (`_selected = [];`). Clearing the bound set in place changes nothing. A parent that only handles `SelectedItemsChanged`, without passing `SelectedItems`, leaves the selection to the table. The selection survives paging, sorting and filtering.

The set holds the items and compares them with their own `Equals`, not with `ItemKey`. Records compare by value. `ServerData` creates new objects on every load, so rows of a class without its own `Equals` and `GetHashCode` stop showing as selected when their page reloads.

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

## Row and selection actions

`RowActions` adds a column at the end of each row for per-row controls. `ActionsColumnTitle` sets its header and `ActionsColumnWidth` its width. Clicks in it don't raise `OnRowClick`.

`SelectionActions` holds bulk actions. While at least one row is selected, a bar takes the toolbar's place, even with `ShowToolbar="false"`. It shows the count, your content and a **Clear** button. The toolbar, with its title, search box and buttons, comes back once the selection is empty. The template's context is a copy of the selection: read it, and change the selection by assigning a new set to your bound variable, as `Archive` does below. Changing the copy itself changes nothing.

```blazor-preview
@code {
    record Invoice(string Number, string Customer, decimal Total);

    List<Invoice> _invoices =
    [
        new("INV-101", "Acme", 1200m),
        new("INV-102", "Globex", 340m),
        new("INV-103", "Initech", 975m),
    ];

    HashSet<Invoice> _picked = [];

    void Archive(HashSet<Invoice> selected)
    {
        _invoices = _invoices.Where(i => !selected.Contains(i)).ToList();
        _picked = [];
    }

    void Delete(Invoice invoice)
    {
        _invoices = _invoices.Where(i => i != invoice).ToList();
        _picked = _picked.Where(i => i != invoice).ToHashSet();
    }
}

<MokaTable TItem="Invoice" Items="_invoices" Title="Invoices" Selectable @bind-SelectedItems="_picked">
    <ChildContent>
        <MokaColumn Title="Number" Field="x => x.Number" />
        <MokaColumn Title="Customer" Field="x => x.Customer" />
        <MokaColumn Title="Total" Field="x => x.Total" Format="N2" Align="MokaTextAlign.Right" />
    </ChildContent>
    <SelectionActions Context="selected">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Soft" OnClick="() => Archive(selected)">Archive</MokaButton>
    </SelectionActions>
    <RowActions Context="invoice">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" Color="MokaColor.Error"
                    OnClick="() => Delete(invoice)">Delete</MokaButton>
    </RowActions>
</MokaTable>
```

## Expandable rows

`Expandable` adds a button at the start of each row that opens a detail row under it, filled by `DetailTemplate`. The table remembers expanded rows by `ItemKey` when it's set, and by the item otherwise. `ServerData` creates new objects on every load, so set `ItemKey` there to keep rows open across pages and sorts.

The columns sit inside `<ChildContent>` because `DetailTemplate` is written as an element (see [Setup](#setup)).

```blazor-preview
<MokaTable Items="_people" Expandable>
    <ChildContent>
        <MokaColumn Title="Name" Field="x => x.Name" />
        <MokaColumn Title="City" Field="x => x.City" />
    </ChildContent>
    <DetailTemplate Context="person">
        <div style="padding:8px 16px">
            <strong>@person.Name</strong> is @person.Age years old and lives in @person.City.
        </div>
    </DetailTemplate>
</MokaTable>
```

## Custom cell template

`CellTemplate` replaces the cell's content and receives the row item. Sorting, search, filters, export and aggregates still read `Field`, so keep it set.

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

## Inline editing

Set `Editable` on a column to edit its cells in place. Double-click a cell, or press Enter while it has focus. The cell becomes a text box holding the cell's text, all selected. Enter saves, and so does moving focus away with Tab or a click elsewhere. Escape cancels. Enter and Escape put focus back on the cell. One cell is edited at a time, and in a `CellTemplate` column the text box replaces the template while it's open.

When the saved text differs from the text the editor opened with, the table raises the column's `OnCellEdited` with `(Item, NewValue)`, then the table's `OnCellEdit` with a `MokaTableCellEditResult<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Item` | `TItem` | The edited row |
| `ColumnTitle` | `string` | The column's `Title`, or `""` when it has none |
| `OldValue` | `object?` | The column's `Field` value before the edit |
| `NewValue` | `string` | The text that was entered |

The table doesn't write the value back. Parse `NewValue` and update your data. A change to the item itself shows on the next render. If you replace the item instead, a record for example, pass a new collection or call `ReloadAsync()`.

The text box starts with the cell's text, so in a column with `Format` the value arrives formatted, such as `1,200.00`. Parse it with the same format and culture. Saving that text unchanged raises nothing. A double-click also clicks the row twice, so a table with `OnRowClick` raises it twice before the editor opens.

```blazor-preview
@code {
    class StockItem
    {
        public string Sku { get; set; } = "";
        public int Quantity { get; set; }
    }

    List<StockItem> _stock =
    [
        new() { Sku = "BOLT-M6", Quantity = 120 },
        new() { Sku = "NUT-M6", Quantity = 80 },
        new() { Sku = "WASHER-M6", Quantity = 45 },
    ];

    string _editNote = "Double-click a quantity, or focus it and press Enter.";

    void SaveQuantity(MokaTableCellEditResult<StockItem> edit)
    {
        if (int.TryParse(edit.NewValue, out int quantity))
        {
            edit.Item.Quantity = quantity;
            _editNote = $"{edit.Item.Sku}: {edit.OldValue} changed to {quantity}";
        }
        else
        {
            _editNote = $"'{edit.NewValue}' is not a whole number, so {edit.Item.Sku} keeps {edit.OldValue}";
        }
    }
}

<MokaTable TItem="StockItem" Items="_stock" OnCellEdit="SaveQuantity">
    <MokaColumn Title="SKU" Field="x => x.Sku" />
    <MokaColumn Title="Quantity" Field="x => x.Quantity" Editable Align="MokaTextAlign.Right" />
</MokaTable>
<MokaCaption>@_editNote</MokaCaption>
```

## Row reordering

`RowReorderable` adds a drag handle at the start of each row. Drag a row by its handle and drop it on another row: a line on that row's top or bottom edge shows where the dragged row will land. The table then raises `OnRowReordered` with the row's item, its position in `Items` and the position of the row it was dropped on. It moves nothing itself: update your collection and pass the new one back, or call `ReloadAsync()` after changing it in place.

Positions count from the start of `Items`, page offset included: with 10 rows per page, the first row on page 2 is position 10. `RemoveAt(OldIndex)` followed by `Insert(NewIndex, Item)` applies the move, as `MoveChore` does below. While a sort, a search or a column filter is active, the rows on screen aren't in the order of `Items`, so the handles are off: they fade, and their tooltip asks for the sort, search and filters to be cleared. The columns below turn sorting off, so their handles stay on.

```blazor-preview
@code {
    record Chore(string Name, string Owner);

    List<Chore> _chores =
    [
        new("Water the plants", "Ana"),
        new("Take out the bins", "Ben"),
        new("Buy groceries", "Cleo"),
        new("Pay the bills", "Dev"),
    ];

    void MoveChore((Chore Item, int OldIndex, int NewIndex) move)
    {
        List<Chore> reordered = _chores.ToList();
        reordered.RemoveAt(move.OldIndex);
        reordered.Insert(move.NewIndex, move.Item);
        _chores = reordered;
    }
}

<MokaTable TItem="Chore" Items="_chores" RowReorderable OnRowReordered="MoveChore">
    <MokaColumn Title="Chore" Field="x => x.Name" Sortable="false" />
    <MokaColumn Title="Owner" Field="x => x.Owner" Sortable="false" />
</MokaTable>
```

With `ServerData`, the positions are in the result set as your source returns it with no sort, search or filter. Save the new order in your source and call `ReloadAsync()`. Reordering works with the mouse only, and a click on the handle doesn't raise `OnRowClick`.

## Arranging columns

Every column with `Resizable`, the default, has a handle on the right edge of its header. Drag it to set the width, down to 50px. The new width replaces the column's `Width` for as long as the table is on the page. The click that ends the drag doesn't sort.

`ColumnReorderable` lets the user drag a header onto another one. The dragged column takes that position and the columns in between shift over; while you drag, a line on the edge of the header under the pointer shows which side the column will land on. The table keeps the new order itself, uses it for the column list and the export too, and raises `OnColumnReordered` with the old and new positions among the visible columns. A drag cancelled with Escape, or dropped away from the headers, moves nothing.

`ShowColumnToggle` adds a toolbar button that opens a list of the columns, each with a checkbox. Unchecking one hides its column, and a second click on the button closes the list. The toggle doesn't change `Visible`: that is the state each column starts in, so the toggle can show a column that starts hidden, and a new `Visible` value from the parent replaces the user's choice. Hiding a column drops its filter. Hidden columns drop out of the export, but the search still matches them.

The preview also has `ShowDensityToggle`, a button that switches between dense and comfortable rows, starting from `Dense`.

```blazor-preview
<MokaTable Items="_people" Title="People" ColumnReorderable ShowColumnToggle ShowDensityToggle>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

Resizing and moving columns have no keyboard equivalent.

## Footer and aggregates

`Aggregate` adds a footer row with a label and a value under each column that sets it: `Sum`, `Average`, `Count`, `Min` or `Max`. `Count` counts the values that aren't `null`. The others use the values that convert to a number and skip the rest, so they don't work on dates. `AggregateFormat` formats the result. It's `N2` when not set, and `Count` has no format.

Aggregates cover the rows the table has loaded. With `Items` that's every row that passes the search and filters. With `ServerData` it's the current page, or every row once the pager is off.

`FooterContent` replaces the aggregate row with your own `<tfoot>` rows.

```blazor-preview
<MokaTable Items="_people">
    <MokaColumn Title="Name" Field="x => x.Name" Aggregate="MokaAggregateType.Count" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right"
                Aggregate="MokaAggregateType.Average" AggregateFormat="N1" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

## Loading and empty states

While a `ServerData` call runs, the table shows `SkeletonRows` placeholder rows. `SkeletonRows="0"` shows a spinner instead, and `LoadingContent` replaces both with your own content in one full-width row. `Items` are ready at once, so the loading state never shows for them.

`EmptyContent` is shown when there are no rows, including when the search or filters match nothing. Without it the table says "No data available". A table whose `Items` is still `null` shows it too, so render the table once the data is there, or use `ServerData`.

In the preview, click refresh to see the placeholder rows again, or search for a service that doesn't exist.

```blazor-preview
@code {
    record Deployment(string Service, string Version);

    Deployment[] _deployments =
    [
        new("api", "2.4.1"),
        new("web", "1.9.0"),
        new("worker", "0.7.3"),
    ];

    async Task<MokaTableResult<Deployment>> LoadDeployments(MokaTableState state)
    {
        await Task.Delay(800);
        List<Deployment> matches = _deployments
            .Where(d => string.IsNullOrEmpty(state.SearchTerm)
                        || d.Service.Contains(state.SearchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new MokaTableResult<Deployment> { Items = matches, TotalItems = matches.Count };
    }
}

<MokaTable TItem="Deployment" ServerData="LoadDeployments" Title="Deployments"
           Searchable ShowRefresh SkeletonRows="3">
    <ChildContent>
        <MokaColumn Title="Service" Field="x => x.Service" />
        <MokaColumn Title="Version" Field="x => x.Version" />
    </ChildContent>
    <EmptyContent>
        <MokaCaption>No service matches the search.</MokaCaption>
    </EmptyContent>
</MokaTable>
```

## Pager placement

The pager shows while there is more than one page. It also stays while the user is past page 1, and while the rows would need more than one page at the `PageSize` you set, so a user who picked a size that fits every row can pick a smaller one again. That holds with `@bind-PageSize`, where the bound value follows the user's pick. `PaginationPosition` puts it under the table (`Bottom`, the default), above it (`Top`) or both. The top pager is the compact one: previous, next and the page number, with no page size menu, so `Top` alone gives the user no way to change the page size.

Sorting, search, filters and a new page size take the table back to page 1. The current page has no parameter, so the parent can't set it. `PageSize` is two-way bindable.

```blazor-preview
@code {
    record Reading(int Id, string Sensor);

    List<Reading> _readings = Enumerable.Range(1, 42)
        .Select(i => new Reading(i, $"Sensor {i % 5 + 1}"))
        .ToList();

    int[] _pageSizes = [5, 10, 25];
}

<MokaTable Items="_readings" PageSize="5" PageSizeOptions="_pageSizes"
           PaginationPosition="MokaTablePaginationPosition.Both">
    <MokaColumn Title="Id" Field="x => x.Id" />
    <MokaColumn Title="Sensor" Field="x => x.Sensor" />
</MokaTable>
```

`StickyPagination` keeps the pager in view while the page, or a scrolling parent, scrolls past a long table. The bottom pager sticks to the bottom edge and the top pager to the top edge. In this mode the table's wrapper stops clipping its content to its rounded corners.

```razor
<MokaTable Items="_orders" PageSize="100" StickyPagination
           PaginationPosition="MokaTablePaginationPosition.Both">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
</MokaTable>
```

## Server-side data

`ServerData` receives a `MokaTableState` and returns a `MokaTableResult<TItem>`. The table calls it when it first loads, after every sort, page, page size, search and filter change, on refresh, from `ReloadAsync()`, and when the parent passes a new `PageSize`, `SortColumn`, `SortDirection` or `ShowPagination`. A value that `@bind-` hands back after the table reported it isn't new, so a header click or a page size pick makes one call. It filters, sorts and pages nothing itself, so the callback has to apply the whole state.

```csharp
async Task<MokaTableResult<Order>> LoadOrders(MokaTableState state)
{
    var query = _db.Orders.AsQueryable();

    if (!string.IsNullOrEmpty(state.SearchTerm))
        query = query.Where(o => o.Reference.Contains(state.SearchTerm));

    foreach ((string column, string value) in state.ColumnFilters)
    {
        query = column switch
        {
            "Reference" => query.Where(o => o.Reference.Contains(value)),
            "Status" => query.Where(o => o.Status == value),
            _ => query
        };
    }

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
<MokaTable TItem="Order" ServerData="LoadOrders" Title="Orders" Searchable ShowFilters>
    <MokaColumn Title="Reference" Field="x => x.Reference" Filterable />
    <MokaColumn Title="Amount" Field="x => x.Amount" Format="C2" Align="MokaTextAlign.Right"
                Aggregate="MokaAggregateType.Sum" AggregateFormat="C2" />
    <MokaColumn Title="Status" Field="x => x.Status" Filterable FilterType="MokaColumnFilterType.Select" />
</MokaTable>
```

`MokaTableState` carries:

| Property | Type | Description |
|----------|------|-------------|
| `Page` | `int` | Page number, starting at 1 |
| `PageSize` | `int` | Rows per page. While the pager is off, `Page` is 1 and `PageSize` covers the whole result set |
| `SearchTerm` | `string?` | Text in the search box. Empty or `null` when there is none |
| `SortColumn` | `string?` | `Title` of the sorted column, or `null` |
| `SortDirection` | `MokaSortDirection` | Direction of `SortColumn` |
| `SortDescriptors` | `IReadOnlyList<MokaTableSortDescriptor>` | Every sorted column in priority order, each with `Column`, `Direction` and `Priority`. Read it with `MultiSort`. Each call gets its own copies, which later sorts don't change |
| `ColumnFilters` | `IReadOnlyDictionary<string, string>` | Filter row values keyed by column `Title`. A column without a value has no entry |

`MokaTableResult<TItem>` has two required properties: `Items` (`IReadOnlyList<TItem>`), the rows of the requested page, and `TotalItems` (`int`), the row count across all pages, which drives the pager. When a narrower result leaves the current page past the end, the table asks again for the last page.

In the table, a `Text` filter matches on contains, ignoring case, and a `Select` filter on equals. `ColumnFilters` doesn't say which kind a column has, so the callback decides per title, as the `Status` case above does. The loading state shows while the call runs (see [Loading and empty states](#loading-and-empty-states)). Aggregates and the options of a `Select` filter only see the rows of the current page.

## Showing every row

`ShowPagination="false"` turns paging off. The pager is hidden, every row renders, and `PageSize` no longer limits anything. Sorting, search, column filters, select-all, keyboard navigation, row reordering and aggregates all work across the full set. Changing `ShowPagination` at runtime reloads the rows.

```razor
<MokaTable Items="_logEntries" ShowPagination="false" Virtualize Height="480px">
    <MokaColumn Title="Time" Field="x => x.Time" Format="HH:mm:ss" />
    <MokaColumn Title="Message" Field="x => x.Message" />
</MokaTable>
```

For long lists, add `Virtualize` and a fixed `Height` so only the rows in view are rendered. `VirtualRowHeight` is the row height virtualization starts from (36px by default), so set it close to your real row height.

With `ServerData`, the table asks for every row: `Page` is 1 and `PageSize` covers the whole result set, so a `Skip`/`Take` source like the one above works unchanged. The table sizes that request from the last `TotalItems` it saw, so the first load can take two calls: one that learns the total and one that fetches that many rows. A source that caps its page size returns only part of the set, and the table shows what it got. Keep the pager for sources like that.

## Keyboard

The data cells form a grid with one tab stop: the first cell until a cell takes focus, then the last cell that had it. When a page change, a filter or a hidden column takes that cell away, the tab stop moves to the nearest cell left. With `Virtualize`, while that cell's row is scrolled out of the rendered rows, the first rendered cell stands in for it. Tab moves into the grid and out again, and buttons, checkboxes and links inside the rows keep their own place in the tab order. Each sortable header's button is a tab stop of its own, before the grid.

| Key | Action |
|-----|--------|
| Arrow keys | Move to the neighbouring cell. They stop at the edges |
| Home / End | Move to the first or last cell of the row |
| Enter | On an editable cell, start editing. On any other cell of a row with `OnRowClick`, raise `OnRowClick` |
| Space | Toggle the row's selection when the table is `Selectable` |
| Context-menu key / Shift+F10 | Raise `OnRowContextMenu` for the row, when it is set |
| Enter / Space on a header's button | Sort by that column, like a click. With Shift held, add the column to a `MultiSort` sort |

A table inside another table's detail row keeps its keys to itself: Enter and the arrows there act on the inner table only.

Enter raises `OnRowClick` by clicking the focused cell, so the keyboard takes the same path as a mouse click on that row. Only a key pressed on the cell itself counts: Enter on a button inside a cell still presses that button. An editable cell keeps Enter for editing, where Enter saves the value and Escape drops it, and both return focus to the cell.

While a cell is being edited the grid keys are off: the arrows move the caret in the text box, and Tab or a click elsewhere saves the value. Keys pressed in a control inside a `CellTemplate`, such as a text box or a button, stay with that control, so the arrows don't move to another cell, Enter doesn't start an edit and Space doesn't select the row. Moving rows or columns and resizing columns have no keyboard equivalent.

```razor
<MokaTable TItem="Order" Items="_orders" OnRowClick="OpenOrder">
    <MokaColumn Title="Reference" Field="x => x.Reference" />
    <MokaColumn Title="Notes" Field="x => x.Notes" Editable />
</MokaTable>
```

Up to 0.1.12 `OnRowClick` could only be raised with the mouse, and closing the editor with Enter or Escape left focus nowhere, so the arrow keys stopped working. Keys pressed in a control inside a `CellTemplate` reached the cell as well.

## Context menu

`OnRowContextMenu` receives a `MokaItemContextMenuArgs<TItem>` with the row's `Item` and the `MouseEvent`. While a handler is set, right-clicking a row doesn't open the browser's menu, and the context-menu key or Shift+F10 on a focused cell raises it too. Pass the event to `IMokaContextMenuService.Show` to open the shared menu at the pointer. The layout needs a `MokaContextMenuHost` (see [Context Menu](context-menu)).

```razor
@inject IMokaContextMenuService ContextMenu

<MokaTable TItem="Order" Items="_orders" OnRowContextMenu="ShowRowMenu">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
</MokaTable>

@code {
    void ShowRowMenu(MokaItemContextMenuArgs<Order> args) =>
        ContextMenu.Show(args.MouseEvent,
        [
            new MokaContextMenuItem { Text = $"Open {args.Item.Number}", OnClick = () => OpenAsync(args.Item) },
            new MokaContextMenuItem { Text = "Delete", DividerBefore = true, OnClick = () => DeleteAsync(args.Item) }
        ]);
}
```

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

`Exportable` adds an export button to the toolbar. Without `OnExport` the button downloads a CSV file named `export-yyyyMMdd-HHmmss.csv`, in UTF-8 with a byte order mark for Excel. It holds the visible columns that have a `Field`, in the order on screen and with their `Format` applied, and every row that passes the search and filters, in the current sort order. A cell that starts with `=`, `+`, `-`, `@`, a tab or a carriage return gets a leading `'`, so a spreadsheet shows it as text instead of running it as a formula (CSV injection). A plain number such as `-5` holds no formula and stays a number. With `OnExport` set, the table downloads nothing and hands you a `MokaTableExportContext<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | Every row that passes the search and filters, in the current sort order, not just the current page |
| `Columns` | `IReadOnlyList<MokaColumn<TItem>>` | The visible columns, in the order on screen |
| `IsCompleteSet` | `bool` | `false` when a `ServerData` source returned fewer rows than its `TotalItems` |

With `ServerData` the table asks for page 1 with a page size that covers `TotalItems`. A source that caps its page size returns part of the set, and `IsCompleteSet` tells you so.

```razor
<MokaTable TItem="Person" Items="_people" Exportable OnExport="HandleExport">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" />
</MokaTable>

@code {
    void HandleExport(MokaTableExportContext<Person> ctx)
    {
        // ctx.Items: every matching row, not just the current page
        // ctx.Columns: the visible columns
        string csv = string.Join("\n", ctx.Items.Select(p => $"{p.Name},{p.Age}"));
        // Save or download csv here.
    }
}
```
