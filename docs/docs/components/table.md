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
| `ItemKey` | `Func<TItem, object>?` | -- | Row identity: the row's `@key`, its expanded state, and its pick when its data changes. See [Row keys](#row-keys) |

### Toolbar

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Title at the left of the toolbar. It only shows: `Label` names the table for screen readers |
| `TitleContent` | `RenderFragment?` | -- | Your own content at the left of the toolbar, in place of `Title`. See [Title content](#title-content) |
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
| `ShowHeader` | `bool` | `true` | `false` leaves out the header row, for a table whose first row is ordinary data. The sort headings, filter row, column resizing and moving, and select-all box go with it. Set `Label` so the table still has a name |

The built-in toolbar buttons show only an icon, so each shows its name ("Toggle columns", "Export", "Refresh", and the
density it switches to, "Comfortable" or "Dense") in a `MokaTooltip` below the toolbar, the last one lined up with the
table's edge unless `ToolbarContent` follows it. The column button shows none while its menu is open, since the menu opens
in the same place. The search box's clear button and each row's detail toggle are named the same way, the toggles'
names opening beside their column. A toggle at the row's end (`ExpandTogglePosition="MokaTableTogglePosition.End"`) has no
tooltip: see [Expandable rows](#expandable-rows).

#### Title content

`TitleContent` takes the title's place at the left of the toolbar, for more than a string: a count in mono with a status and an action, say. Its parts sit in a row with a small gap. With both set, `TitleContent` shows and `Title` doesn't. Neither names the table for screen readers; set `Label` for that.

```razor
<MokaTable Items="_backups" Label="Backups">
    <TitleContent>
        <MokaText Mono>@_backups.Count</MokaText> backups
        <MokaStatusDot Color="MokaColor.Success" Label="Running" />
        <MokaButton Size="MokaSize.Sm" OnClick="RunNow">Run now</MokaButton>
    </TitleContent>
    <ChildContent>
        <MokaColumn Title="Name" Field="x => x.Name" />
    </ChildContent>
</MokaTable>
```

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
| `BulkActions` | `RenderFragment<IReadOnlyList<TItem>>?` | -- | Buttons in a bar over the header while rows are picked. Its context is the picked rows, the ones on screen first. It wins over `SelectionActions`. See [The bulk bar](#the-bulk-bar) |
| `ItemName` | `Func<TItem, string>?` | -- | What a row is called, for the names in the bulk bar, "Select Printer" on its box and "Show more about Printer" on a toggle at the row's end |
| `ItemsName` | `string?` | -- | What the rows are, in the plural, such as `"devices"`: "50 of 312 devices" and "Show all 312" in the [Show more](#show-more) foot, "Select all devices shown", "Act on the selected devices" naming the bulk bar, and "Loading devices…". Without it the table writes about rows |
| `BulkClearIcon` | `bool` | `false` | The bulk bar's Clear as an icon button, a cross named "Clear the selection" |

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
| `ExpandTogglePosition` | `MokaTableTogglePosition` | `Start` | `Start`, a column before the others, or `End`, a narrow column after them with no tooltip |
| `SingleExpand` | `bool` | `false` | Opening a row closes the one that was open |
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
| `AllowUnsort` | `bool` | `true` | `false` keeps a sorted column sorted: a click flips its direction and never clears it |

### Appearance

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Dense` | `bool` | `true` | Compact rows. Also the starting state of the density toggle, which doesn't write back |
| `Striped` | `bool` | `false` | Alternating row backgrounds |
| `Hoverable` | `bool` | `true` | Highlights the row under the pointer |
| `Bordered` | `bool` | `false` | Borders between cells |
| `Height` | `string?` | -- | Maximum height of the table area, such as `"400px"`. A taller table scrolls inside it; the toolbar and pager stay outside |
| `FixedHeader` | `bool` | `false` | Keeps the header row in view while the rows scroll. Use it with `Height` |
| `Responsive` | `bool` | `true` | Scrolls the table sideways when it's wider than its container. Off, with no `MinWidth` or `Height`, a sticky header sticks to the page. See [A header that sticks to the page](#a-header-that-sticks-to-the-page) |
| `MinWidth` | `string?` | -- | The narrowest the table draws, such as `"960px"`. In a narrower space the rows scroll sideways inside the table |
| `Label` | `string?` | -- | The table's name for screen readers, written as `aria-label` on the `table` element |
| `Framed` | `bool` | `false` | A card of its own, with the toolbar above it. See [Frames](#frames) |
| `ToolbarInFrame` | `bool` | `false` | With `Framed`, draws the toolbar, the chips and the chooser inside the card, as its first rows |
| `Bleed` | `bool` | `false` | Inside a card: drops the table's frame and reaches past the card's padding. See [Frames](#frames) |

### Templates

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `EmptyContent` | `RenderFragment?` | -- | Shown when there are no rows. Without it the table says "No data available". See [Loading and empty states](#loading-and-empty-states) |
| `LoadingContent` | `RenderFragment?` | -- | Shown in one full-width row while `ServerData` loads |
| `SkeletonRows` | `int` | `5` | Placeholder rows while `ServerData` loads and `LoadingContent` isn't set. `0` shows a spinner |
| `Loading` | `bool` | `false` | Skeleton rows in the real columns, for rows the app fetches itself. See [Loading and empty states](#loading-and-empty-states) |
| `LoadingRows` | `int` | `4` | How many rows `Loading` draws |
| `EmptyIcon` | `MokaIconDefinition?` | -- | Icon of the empty state. With this, `EmptyTitle` or `EmptyText` an empty table shows a `MokaEmptyState` |
| `EmptyTitle` | `string?` | -- | Title of the empty state |
| `EmptyText` | `string?` | -- | Text under the title |
| `Empty` | `RenderFragment?` | -- | Your own content for an empty table, without padding. It wins over `EmptyContent` and the empty state |
| `FooterContent` | `RenderFragment?` | -- | Your own `<tfoot>` rows. Replaces the aggregate row |
| `FootContent` | `RenderFragment?` | -- | Any markup in a row under the rows, inside the frame: a flex row with the room between its children, muted, over a line. See [Frames](#frames) |

### Arranging and saved views

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ShowColumnChooser` | `bool` | `false` | A Columns button and a panel to show, hide and order the columns. See [Arranging columns](#arranging-columns) |
| `Layout` | `MokaTableLayout?` | -- | The arrangement to put back. Two-way bindable |
| `LayoutChanged` | `EventCallback<MokaTableLayout>` | -- | Raised when someone hides, moves or resizes a column, or puts them back. The table stores nothing |
| `Views` | `IReadOnlyList<MokaTableView>?` | -- | Saved views, as chips over the table. See [Saved views](#saved-views) |
| `ActiveView` | `MokaTableView?` | -- | The filters in force now. Lights the matching chip, and is what "Save this view" keeps |
| `ViewPicked` | `EventCallback<MokaTableView>` | -- | Raised when a chip is picked |
| `ViewSaved` | `EventCallback<MokaTableView>` | -- | Raised with a new view when someone saves. Without it there is no save button |
| `ViewForgotten` | `EventCallback<MokaTableView>` | -- | Raised when someone forgets a view. Without it chips can't be forgotten |

### Virtualization

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Virtualize` | `bool` | `false` | Renders only the rows in view, from rows already in memory. Needs `Height`. Pair it with `ShowPagination="false"` |
| `VirtualRowHeight` | `float` | `36` | Row height in pixels that virtualization assumes |

`MokaTable` inherits from `MokaVisualComponentBase` (see [Base Classes](../api/base-classes)). `Class`, `Style`, `Id`, `Margin`, `MarginValue`, `Padding`, `PaddingValue`, `Rounded`, `RoundedValue` and extra HTML attributes apply to the wrapper `div`, which draws the table's border and clips its corners. `Size`, `SizeValue`, `Color`, `Variant` and `Disabled` have no effect on the table.

## Methods

| Method | Description |
|--------|-------------|
| `ReloadAsync()` | Loads the rows again: calls `ServerData`, or reapplies search, filters, sorting and paging to `Items`. Stays on the current page, or moves to the last page when the current one no longer exists |
| `RefreshAsync()` | Loads the rows again like `ReloadAsync()`, but keeps the rows on screen until the new ones arrive instead of showing the loading state. For a list refreshed on a timer |
| `ShowAtLeastAsync(int rows)` | Loads the rows again like `RefreshAsync()`, and under `ShowMore` draws at least `rows` of them. See [Show more](#show-more) |

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

A `MokaColumn` renders nothing itself: it registers with the table around it. Sorting and filters name a column by its `Title`, in the table and in the state `ServerData` receives, so give each column a unique title. A renamed column keeps its sort and filter; a column whose title goes away loses them. The column toggle, column moves and resized widths follow the column itself, so they work for columns without a title too. A column takes no `Style`. Its `Class` goes on every cell of the column, header included, and `CellClass` on the data cells only.

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
| `Class` | `string?` | -- | CSS class on every cell of the column: the header, the filter, skeleton, data and aggregate cells. A rule that hides the column at some widths then keeps the rows lined up |

### Sorting and filtering

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Sortable` | `bool` | `true` | Clicking the header sorts by this column. Needs a `Title`, and with `Items` a `Field` or a `SortComparer`. A header that can't sort shows no sort icon |
| `SortComparer` | `Func<TItem, TItem, int>?` | -- | Compares two rows in place of their `Field` values, in single and multi-column sorts. Needs no `Field`. Descending is the comparer turned round, so where it puts empty values last going up they come first going down |
| `SortDescendingFirst` | `bool` | `false` | The first click sorts descending, the most first: for rates, byte counts and times |
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
| `Fixed` | `bool` | `false` | Stays where it is: it can't be dragged or dropped on, and isn't offered in the column toggle or the chooser. See [Arranging columns](#arranging-columns) |
| `Phone` | `MokaColumnPhone?` | -- | Where the cell goes in the list below 720 px: `Primary`, `Secondary`, `Trailing` or `Hidden`. See [On a phone](#on-a-phone) |
| `Mono` | `bool` | `false` | Draws the cells in the monospace font, for addresses, hashes and rates |
| `NoWrap` | `bool` | `false` | Keeps every value on one line, for names, event ids and times. The column grows to its longest value until it has a width; then a longer value ends in an ellipsis. See [Values on one line](#values-on-one-line) |
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

Click a header to sort by that column: ascending, then descending, then back to the original order. A column with `SortDescendingFirst` starts descending, and the table's `AllowUnsort="false"` keeps a sorted column sorted, flipping it on every click. Every sort goes back to page 1. Each sortable header holds a button with the column's title, so Tab reaches it and Enter or Space sorts like a click. The sorted column's header carries `aria-sort`.

With `Items`, rows sort by the column's `Field` value, which has to implement `IComparable`. Strings, numbers and dates do. `SortComparer` compares two rows instead, in single and multi-column sorts, and needs no `Field`. A column with neither can't be sorted with `Items`, so its header shows no sort icon. `Sortable="false"` turns sorting off for a column.

With `MultiSort`, Shift+click adds a column to the sort. Shift+click the same header again to reverse it, and a third time to remove it, unless `AllowUnsort` is off. While more than one column is sorted, each sorted header shows its priority. A click without Shift sorts by that column alone.

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

With `ServerData` the table filters nothing itself. It passes the search text as `SearchTerm` and the filter values as `ColumnFilters`, keyed by column title, and your callback applies them (see [Server-side data](#server-side-data)). It also passes a `CancellationToken`, cancelled once the table has asked for something newer or gone away. Pass it on to whatever fetches the rows. The table ignores the answer to a replaced request either way, so the token only saves the work. A `Select` filter lists the values of the rows the table has loaded. With `ServerData` that's the current page, or every row once the pager is off. With `Items` it's every item, whatever the other filters.

## Selecting rows

`Selectable` adds a checkbox to each row. The header checkbox selects or clears the rows on the current page (every row when the pager is off), and shows a partial state when only some of them are selected. `SingleSelect` removes the header checkbox, picking a row replaces the selection, and unticking the selected row clears it. Space on a focused cell toggles its row. A click on the row itself doesn't select it.

Bind the selection with `@bind-SelectedItems`. The table works on its own copy and hands a new set to `SelectedItemsChanged` after each change. It notices a new set from the parent by reference, so to change the selection from code, assign a new set (`_selected = [];`). Clearing the bound set in place changes nothing. A parent that only handles `SelectedItemsChanged`, without passing `SelectedItems`, leaves the selection to the table. The selection survives paging, sorting and filtering.

The set holds the items and compares them with their own `Equals`. Records compare by value. `ServerData` creates new objects on every load, so rows of a class without its own `Equals` and `GetHashCode` stop showing as selected when their page reloads, unless the table has an `ItemKey`: a new item under the key of a picked row takes the old one's place, so a row whose data changed stays picked.

Shift-click picks a run: every row from the last box touched to this one takes that box's state, so unticking a box and shift-clicking unpicks a run. Shift+Space on a focused cell does the same from the keyboard. A run covers the rows on screen.

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

## The bulk bar

`BulkActions` draws a bar over the header while any row is picked, in the selected tint. It says "3 selected" and, with `ItemName`, their names, cut off when they don't fit, with the whole list on hover. A Clear button sits at its end. Its context is the picked rows, the ones on screen first, then any on other pages.

```razor
<MokaTable Items="_devices" Selectable ItemName="d => d.Name">
    <BulkActions Context="picked">
        <MokaButton Size="MokaSize.Sm" Variant="MokaVariant.Outlined" Color="MokaColor.Secondary"
                    OnClick="() => Block(picked)">Block</MokaButton>
    </BulkActions>
    <ChildContent>
        <MokaColumn Title="Name" Field="x => x.Name" />
        <MokaColumn Title="Address" Field="x => x.Address" Mono />
    </ChildContent>
</MokaTable>
```

`BulkClearIcon` makes the Clear a cross named "Clear the selection", and `ItemsName` names the bar ("Act on the selected devices").

The bar sticks to the top while the page scrolls, provided nothing around the table scrolls on its own. Unlike `SelectionActions` it leaves the toolbar where it is. Set both and `BulkActions` wins. A screen reader hears the count through a status region.

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

At the end of the row instead, the toggle is a narrow last column after any `RowActions`. Its chevron turns when the row is open, it carries `aria-expanded`, and it is named "Show more about" and the row's `ItemName` ("Hide more about" while open), or just "Show more". It has no tooltip, so a long list doesn't carry one per row. On a phone it sits at the far right, after the trailing cell. `SingleExpand` closes the open row when another opens, at either end.

```razor
<MokaTable Items="_devices" ItemKey="d => d.Id" ItemName="d => d.Name"
           Expandable ExpandTogglePosition="MokaTableTogglePosition.End" SingleExpand>
    <ChildContent>
        <MokaColumn Title="Name" Field="d => d.Name" />
        <MokaColumn Title="Address" Field="d => d.Address" Mono />
    </ChildContent>
    <DetailTemplate Context="device">
        <DeviceDetail Device="device" />
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

## Values on one line

A value such as a name, an event id or a time reads wrong broken over two lines. `NoWrap` keeps every value in the column on one line, so the column is as wide as its longest value.

```razor
<MokaTable Items="_entries">
    <MokaColumn Title="Who" Field="x => x.Actor" NoWrap />
    <MokaColumn Title="Action" Field="x => x.Action" Mono NoWrap />
    <MokaColumn Title="Detail" Field="x => x.Detail" />
</MokaTable>
```

Once the column has a width, from `Width` or a drag on its heading's edge, it stays at that width and a longer value ends in an ellipsis instead of widening it. Dragging the edge narrower works the same way, so a column of long ids can be shrunk to what matters. Headings never wrap, with or without it. In the list a table becomes on a phone (see [On a phone](#on-a-phone)), the value stays on one line in its slot and ends in an ellipsis at the row's edge; the column's width means nothing there. Without `NoWrap` nothing changes: values wrap as before.

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

While a sort, search or filter turns reordering off, each handle says why in a `MokaTooltip` beside it. A handle that
can drag has no tooltip: one that showed as a drag started would be carried along in the drag image.

## Arranging columns

Every column with `Resizable`, the default, has a handle on the right edge of its header. Drag it to set the width, down to 50px. The new width replaces the column's `Width` for as long as the table is on the page. The click that ends the drag doesn't sort.

`ColumnReorderable` lets the user drag a header onto another one. The dragged column takes that position and the columns in between shift over; while you drag, a line on the edge of the header under the pointer shows which side the column will land on. The table keeps the new order itself, uses it for the column list and the export too, and raises `OnColumnReordered` with the old and new positions among the visible columns. A drag cancelled with Escape, or dropped away from the headers, moves nothing.

`ShowColumnToggle` adds a toolbar button that opens a list of the columns, each with a checkbox. Unchecking one hides its column, and a second click on the button closes the list. The toggle doesn't change `Visible`: that is the state each column starts in, so the toggle can show a column that starts hidden, and a new `Visible` value from the parent replaces the user's choice. Hiding a column drops its filter. Hidden columns drop out of the export, but the search still matches them.

The list sits in the browser's top layer while it's open, so the table's frame, which hides its overflow for the rounded corners, no longer cuts it off under a table of a row or two, and nothing later on the page paints over it.

The preview also has `ShowDensityToggle`, a button that switches between dense and comfortable rows, starting from `Dense`.

```blazor-preview
<MokaTable Items="_people" Title="People" ColumnReorderable ShowColumnToggle ShowDensityToggle>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

`ShowColumnChooser` adds a Columns button that opens a panel: a box to show or hide each column, buttons to move it earlier or later, the width someone dragged with a way to reset it, and "Put the columns back as they were". Escape or Done closes it, and focus goes back to the button. A column with `Fixed` isn't offered, can't be dragged or dropped on, and the others move around it, so a name column can stay first. Hiding and showing is all the old `ShowColumnToggle` does, so use one or the other.

`Layout` and `LayoutChanged` keep an arrangement. `MokaTableLayout` holds `Order`, `Hidden` and `Widths`, each by column `Title`, so keep titles unique. The table raises it and never stores anything, and it serializes to JSON as it is, so keeping one per person is one line in the app.

```razor
<MokaTable Items="_devices" ShowColumnChooser ColumnReorderable @bind-Layout="_layout">
    <MokaColumn Title="Name" Field="x => x.Name" Fixed />
    <MokaColumn Title="Address" Field="x => x.Address" Mono />
    <MokaColumn Title="Zone" Field="x => x.Zone" />
</MokaTable>

@code {
    MokaTableLayout? _layout;
}
```

An empty layout means the columns as their parameters set them, which is what "Put the columns back" raises. A title no column has is ignored, and kept in the next layout the table raises, so a column inside an `@if` doesn't lose its place while it's away. A column that isn't in `Order` goes after the ones that are. A new `Visible` from the parent still replaces what someone chose.

With a chooser, a `Layout` or a `LayoutChanged`, a heading's right edge is a tab stop: Left and Right change the width by 16 px, Home gives the column its own width back, and so does a double click. Without any of them the edges only take the mouse, as they always did. Moving a column from the keyboard is what the chooser's buttons are for.

## Saved views

`Views` draws named chips over the table, in the toolbar after the search box. A view is a name and the page's filters as a dictionary: `new MokaTableView("Guests", new Dictionary<string, string> { ["zone"] = "guests" })`. The table draws the chips and reports what happens to them. Applying the filters and storing the views is the app's job.

`ActiveView` is the filters in force now, as a view. The chip with the same filters is lit, and "Save this view" keeps these filters under the name typed. It's offered only when there are filters and no view already has them. Its own name isn't used, so a page can build one on the spot.

```razor
<MokaTable Items="_shown" Views="_views" ActiveView="@(new MokaTableView("Now", _filters))"
           ViewPicked="Pick" ViewSaved="v => _views = [.. _views, v]"
           ViewForgotten="v => _views = _views.Where(x => x != v).ToList()">
    <MokaColumn Title="Name" Field="x => x.Name" />
</MokaTable>

@code {
    void Pick(MokaTableView view)
    {
        _filters = new Dictionary<string, string>(view.Filters);
        _shown = Filter(_filters);
    }
}
```

Without a `ViewSaved` handler there is no save button, and without a `ViewForgotten` handler chips can't be forgotten. A view with no filters, such as "Everything", can never be forgotten. A name already taken is refused beside the box.

## Footer and aggregates

`Aggregate` adds a footer row with a label and a value under each column that sets it: `Sum`, `Average`, `Count`, `Min` or `Max`. `Count` counts the values that aren't `null`. The others use the values that convert to a number and skip the rest, so they don't work on dates. `AggregateFormat` formats the result. It's `N2` when not set, and `Count` has no format.

Aggregates cover the rows the table has loaded. With `Items` that's every row that passes the search and filters. With `ServerData` it's the current page, or every row once the pager is off.

`FooterContent` replaces the aggregate row with your own `<tfoot>` rows. Their cells take the footer's padding and
text size, as the aggregate cells do (up to 0.4.4 that rule was scoped to the table and missed your cells).

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

`Loading` is for rows the app fetches itself: it draws `LoadingRows` skeleton rows through `MokaSkeleton`, in the real columns, in place of the rows, and marks the table busy for a screen reader. `LoadingContent` takes their place when set.

`EmptyContent` is shown when there are no rows, including when the search or filters match nothing. Without it the table says "No data available". A table whose `Items` is still `null` shows it too, so render the table once the data is there, or use `ServerData`.

`EmptyIcon`, `EmptyTitle` and `EmptyText` show a `MokaEmptyState` instead, which announces itself politely. Any one of them is enough. `Empty` is a fragment of your own, filling the row without padding, for when the empty state needs a button. `Empty` wins over `EmptyContent`, and `EmptyContent` over the three parameters.

```razor
<MokaTable Items="_devices" Loading="_loading"
           EmptyIcon="MokaIcons.Action.Search" EmptyTitle="No device matches"
           EmptyText="Try another zone, or clear the search.">
    <MokaColumn Title="Name" Field="x => x.Name" />
</MokaTable>
```

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

The pager's arrows ("First page", "Previous page", "Next page", "Last page") show their names in a `MokaTooltip` above
the pager, or below it when `MokaPagination` is pinned to the top, and keep it while greyed out. Up to 0.1.19 they were
`title` attributes.

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

## Show more

`ShowMore` draws the first `PageSize` rows in sort order, then "Showing 8 of 14", "Show 6 more" and "Show all" in the foot, in place of numbered pages. Numbered pages stay the default. It wins over `ShowPagination`.

```razor
<MokaTable Items="_devices" ShowMore PageSize="8" SortColumn="Name" SortDirection="MokaSortDirection.Ascending">
    <MokaColumn Title="Name" Field="x => x.Name" />
</MokaTable>
```

With `ItemsName` the foot counts what the rows are: "50 of 312 devices", "Show 50 more" and "Show all 312".

A sort or new rows keep what was asked for, and a new `PageSize` from the parent starts over. Under `ServerData` the table asks for page 1, one page size larger each time, and the rows on screen stay while more load. "Show all" asks for the whole set the way `ShowPagination="false"` does. When the foot goes with the last rows, focus moves to the first new row.

A page that loads more rows because someone asked for them, such as older log entries added to the same list, calls `ShowAtLeastAsync` so they are drawn. It reads the list again too, which the table can't notice when the list changes in place.

```razor
<MokaTable @ref="_table" TItem="LogEntry" Items="_entries" ShowMore PageSize="100">
    <MokaColumn Title="Time" Field="e => e.Time" />
</MokaTable>

@code {
    MokaTable<LogEntry>? _table;
    List<LogEntry> _entries = [];

    async Task LoadOlderAsync()
    {
        _entries.AddRange(await Logs.OlderThanAsync(_entries[^1]));
        if (_table is not null)
            await _table.ShowAtLeastAsync(_entries.Count);
    }
}
```

## Showing every row

`ShowPagination="false"` turns paging off. The pager is hidden, every row renders, and `PageSize` no longer limits anything. Sorting, search, column filters, select-all, keyboard navigation, row reordering and aggregates all work across the full set. Changing `ShowPagination` at runtime reloads the rows.

```razor
<MokaTable Items="_logEntries" ShowPagination="false" Virtualize Height="480px">
    <MokaColumn Title="Time" Field="x => x.Time" Format="HH:mm:ss" />
    <MokaColumn Title="Message" Field="x => x.Message" />
</MokaTable>
```

For long lists, add `Virtualize` and a fixed `Height` so only the rows in view are rendered. `VirtualRowHeight` is the row height virtualization starts from (36px by default), so set it close to your real row height.

`Virtualize` saves rendering, not loading. With `ServerData`, the table asks for every row: `Page` is 1 and `PageSize` covers the whole result set, so a `Skip`/`Take` source like the one above works unchanged. For a server list too long to load, keep the pager, or use [`MokaVirtualList`](/components/virtual-list) with `ItemsProvider`, which loads only what's in view. The table sizes that request from the last `TotalItems` it saw, so the first load can take two calls: one that learns the total and one that fetches that many rows. A source that caps its page size returns only part of the set, and the table shows what it got. Keep the pager for sources like that.

## Grouped rows

`GroupBy` returns the heading a row belongs under; rows that return the same string are shown together under a
collapsible heading that spans the table.

```razor
<MokaTable Items="_tasks" GroupBy="@(t => t.Status)">
    <MokaColumn TItem="TaskItem" Title="Name" Field="@(t => t.Name)" />
    <MokaColumn TItem="TaskItem" Title="Owner" Field="@(t => t.Owner)" />
</MokaTable>
```

Grouping gathers; it does not reorder. A group appears where its first row landed and keeps its rows in the
order they already had, so the sort and the filters still decide the order. Group by a column the rows are not
sorted by and you get each group once, where it first appeared, rather than one heading per run.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `GroupBy` | `Func<TItem, string>?` | - | The heading a row belongs under. Null leaves the rows ungrouped |
| `GroupHeaderTemplate` | `RenderFragment<MokaTableGroup<TItem>>?` | - | Content of a heading, in place of the key and the count |
| `GroupsCollapsible` | `bool` | `true` | Whether a heading collapses its group |
| `InitiallyCollapsedGroups` | `IReadOnlyCollection<string>?` | - | Groups that start collapsed, by key. The starting state only: the user's own collapsing is kept apart from it |
| `OnGroupToggled` | `EventCallback<(string Key, bool Collapsed)>` | - | Raised when a group is collapsed or expanded |

`MokaTableGroup<TItem>` carries the group's `Key`, its `Count` on the page, whether it is `Collapsed`, and its
`Items`.

Grouping and `Virtualize` do not combine: grouping wins, because a virtualized list has no room for headings
between its rows.

## Frames

`Framed` makes the table a card of its own: surface, a 1 px outline and large corners, with the toolbar, the chips and the chooser above the card rather than in it. `Bleed` is for a table inside a card: it drops the table's frame and reaches past the card's padding, so the rows run to the card's edges and bottom corners. Without either the table keeps its plain border.

`ToolbarInFrame` puts the toolbar, the chips and the chooser inside the card instead, as its first rows with a line under them. `FootContent` is a row under the rows, inside the card: any markup, laid out with the room between its children, in small muted text over a line. `FooterContent` is something else, table rows in a `tfoot`.

```razor
<MokaTable Items="_devices" Label="Devices" ItemsName="devices" Framed ToolbarInFrame Title="Devices" Searchable>
    <FootContent>
        <span>All @_devices.Count devices</span>
        <span>Right-click a row for more</span>
    </FootContent>
    <ChildContent>
        <MokaColumn Title="Name" Field="x => x.Name" />
        <MokaColumn Title="Maker" Field="x => x.Maker" Class="hide-below-1500" />
    </ChildContent>
</MokaTable>
```

A column's `Class` lands on every cell of the column, header included, so a page can hide a column at some widths with its own CSS and the rows stay lined up.

### A header that sticks to the page

On the new design the header sticks to the top of whatever scrolls the rows. `Responsive`, on by default, makes the table scroll sideways inside its own box, so that box is what the header sticks to, and it never sticks to the page. With `Responsive="false"`, and no `MinWidth` or `Height`, nothing around the rows scrolls: the header sticks to the page, under a fixed app bar (`--moka-appbar-space`), and so does the bulk bar. Give a wide list a `MinWidth` and it scrolls sideways inside the table again, with the header sticking inside it.

```razor
<MokaTable Items="_devices" Framed Responsive="false">
    <MokaColumn Title="Name" Field="x => x.Name" />
</MokaTable>
```

```razor
<MokaCard Title="Latest knocks">
    <MokaTable Items="_knocks" Bleed>
        <MokaColumn Title="Address" Field="x => x.Address" Mono />
    </MokaTable>
</MokaCard>
```

## On a phone

A table stays a table that scrolls sideways until one of its columns names a `Phone` slot. Then, below 720 px, each row becomes a list item and the header goes out of sight.

| Slot | Where |
|------|-------|
| `Primary` | The first line, larger. Without any, the first column takes it |
| `Secondary` | The second line, smaller and muted. Several are joined with a dot |
| `Trailing` | At the right, beside both lines: a value, a status or a button |
| `Hidden` | Not shown. This is what a column with no slot gets too |

```razor
<MokaTable Items="_devices" Selectable>
    <MokaColumn Title="Name" Field="x => x.Name" Phone="MokaColumnPhone.Primary" />
    <MokaColumn Title="Address" Field="x => x.Address" Mono Phone="MokaColumnPhone.Secondary" />
    <MokaColumn Title="Zone" Field="x => x.Zone" Phone="MokaColumnPhone.Secondary" />
    <MokaColumn Title="Seen" Field="x => x.Seen" />
    <MokaColumn Title="Rate" Field="x => x.Rate" Mono Phone="MokaColumnPhone.Trailing" />
</MokaTable>
```

The markup is still a `<table>` with one cell per column, so there is one copy of each cell and a change at 720 px costs no render. Once a column names a slot the elements carry explicit table roles, because a browser stops treating a table drawn as a grid as a table, and the header stays readable to a screen reader. A slot shows on a phone even if the column is hidden above 720 px, so someone who hid the name on a desktop doesn't get empty rows. Sorting, the chooser and the handles aren't offered in the list. The filter row and the toolbar stay. Tab starts on the primary cell, and the arrow keys skip cells a phone hides.

## Keyboard

The data cells form a grid with one tab stop: the first cell until a cell takes focus, then the last cell that had it. When a page change, a filter or a hidden column takes that cell away, the tab stop moves to the nearest cell left. With `Virtualize`, while that cell's row is scrolled out of the rendered rows, the first rendered cell stands in for it. Tab moves into the grid and out again, and buttons, checkboxes and links inside the rows keep their own place in the tab order. Each sortable header's button is a tab stop of its own, before the grid.

| Key | Action |
|-----|--------|
| Arrow keys | Move to the neighbouring cell. They stop at the edges |
| Home / End | Move to the first or last cell of the row |
| Enter | On an editable cell, start editing. On any other cell of a row with `OnRowClick`, raise `OnRowClick` |
| Space | Toggle the row's selection when the table is `Selectable` |
| Shift+Space | Pick the run from the last box touched to this row |
| Left / Right / Home on a heading's right edge | Change the column's width by 16 px, or give it its own back. The edges take focus only in a table people arrange. See [Arranging columns](#arranging-columns) |
| Context-menu key / Shift+F10 | Raise `OnRowContextMenu` for the row, when it is set |
| Enter / Space on a header's button | Sort by that column, like a click. With Shift held, add the column to a `MultiSort` sort |

A table inside another table's detail row keeps its keys to itself: Enter and the arrows there act on the inner table only.

Enter raises `OnRowClick` by clicking the focused cell, so the keyboard takes the same path as a mouse click on that row. Only a key pressed on the cell itself counts: Enter on a button inside a cell still presses that button, and arrows in a text box inside a cell stay in the box. An editable cell keeps Enter for editing, where Enter saves the value and Escape drops it, and both return focus to the cell.

While a cell is being edited the grid keys are off: the arrows move the caret in the text box, and Tab or a click elsewhere saves the value. Keys pressed in a control inside a `CellTemplate`, such as a text box or a button, stay with that control, so the arrows don't move to another cell, Enter doesn't start an edit and Space doesn't select the row. Moving rows, and dragging a header to move its column, have no keyboard equivalent: the chooser's buttons move columns.

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

`ItemKey` gives each row an identity. The table uses it as the row's `@key`, so when sorting, filtering or a reload moves a row, Blazor moves the row's DOM and component state with it instead of rebuilding it. It also records which rows are expanded, and a picked row whose data changed stays picked. Without `ItemKey` the item itself is the key, and records compare by value.

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
