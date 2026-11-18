---
title: Table
description: Data grid over client-side, server-side or streamed rows, with sorting, search, five filter controls, column menus, pinned columns and rows, nested groups, seven pagers, seven looks, three densities, selection, editing, CSV and Excel export, blocks of cells, row and column virtualization, column kinds and one savable state.
order: 7
---

# Table

`MokaTable<TItem>` shows rows from `Items`, a collection in memory, from a `ServerData` callback that loads one page or one window at a time, or from an `ItemStream` whose rows show as they arrive. Columns are child `MokaColumn<TItem>` components, or the [column kinds](/components/table-columns) built on it. Sorting, paging, search, filters, grouping and selection work in both modes: with `Items` the table does the work itself, and with `ServerData` it passes a `MokaTableRequest` to your callback. Everything someone can change is one `MokaTableState`, which you can bind, restore, keep in the browser or save as a view. The search, the filter controls and quick filters have a page of their own: [Table Filters](/components/table-filters). So do [editing](/components/table-editing) and [export and copying](/components/table-export).

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
| `ServerData` | `Func<MokaTableRequest, Task<MokaTableResult<TItem>>>?` | -- | Loads rows on demand. Use it instead of `Items`; it wins when both are set. See [Server-side data](#server-side-data) |
| `ItemStream` | `IAsyncEnumerable<TItem>?` | -- | Rows that arrive over time, shown as they come, a render a frame. Used when `Items` and `ServerData` are null. See [Streamed rows](#streamed-rows) |
| `ChildContent` | `RenderFragment?` | -- | The columns, and a `MokaTablePager` when the pager isn't the numbered one |
| `ItemKey` | `Func<TItem, object>?` | -- | Row identity: the row's `@key`, its expanded state, and its pick when its data changes. See [Row keys](#row-keys) |
| `OnLoadFailed` | `EventCallback<Exception>` | -- | Raised when the latest `ServerData` load fails, for logging. The table shows the failure with Try again either way. See [A failed load](#a-failed-load) |

### State

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `State` | `MokaTableState?` | -- | What the table shows: sorts, filters, quick filter, search, page, page size, density, columns, grouping and open rows. Two-way bindable. A new value seeds the table. See [The table's state](#the-tables-state) |
| `StateChanged` | `EventCallback<MokaTableState>` | -- | Raised with the whole state whenever someone changes any part of it |
| `StorageKey` | `string?` | -- | Keeps the state from visit to visit under this key, in local storage or in a registered `IMokaTableStateStore`. See [Keeping the state](#keeping-the-state) |
| `Page` | `int` | `1` | The page shown. Two-way bindable with `@bind-Page`. It is the state's page, and like the state a new value seeds it |
| `PageChanged` | `EventCallback<int>` | -- | Raised when the page changes |
| `Search` | `string?` | -- | The text in the search box. Two-way bindable with `@bind-Search`. It is the state's search, and like the state a new value seeds it |
| `SearchChanged` | `EventCallback<string?>` | -- | Raised when the search changes |

### Toolbar

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Title at the left of the toolbar. It only shows: `Label` names the table for screen readers |
| `TitleContent` | `RenderFragment?` | -- | Your own content at the left of the toolbar, in place of `Title`. See [Title content](#title-content) |
| `Searchable` | `bool` | `false` | Shows the search box. See [Search and filters](#search-and-filters) |
| `ShowColumnToggle` | `bool` | `false` | Button that lists the columns with a checkbox each, to hide and show them. See [Arranging columns](#arranging-columns) |
| `ShowDensityToggle` | `bool` | `false` | Button that opens the three densities. See [Densities](#densities) |
| `ShowRefresh` | `bool` | `false` | Refresh button. It reloads the rows, or raises `OnRefresh` when that is set |
| `OnRefresh` | `EventCallback` | -- | Runs in place of the reload when the refresh button is clicked |
| `Exportable` | `bool` | `false` | Export button. See [Table Export](/components/table-export) |
| `ExportFormats` | `MokaTableExportFormats` | `Csv` | What the export button writes: `Csv`, `Excel`, or both, which opens a menu |
| `ExportFileName` | `string?` | -- | The exported file's name, without its extension. Null is `export-` and the date and time |
| `OnExport` | `EventCallback<MokaTableExportContext<TItem>>` | -- | Receives the rows to export and the format. Without it the table writes the file |
| `CopySelectedRows` | `bool` | `false` | A Copy button for the picked rows, and Ctrl+C, as tab-separated text for a spreadsheet |
| `CellRanges` | `bool` | `false` | Marking blocks of cells, copying them, and pasting blocks into editable cells |
| `ToolbarContent` | `RenderFragment?` | -- | Your own content, after the built-in buttons |
| `ShowToolbar` | `bool` | `true` | `false` hides the toolbar. It only renders when one of the parameters above gives it something to show |
| `ShowHeader` | `bool` | `true` | `false` leaves out the header row, for a table whose first row is ordinary data. The sort headings, filter row, column resizing and moving, and select-all box go with it. Set `Label` so the table still has a name |
| `Text` | `MokaTableText?` | -- | Every word the table, its pager and its column kinds write. Null is `MokaTableText.Default`, in English. See [Words](#words) |

The built-in toolbar buttons show only an icon, so each shows its name ("Toggle columns", "Export", "Refresh", "Density") in a `MokaTooltip` below the toolbar, the last one lined up with the table's edge unless `ToolbarContent` follows it. The column and density buttons show none while their menu is open, since the menu opens in the same place. The search box's clear button and each row's detail toggle are named the same way, the toggles' names opening beside their column. A toggle at the row's end (`ExpandTogglePosition="MokaTableTogglePosition.End"`) has no tooltip: see [Expandable rows](#expandable-rows).

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

### Filtering

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `FilterControls` | `MokaTableFilterControls` | `Row` | Which filter controls the table draws, combined as needed: `Row`, `Menus`, `Chips`, `Facets`, `Builder`, or `None`. The filter row draws once a shown column is `Filterable`. See [Table Filters](/components/table-filters#filter-controls) |
| `QuickFilters` | `IReadOnlyList<MokaTableQuickFilter>?` | -- | Preset filters above the table, with an "All" choice first and how many rows each keeps. See [Quick filters](/components/table-filters#quick-filters) |
| `QuickFilterLook` | `MokaQuickFilterLook` | `Segments` | `Segments`, a segmented control, or `Tabs` |
| `FacetsFromServer` | `bool` | `false` | Asks a `ServerData` source for the values of the list filters and facets with their counts, and for the quick filters' counts |
| `Debounce` | `TimeSpan` | 300 ms | How long the table waits after the last key in the search box or a filter box before it loads. Uses a registered `TimeProvider`, else the system clock |
| `HighlightSearch` | `bool` | `false` | Marks what the search found in the cells that write their text, in a `<mark class="moka-table-mark">` |

`Search` and `SearchChanged` are with the [state](#state) parameters.

### Paging

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `PageSize` | `int` | `10` | Rows per page when the state doesn't say. No limit while `ShowPagination` is `false`. A size someone picks is part of the state |
| `ShowPagination` | `bool` | `true` | Pages the rows and shows the pager. `false` shows every row. See [Showing every row](#showing-every-row) |

How the pager looks, where it sits, whether it sticks and which page sizes it offers belong to the `MokaTablePager` part. See [Pagers](#pagers).

### Selection

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Selectable` | `bool` | `false` | Adds a checkbox to each row. See [Selecting rows](#selecting-rows) |
| `SelectedItems` | `HashSet<TItem>?` | -- | The selected rows. Two-way bindable. A parent that passes it and handles `SelectedItemsChanged` owns the selection: a change it doesn't take back is undone. See [Selecting rows](#selecting-rows) |
| `SelectedItemsChanged` | `EventCallback<HashSet<TItem>>` | -- | Raised with a new set after every change |
| `SelectAllMatching` | `bool` | `false` | With `ServerData` and `Virtualize`, the select-all box picks every row that matches the search and the filters, the rows not loaded too, rather than the rows loaded. See [Every matching row](#every-matching-row) |
| `SelectionChanged` | `EventCallback<MokaTableSelection<TItem>>` | -- | Raised after every change with all that is picked: the rows picked one by one, or every matching row but those unticked since |
| `SingleSelect` | `bool` | `false` | One row at a time. Hides the select-all checkbox |
| `SelectOnRowClick` | `bool` | `false` | With `Selectable`, a click on the row selects it the way a file list does: a plain click picks that row alone, Ctrl or Cmd adds or removes it, Shift picks a run. `OnRowClick` is still raised |
| `SelectionActions` | `RenderFragment<HashSet<TItem>>?` | -- | Bar that takes the toolbar's place while rows are selected. Its context is a copy of the selection. See [Row and selection actions](#row-and-selection-actions) |
| `BulkActions` | `RenderFragment<IReadOnlyList<TItem>>?` | -- | Buttons in a bar over the header while rows are picked. Its context is the picked rows, the ones on screen first. It wins over `SelectionActions`. See [The bulk bar](#the-bulk-bar) |
| `ItemName` | `Func<TItem, string>?` | -- | What a row is called, for the names in the bulk bar, "Select Printer" on its box and "Show more about Printer" on a toggle at the row's end |
| `ItemsName` | `string?` | -- | What the rows are, in the plural, such as `"devices"`: "50 of 312 devices" and "Show all 312" under the [load more pager](#load-more-and-endless-scroll), "Select all devices shown", "Act on the selected devices" naming the bulk bar, and "Loading devices…". Without it the table writes about rows |
| `BulkClearIcon` | `bool` | `false` | The bulk bar's Clear as an icon button, a cross named "Clear the selection" |

### Rows

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `OnRowClick` | `EventCallback<TItem>` | -- | Raised when a row is clicked, or when Enter is pressed on one of its cells that isn't editable. See [Keyboard](#keyboard) |
| `OnRowDoubleClick` | `EventCallback<TItem>` | -- | Raised when a row is double-clicked. A cell being edited keeps its double click; the row still hears it |
| `OnRowContextMenu` | `EventCallback<MokaItemContextMenuArgs<TItem>>` | -- | Raised on right-click of a row. While it is set, the browser's menu stays closed. See [Context menu](#context-menu) |
| `RowActions` | `RenderFragment<TItem>?` | -- | Content of an extra column at the end of each row. Its heading is `Text.Actions` |
| `ActionsColumnWidth` | `string` | `"auto"` | CSS width of the actions column |
| `Expandable` | `bool` | `false` | Adds a button that opens a detail row. See [Expandable rows](#expandable-rows) |
| `DetailTemplate` | `RenderFragment<TItem>?` | -- | Content of the detail row |
| `ExpandTogglePosition` | `MokaTableTogglePosition` | `Start` | `Start`, a column before the others, or `End`, a narrow column after them with no tooltip |
| `SingleExpand` | `bool` | `false` | Opening a row closes the one that was open |
| `ShowExpandAll` | `bool` | `false` | A button over the toggles that opens every row shown, or closes them all. Ignored with `SingleExpand` |
| `PinnedTop` | `IReadOnlyList<TItem>?` | -- | Rows held at the top, whatever the sort, the filters and the page. See [Pinned rows](#pinned-rows) |
| `PinnedBottom` | `IReadOnlyList<TItem>?` | -- | Rows held at the bottom, such as a totals row. With `FixedHeader` or `Height` they stay in view |
| `RowReorderable` | `bool` | `false` | Adds a drag handle to each row. The handles are off while a sort, search or filter is active. See [Row reordering](#row-reordering) |
| `OnRowReordered` | `EventCallback<(TItem Item, int OldIndex, int NewIndex)>` | -- | Raised when a row is dropped on another row, with positions in `Items`. The table doesn't move it |
| `RowClass` | `Func<TItem, string?>?` | -- | CSS class per row |
| `RowStyle` | `Func<TItem, string?>?` | -- | Inline style per row |

### Columns and cells

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ColumnMenu` | `bool` | `false` | A menu button in each heading: sort, filter, pin, group by, hide and fit the column. See [Column menu](#column-menu) |
| `ColumnReorderable` | `bool` | `false` | Lets the user drag a header onto another one to move the column |
| `OnColumnReordered` | `EventCallback<(int OldIndex, int NewIndex)>` | -- | Raised after a column moves, with its old and new positions among the visible columns. The table has already moved it |

### Sorting

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `MultiSort` | `bool` | `false` | Shift+click sorts by more than one column |
| `AllowUnsort` | `bool` | `true` | `false` keeps a sorted column sorted: a click flips its direction and never clears it |

The sort itself is part of the state. A column's `DefaultSort` sets the sort the table starts with. See [Sorting](#sorting).

### Appearance

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Look` | `MokaTableLook?` | -- | How the rows are drawn. Null takes the theme's `Tables`, else `Lined`, the table as it has always looked. See [Looks](#looks) |
| `Density` | `MokaTableDensity?` | -- | How tightly rows are spaced when the state doesn't say. Null takes the theme's `TableDensity`, else the look's own: `Comfortable` for `Airy`, `Compact` otherwise. See [Densities](#densities) |
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
| `EmptyContent` | `RenderFragment?` | -- | Shown when there are no rows and no search or filter is active. Without it the table writes `Text.NoData`, "No data available". See [Loading and empty states](#loading-and-empty-states) |
| `NoResults` | `RenderFragment?` | -- | Your own content for a search, filters or a quick filter that leave no row, in place of "No results for ..." with a button that clears them. See [No results](#no-results) |
| `ErrorContent` | `RenderFragment<MokaTableFailure>?` | -- | Your own content for a failed `ServerData` load, in place of the message with Try again. Its context has `Error` and `Retry`. See [A failed load](#a-failed-load) |
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
| `ShowColumnChooser` | `bool` | `false` | A Columns button and a panel to show, hide and order the columns. What someone arranges is part of the state. See [Arranging columns](#arranging-columns) |
| `Views` | `IReadOnlyList<MokaTableView>?` | -- | Saved views, as chips over the table. Picking one puts its state in force. See [Saved views](#saved-views) |
| `ViewSaved` | `EventCallback<MokaTableView>` | -- | Raised with a new view when someone saves how the table looks now. Without it there is no save button |
| `ViewForgotten` | `EventCallback<MokaTableView>` | -- | Raised when someone forgets a view. Without it chips can't be forgotten |

### Grouping

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `GroupBy` | `string?` | -- | The `Title` of the column the rows are grouped by when the state doesn't say. The state's `Groups` can nest more levels. See [Grouped rows](#grouped-rows) |
| `GroupHeaderTemplate` | `RenderFragment<MokaTableGroup<TItem>>?` | -- | Content of a heading, in place of the key, the count and the totals |
| `GroupsCollapsible` | `bool` | `true` | Whether a heading collapses its group |
| `InitiallyCollapsedGroups` | `IReadOnlyCollection<string>?` | -- | Groups that start collapsed, by id: the key alone for a group of the first level. The starting state only: the user's own collapsing is kept apart from it |
| `OnGroupToggled` | `EventCallback<(string Key, bool Collapsed)>` | -- | Raised when a group is collapsed or expanded, with the group's id |

### Editing

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `EditMode` | `MokaTableEditMode` | `Cell` | One cell at a time, or a whole row with Save and Cancel |
| `OnRowSave` | `EventCallback<MokaTableRowSave<TItem>>` | -- | Saves an edited, added or pasted row, or an undo. The table changes none of your objects |
| `OnRowDelete` | `EventCallback<TItem>` | -- | Deletes a row, from a `MokaEditColumn`'s Delete button |
| `ConfirmDelete` | `bool` | `true` | Asks before a delete, through `IMokaDialogService` |
| `NewItem` | `Func<TItem>?` | -- | Makes the row the toolbar's Add button opens |
| `AddRowPosition` | `MokaTableAddPosition` | `Top` | Where a new row opens |
| `EditCopy` | `Func<TItem, TItem>?` | -- | Copies a row before it is edited, for setters that set properties in place |
| `Validators` | `RenderFragment?` | -- | Validator components, such as `DataAnnotationsValidator`, run against the row before a save |
| `ShowUndo` | `bool` | `true` | A line under the rows after a save, with Undo |

See [Table Editing](/components/table-editing).

### Virtualization

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Virtualize` | `bool` | `false` | Renders only the rows in view. Needs `Height`. With `Items`, pair it with `ShowPagination="false"`. With `ServerData` the rows load window by window: see [Rows a server sends window by window](#rows-a-server-sends-window-by-window) |
| `VirtualRowHeight` | `float` | `36` | Row height in pixels that virtualization assumes |
| `VirtualizeColumns` | `bool` | `false` | Draws only the cells of the columns in view, for tables of about 50 columns or more. See [Columns in view](#columns-in-view) |

`MokaTable` inherits from `MokaVisualComponentBase` (see [Base Classes](../api/base-classes)). `Class`, `Style`, `Id`, `Margin`, `MarginValue`, `Padding`, `PaddingValue`, `Rounded`, `RoundedValue` and extra HTML attributes apply to the wrapper `div`, which draws the table's border and clips its corners. `Size`, `SizeValue`, `Color`, `Variant` and `Disabled` have no effect on the table.

## Methods

| Method | Description |
|--------|-------------|
| `ReloadAsync()` | Loads the rows again: calls `ServerData`, or reapplies search, filters, sorting and paging to `Items`. Stays on the current page, or moves to the last page when the current one no longer exists |
| `RefreshAsync()` | Loads the rows again like `ReloadAsync()`, but keeps the rows on screen until the new ones arrive instead of showing the loading state. For a list refreshed on a timer |
| `ShowAtLeastAsync(int rows)` | Loads the rows again like `RefreshAsync()`, and under the load more and endless pagers draws at least `rows` of them. See [Load more and endless scroll](#load-more-and-endless-scroll) |
| `CurrentState()` | The state the table shows now, with every property filled in |
| `CurrentSelection()` | What is picked now, as a `MokaTableSelection<TItem>`: the rows picked one by one, or every matching row but those unticked since. See [Every matching row](#every-matching-row) |
| `ResetStateAsync()` | Puts the table back as its parameters set it: the columns' default sorts, no filters, quick filter or search, page 1, the default page size, density, columns and grouping, nothing opened. Raises `StateChanged` |
| `ClearSearchAndFiltersAsync()` | Clears the search, the filters and the quick filter, and loads the rows again. Raises `StateChanged` |
| `ExpandAllAsync()` | Opens the detail of every row shown. Raises `StateChanged` |
| `CollapseAllAsync()` | Closes the detail of every row. Raises `StateChanged` |
| `ExpandAllGroupsAsync()` | Opens every group. Raises `StateChanged` |
| `CollapseAllGroupsAsync()` | Folds every group, at every level. Raises `StateChanged` |
| `ExportAsync(format)` | Exports every matching row as CSV or Excel, as the export button does |
| `StartEditAsync(item, column)` | Opens a row for editing, at one column in cell mode or the whole row in row mode |
| `AddRowAsync()` | Opens a new row from `NewItem` |
| `SaveEditAsync()` | Saves the open row. Returns whether it was saved |
| `CancelEdit()` | Closes the open row without saving it |
| `UndoAsync()` | Puts back the values the last save changed, through `OnRowSave` |
| `StopStreaming()` | Stops reading `ItemStream`, keeping the rows that came |

`HasLoadFailed` says whether the last `ServerData` load failed, or the `ItemStream` threw, and the table shows the failure. `IsEditing` says whether a row is open, `CanUndo` whether there is a save to undo, and `IsStreaming` whether rows are still arriving.

The table reloads by itself after its own sort, page, search and filter changes, when you pass a different `Items` collection, and when the parent passes a state that says something new. It can't see changes inside the same collection, or in whatever `ServerData` reads, so call `ReloadAsync()` after those, or pass a new collection. A LINQ query written in the markup, such as `Items="_orders.Where(o => o.Open)"`, is a new collection on every render, so the table reloads every time the parent renders.

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

A `MokaColumn` renders nothing itself: it registers with the table around it. Sorts, filters, grouping and the saved state name a column by its `Title`, in the table and in the request `ServerData` receives, so give each column a unique title. A renamed column keeps its sort, filter and grouping; a column whose title goes away loses them. The column toggle, column moves and resized widths follow the column itself, so they work for columns without a title too. A column takes no `Style`. Its `Class` goes on every cell of the column, header included, and `CellClass` on the data cells only.

A new value the parent passes to a column, such as a new `Title`, `Width` or `Align`, shows in the same render.

Columns and heading groups keep the order they stand in the markup. A column or a group inside an `@if` that comes back takes its place again, inside its group too, and **Put the columns back** puts it there. An arrangement in force, from someone moving the columns or from the state, still decides where a column it names goes. A column written inside another component of yours, such as a reusable column, renders after the columns written straight in the table, so it joins after them. Give the columns an `Order` when one of them sits in a component of yours: the table lists them lowest first, and columns with the same `Order` keep the markup's order, as CSS `order` does. A column in a `MokaColumnGroup` orders within the group, and a group takes an `Order` among the table's columns. A column whose `Order` changes moves, unless an arrangement in force places it. A column kind that overrides `OnParametersSet` calls the base, which is where the column says where it stands. Up to 0.8.0 a column or group that came back went after all the others.

The [column kinds](/components/table-columns) (`MokaPersonColumn`, `MokaNumberColumn`, `MokaStatusColumn` and the rest) derive from `MokaColumn` and take every parameter below, plus their own. Wrap columns in a `MokaColumnGroup` for a heading over several of them (see [Column groups](#column-groups)).

### Content

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | Header text, and the column's name in sorts, filters, grouping and the state |
| `Field` | `Func<TItem, object?>?` | -- | The cell value. Read for sorting, value filters, export and aggregates, and written with `Format` for the cell text, the search and text filters |
| `Format` | `string?` | -- | Format string such as `"N2"` or `"d"`, for values that implement `IFormattable`. Applies to the cell text, the search, text filters and the CSV export, with the current culture |
| `CellTemplate` | `RenderFragment<TItem>?` | -- | Your own cell content. Receives the row item. It wins over what a column kind draws |
| `HeaderTemplate` | `RenderFragment?` | -- | Your own header content, shown in place of `Title`. Sorting and filters still use `Title`, and a sortable column puts its sort button, named "Sort by" and the title, after the template |
| `CellClass` | `string?` | -- | CSS class on this column's data cells |
| `Class` | `string?` | -- | CSS class on every cell of the column: the header, the filter, skeleton, data and aggregate cells. A rule that hides the column at some widths then keeps the rows lined up |

### Sorting, filtering and grouping

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Sortable` | `bool` | `true` | Clicking the header sorts by this column. Needs a `Title`, and with `Items` a value: a `Field`, a column kind's own value or a `SortComparer`. A header that can't sort shows no sort icon |
| `DefaultSort` | `MokaSortDirection` | `None` | The sort the table starts with and goes back to on reset, and the column's first click. Several columns sort in the order they stand. A `State` that names sorts wins |
| `SortComparer` | `Func<TItem, TItem, int>?` | -- | Compares two rows in place of their values, in single and multi-column sorts. Needs no `Field`. Descending is the comparer turned round, so where it puts empty values last going up they come first going down |
| `SortDescendingFirst` | `bool` | `false` | The first click sorts descending, the most first: for rates, byte counts and times |
| `Filterable` | `bool` | `false` | The table's filter controls offer this column: an input in the filter row, a filter button in its heading, a choice in the builder, and a list in the facet panel for list and yes or no filters. Needs a `Title`. See [Table Filters](/components/table-filters) |
| `FilterType` | `MokaColumnFilterType` | `Auto` | What the column's filter takes: `Auto` (from its values), `Text`, `Number`, `Date`, `Select`, `MultiSelect` or `YesNo`. See [What a column's filter takes](/components/table-filters#what-a-columns-filter-takes) |
| `FilterOptions` | `IReadOnlyList<MokaFilterOption>?` | -- | The choices a list filter offers, in place of the values the table finds in its rows. Each is `MokaFilterOption(Value, Text)` |
| `GroupKey` | `Func<TItem, string>?` | -- | The heading a row goes under when the table is grouped by this column. Null takes the cell's text. A column with a key groups without a `Field` |

### Layout

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Width` | `string?` | -- | CSS width, such as `"200px"` or `"30%"`. A width the user drags replaces it |
| `MinWidth` | `string?` | -- | CSS minimum width |
| `Align` | `MokaTextAlign` | `Left` | Text alignment of the header, the cells and the aggregate. Number, money and size columns default to `Right` |
| `Visible` | `bool` | `true` | Whether the column starts out shown. The column toggle can change it afterwards, and a new value replaces the user's choice. See [Arranging columns](#arranging-columns) |
| `Order` | `int` | `0` | Where the column stands among the table's columns, or its group's: lower first, and columns with the same `Order` keep the markup's order. For a column rendered by a component of yours |
| `Pin` | `MokaColumnPin` | `None` | The edge the column starts pinned to while the rows scroll sideways: `Start` or `End`. Any number of columns can pin to each side. See [Pinned columns](#pinned-columns) |
| `Resizable` | `bool` | `true` | Adds a resize handle to the header |
| `Fixed` | `bool` | `false` | Stays where it is: it can't be dragged or dropped on, and isn't offered in the column toggle or the chooser. See [Arranging columns](#arranging-columns) |
| `Phone` | `MokaColumnPhone?` | -- | Where the cell goes in the list below 720 px, and in the list look at every width: `Primary`, `Secondary`, `Trailing` or `Hidden`. See [On a phone](#on-a-phone) |
| `Mono` | `bool` | `false` | Draws the cells in the monospace font, for addresses, hashes and rates |
| `NoWrap` | `bool` | `false` | Keeps every value on one line, for names, event ids and times. The column grows to its longest value until it has a width; then a longer value ends in an ellipsis. See [Values on one line](#values-on-one-line) |
| `HideOnMobile` | `bool` | `false` | Hides the column's header and all its cells, filter and aggregate cells included, at viewport widths of 768px and below |

### Editing

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Editable` | `bool` | `false` | The cells can be edited in place. See [Table Editing](/components/table-editing) |
| `Editor` | `MokaColumnEditor` | `Auto` | The editor: `Auto` (from the column), `Text`, `Number`, `Date`, `Select`, `Checkbox`, `Switch` or `Rating` |
| `EditOptions` | `IReadOnlyList<MokaFilterOption>?` | -- | The choices a list editor offers. Null takes `FilterOptions`, an enum's names or the loaded values |
| `EditTemplate` | `RenderFragment<MokaCellEdit<TItem>>?` | -- | Your own editor |
| `SetField` | `Func<TItem, object?, TItem>?` | -- | Writes a value into a row and returns it: a new record with `with`. Column kinds have a typed setter too |
| `Validate` | `Func<TItem, object?, string?>?` | -- | Checks a value: what is wrong with it, or null |
| `Property` | `string?` | the `Title` | The property the table's `Validators` file this column's messages under |

### Footer

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Aggregate` | `MokaAggregateType` | `None` | Footer value: `Sum`, `Average`, `Count`, `Min` or `Max`. See [Footer and aggregates](#footer-and-aggregates) |
| `AggregateFormat` | `string?` | -- | Format of the footer value. `N2` when not set, and no format for `Count` |

## The table's state

`MokaTableState` is everything someone can change and expect to find again: `Sorts`, `Filters`, `FilterJoin` (`All` or `Any`), `QuickFilter` (the quick filter's name), `Search`, `Page`, `PageSize`, `Density`, `Columns` (order, hidden, dragged widths and pins), `Groups`, `CollapsedGroups` and `Expanded`. Columns are named by their `Title`. Bind it with `@bind-State`, or pass one and listen to `StateChanged`.

```blazor-preview
@code {
    record Person(string Name, int Age, string City);
    IEnumerable<Person> _people = [
        new("Alice", 31, "London"),
        new("Bob", 27, "Paris"),
        new("Carol", 35, "Berlin"),
        new("Dan", 42, "Madrid"),
    ];

    MokaTableState? _state;
}

<MokaTable Items="_people" Searchable ShowDensityToggle @bind-State="_state">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
<MokaCaption>
    Sorted by @(_state?.Sorts is { Count: > 0 } sorts ? string.Join(", ", sorts.Select(s => $"{s.Column} {s.Direction}")) : "nothing"),
    page @(_state?.Page ?? 1), @(_state?.Density ?? MokaTableDensity.Compact)
</MokaCaption>
```

A property left null keeps the table's default for it, so a state that says only `Sorts` sorts the table and leaves the rest as its parameters set it. `Sorts = null` keeps the columns' `DefaultSort`, where an empty list is unsorted; `PageSize` and `Density` null keep the table's parameters; `Groups` null keeps `GroupBy`, where an empty list is ungrouped. The table always reports a state with every property filled in, and `CurrentState()` gives you one at any time.

```razor
<MokaTable Items="_orders" State="@(new MokaTableState { Sorts = [new("Placed", MokaSortDirection.Descending)] })">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Placed" Field="o => o.Placed" Format="d" />
</MokaTable>
```

Two states are equal when they say the same thing, whichever lists hold them. The table applies a state only when the parent passes one that differs, by content, from the last one it passed, and from the state in force. So an inline `new MokaTableState { ... }` written in the markup doesn't undo a click every time the parent renders, and `@bind-State` handing back what the table just raised changes nothing and loads nothing. To put a state back after someone changed it, pass a state that says something new, or call `ResetStateAsync()`, which goes back to the parameters: the columns' default sorts, page 1, `PageSize`, `Density`, `GroupBy` and `InitiallyCollapsedGroups`.

`StateChanged` is raised after every change someone makes: a sort, a filter or search once it loads, a page, a page size, a density, a column hidden, moved or resized, a view picked, a group folded, a row opened. A table nobody binds, keeps or saves views of builds no state at all.

`MokaTableState.SameView(other)` says whether two states show the same view of the rows: the same sorts, filters and their join, quick filter, search, page size, columns and grouping. The page, the density and what is folded or opened don't count. Saved views use it.

`ToJson()` and `MokaTableState.FromJson(json)` write and read a state as JSON with no reflection, so a trimmed app can store it. Enums are written by name. `FromJson` gives null for empty text or text that isn't a table state.

### The current page and the search

`@bind-Page` binds the page alone, for a page that shows "Page 3" somewhere else or keeps it in the address. It is the state's page, so it moves with `StateChanged` too.

```razor
<MokaTable Items="_orders" PageSize="20" @bind-Page="_page">
    <MokaColumn Title="Number" Field="o => o.Number" />
</MokaTable>

@code {
    int _page = 1;
}
```

`@bind-Search` binds the search the same way, for a search typed somewhere else on the page or kept in the address. A new value shows in the box and loads the rows.

```razor
<MokaTable Items="_files" Searchable @bind-Search="_search">
    <MokaColumn Title="Name" Field="f => f.Name" />
</MokaTable>
<MokaButton OnClick="@(() => _search = "report")">Find reports</MokaButton>

@code {
    string? _search;
}
```

### Seeds and owned values

`State`, `Page` and `Search` are seeds. A new value from the parent seeds the table, and `StateChanged`, `PageChanged` and `SearchChanged` report what someone changes after that. A parent that passes the same value again changes nothing, and a change the parent doesn't take back stays on screen.

`SelectedItems` is an owned value, and so are [`MokaPagination.CurrentPage` and `PageSize`](/components/pagination). A parent that passes it and handles its change callback owns it: a change the parent doesn't take back, by passing the new value, is undone once its handler has run. That lets a handler refuse a change. `@bind-` takes every change, for either kind.

### Keeping the state

`StorageKey` keeps the state from visit to visit under that key. Without anything registered it goes to the browser's local storage, under `moka-table:` and the key. A private window or full storage keeps nothing, and the table works on. Register your own `IMokaTableStateStore` as a scoped service to keep it per person on the server, for example: `LoadAsync(key, cancellationToken)` and `SaveAsync(key, state, cancellationToken)`, with `ToJson` and `FromJson` to turn the state into text.

```razor
<MokaTable Items="_devices" StorageKey="devices" ShowColumnChooser ShowDensityToggle>
    <MokaColumn Title="Name" Field="x => x.Name" Fixed />
    <MokaColumn Title="Address" Field="x => x.Address" Mono />
</MokaTable>
```

```csharp
public sealed class ProfileTableStore(ProfileService profiles) : IMokaTableStateStore
{
    public async ValueTask<MokaTableState?> LoadAsync(string key, CancellationToken cancellationToken) =>
        MokaTableState.FromJson(await profiles.ReadAsync($"table:{key}", cancellationToken));

    public async ValueTask SaveAsync(string key, MokaTableState state, CancellationToken cancellationToken) =>
        await profiles.WriteAsync($"table:{key}", state.ToJson(), cancellationToken);
}

builder.Services.AddScoped<IMokaTableStateStore, ProfileTableStore>();
```

The kept state is read once the table is interactive, after its first render, and put back over whatever the table started with; then `StateChanged` is raised with it. The first load waits for it, so the rows load once, already in the kept order. A render that won't go interactive, such as static server rendering, loads straight away and keeps nothing. Every later change is saved.

## Sorting

Click a header to sort by that column: its first direction, then the other, then back to the original order. The first direction is the column's `DefaultSort` when it has one, else descending with `SortDescendingFirst`, else ascending. The table's `AllowUnsort="false"` keeps a sorted column sorted, flipping it on every click. Every sort goes back to page 1. Each sortable header holds a button with the column's title, so Tab reaches it and Enter or Space sorts like a click. The first sorted column's header carries `aria-sort`.

With `Items`, rows sort by the column's value, which has to implement `IComparable`. Strings, numbers and dates do. `SortComparer` compares two rows instead, in single and multi-column sorts, and needs no `Field`. A column with neither can't be sorted with `Items`, so its header shows no sort icon. `Sortable="false"` turns sorting off for a column.

With `MultiSort`, Shift+click adds a column to the sort. Shift+click the same header again to reverse it, and a third time to remove it, unless `AllowUnsort` is off. While more than one column is sorted, each sorted header shows its priority. A click without Shift sorts by that column alone.

`DefaultSort` on a column is the sort the table starts with and goes back to on `ResetStateAsync()`. The sort is part of the state, as `MokaTableSort(Column, Direction)` records, first sort first: set it with `State` and read it from `StateChanged`.

```blazor-preview
<MokaTable Items="_people" Title="People" MultiSort>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" DefaultSort="MokaSortDirection.Descending" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

With `ServerData`, the first request waits for the columns to register during the first render, so their `DefaultSort` goes out with it and no second request is needed. See [Server-side data](#server-side-data).

With `ColumnMenu`, a heading's menu sorts too: Sort ascending, Sort descending, and Clear sort while the column is sorted. Without `MultiSort` a pick replaces the sort; with it the pick adds or changes that column's part of the sort.

## Search and filters

`Searchable` adds a search box to the toolbar. A row matches when the text of any column with a value holds the search, ignoring case: the text the cell shows, `Format` applied, and a column kind's own text. Hidden columns count too. The table searches `Debounce` (300 ms) after the last key press, goes back to page 1 and shows the number of matches next to the box. The search is part of the state and binds with `@bind-Search`. `HighlightSearch` marks the matches in the cells that write their text.

Filters come from five controls, picked with `FilterControls` and combined as needed, and from quick filters. The default, `Row`, draws a filter row under the headings once a shown column is `Filterable`, with an input that fits each column: text, a number range, a date range, a list, a list of several, or yes and no. `FilterType` decides which, from the column's values unless you say. `Menus` puts a filter button in each heading, `Chips` lists the filters in force, `Facets` adds a side panel of values with counts, and `Builder` a panel of conditions joined by all or any. Every control reads and writes the same filters, which are part of the state.

```razor
<MokaTable Items="_orders" Searchable HighlightSearch
           FilterControls="MokaTableFilterControls.Row | MokaTableFilterControls.Chips">
    <MokaColumn Title="Number" Field="o => o.Number" Filterable />
    <MokaColumn Title="Placed" Field="o => o.Placed" Format="d" Filterable />
    <MokaColumn Title="Total" Field="o => o.Total" Format="C2" Align="MokaTextAlign.Right" Filterable />
    <MokaStatusColumn Title="Status" Status="o => o.Status" Filterable />
</MokaTable>
```

Here the number filters as text, the date as a range of days, the total as a range of amounts and the status as a list of several. [Table Filters](/components/table-filters) covers each control, quick filters, the operators, the counts and the no results state.

With `ServerData` the table filters nothing itself. It passes the search, the filters, their join and the quick filter in the request, and your callback applies them (see [Server-side data](#server-side-data)).

## Selecting rows

`Selectable` adds a checkbox to each row. The header checkbox selects or clears the rows on the current page (every row when the pager is off), and shows a partial state when only some of them are selected. `SingleSelect` removes the header checkbox, picking a row replaces the selection, and unticking the selected row clears it. Space on a focused cell toggles its row. A click on the row itself doesn't select it, unless `SelectOnRowClick` is set (see below).

Bind the selection with `@bind-SelectedItems`. The table works on its own copy and hands a new set to `SelectedItemsChanged` after each change. The selection is an owned value: a parent that passes `SelectedItems` and handles `SelectedItemsChanged` owns it, and a change it doesn't take back, by passing the new set, is undone once its handler has run. `@bind-SelectedItems` takes every change. A parent that only handles `SelectedItemsChanged`, or only passes `SelectedItems`, leaves the selection to the table, which follows a set the parent passes when it is a different set. Sets are compared by reference, so to change the selection from code, assign a new set (`_selected = [];`). Clearing the bound set in place changes nothing. The selection survives paging, sorting and filtering.

A handler that refuses some changes passes back only the sets it accepts:

```razor
<MokaTable Items="_files" Selectable SelectedItems="_picked" SelectedItemsChanged="Pick">
    <MokaColumn Title="Name" Field="f => f.Name" />
</MokaTable>

@code {
    HashSet<FileEntry> _picked = [];

    void Pick(HashSet<FileEntry> picked)
    {
        if (picked.Count <= 10)
            _picked = picked;
    }
}
```

An eleventh pick shows while the handler runs and goes away after it.

`SelectOnRowClick` selects from a click on the row, the way a file list does: a plain click picks that row alone, Ctrl or Cmd adds or removes it, and Shift picks every row from the last one picked. With `SingleSelect` a click toggles the row. Controls in the row, such as buttons and links, keep their clicks, and `OnRowClick` is still raised.

```razor
<MokaTable Items="_files" Selectable SelectOnRowClick @bind-SelectedItems="_picked"
           OnRowDoubleClick="OpenAsync">
    <MokaColumn Title="Name" Field="f => f.Name" />
    <MokaBytesColumn Title="Size" Value="f => f.Length" />
</MokaTable>
```

The set holds the items and compares them with their own `Equals`. Records compare by value. `ServerData` creates new objects on every load, so rows of a class without its own `Equals` and `GetHashCode` stop showing as selected when their page reloads, unless the table has an `ItemKey`: a new item under the key of a picked row takes the old one's place, so a row whose data changed stays picked.

Shift-click picks a run: every row from the last box touched to this one takes that box's state, so unticking a box and shift-clicking unpicks a run. Shift+Space on a focused cell does the same from the keyboard. A run covers the rows on screen, and under [windows](#runs-past-the-window) the rows between too.

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

`RowActions` adds a column at the end of each row for per-row controls. `Text.Actions` is its header and `ActionsColumnWidth` its width. Clicks in it don't raise `OnRowClick`. For quiet icon buttons and a menu per row, the [actions and row menu columns](/components/table-columns) draw them for you.

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

With an `ItemKey`, the open rows are in the state's `Expanded`, each key written in the invariant culture, so a kept or bound state opens them again. Without one the table can't say which rows they are, and `Expanded` stays empty.

The columns sit inside `<ChildContent>` because `DetailTemplate` is written as an element (see [Setup](#setup)).

```blazor-preview
<MokaTable Items="_people" Expandable ItemKey="p => p.Name">
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

`ShowExpandAll` draws a button over the toggles, in their column's heading, that opens every row shown, or closes them all while they are open. It is named "Show detail for every row" or "Hide detail for every row" in a `MokaTooltip`, and `SingleExpand` turns it off. From code, `ExpandAllAsync()` opens every row shown and `CollapseAllAsync()` closes every row. Both raise `StateChanged`.

## Pinned rows

`PinnedTop` and `PinnedBottom` hold rows at the top and the bottom of the table, whatever the sort, the filters, the search and the page: a row to compare with, or a totals row. They aren't sorted, filtered, paged or counted, and they draw the columns' cells with nothing to select, open or drag. With `FixedHeader` or a `Height`, the bottom ones stay in view while the rows scroll. They don't show while `Loading` is on or after a failed load. Buttons from the [actions column](/components/table-columns#actions) work in them.

```razor
<MokaTable Items="_regions" PinnedBottom="_allRegions" FixedHeader Height="420px">
    <MokaColumn Title="Region" Field="r => r.Name" />
    <MokaNumberColumn Title="Orders" Value="r => r.Orders" Format="N0" />
    <MokaMoneyColumn Title="Revenue" Value="r => r.Revenue" Currency="EUR" />
</MokaTable>

@code {
    List<Region> _regions = [];
    List<Region> _allRegions = [];

    protected override async Task OnInitializedAsync()
    {
        _regions = await Sales.RegionsAsync();
        _allRegions = [new Region("All regions", _regions.Sum(r => r.Orders), _regions.Sum(r => r.Revenue))];
    }
}
```

## Custom cell template

`CellTemplate` replaces the cell's content and receives the row item. Sorting, search, filters, export and aggregates still read the column's value, so keep `Field` set. For a person with an avatar, a status tag, a progress bar and the other common cells, a [column kind](/components/table-columns) draws them and tells sort, filter and export its value.

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

Set `Editable` on a column and give the table an `OnRowSave`. Enter, F2 or a double click opens an editor that fits the column, from the library's inputs; Enter saves, Escape puts the value back, and in cell mode Tab saves and opens the next editable cell. The table writes the change into a copy through the column's setter and hands `OnRowSave` the row as it was, the updated row and the changes, each value of the column's own type. `EditMode="MokaTableEditMode.Row"` opens a whole row with Save and Cancel, `MokaEditColumn` draws each row's Edit, Save, Cancel and Delete buttons, `NewItem` adds rows, and Ctrl+Z undoes the last save. Values are checked by each column's `Validate` and by `Validators` such as `DataAnnotationsValidator`. All of it is on [Table Editing](/components/table-editing).

```razor
<MokaTable Items="_parts" ItemKey="p => p.Id" OnRowSave="Save">
    <MokaColumn Title="Name" Field="p => p.Name" Editable SetField="@((p, v) => p with { Name = (string?)v ?? "" })" />
    <MokaNumberColumn Title="Stock" Value="p => p.Stock" Editable SetValue="@((p, v) => p with { Stock = (int)(v ?? 0) })" />
</MokaTable>

@code {
    void Save(MokaTableRowSave<Part> save) =>
        _parts = _parts.Select(p => p.Id == save.Item.Id ? save.Updated : p).ToList();
}
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

Every column with `Resizable`, the default, has a handle on the right edge of its header. Drag it to set the width, down to 50px. The new width replaces the column's `Width` and goes into the state. The click that ends the drag doesn't sort.

`ColumnReorderable` lets the user drag a header onto another one. The dragged column takes that position and the columns in between shift over; while you drag, a line on the edge of the header under the pointer shows which side the column will land on. The table keeps the new order itself, uses it for the column list and the export too, and raises `OnColumnReordered` with the old and new positions among the visible columns. A drag cancelled with Escape, or dropped away from the headers, moves nothing.

`ShowColumnToggle` adds a toolbar button that opens a list of the columns, each with a checkbox. Unchecking one hides its column, and a second click on the button closes the list. The toggle doesn't change `Visible`: that is the state each column starts in, so the toggle can show a column that starts hidden, and a new `Visible` value from the parent replaces the user's choice. Hiding a column drops its filter while the only controls that could show it are the filter row, the heading menus or the facets (see [Which filters stay](/components/table-filters#which-filters-stay)). Hidden columns drop out of the export, but the search still matches them.

The list sits in the browser's top layer while it's open, so the table's frame, which hides its overflow for the rounded corners, no longer cuts it off under a table of a row or two, and nothing later on the page paints over it.

```blazor-preview
<MokaTable Items="_people" Title="People" ColumnReorderable ShowColumnToggle ShowDensityToggle>
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

`ShowColumnChooser` adds a Columns button that opens a panel: a box to show or hide each column, buttons to move it earlier or later, the width someone dragged with a way to reset it, and "Put the columns back as they were". Escape or Done closes it, and focus goes back to the button. A column with `Fixed` isn't offered, can't be dragged or dropped on, and the others move around it, so a name column can stay first. Hiding and showing is all `ShowColumnToggle` does, so use one or the other.

The arrangement is the state's `Columns`: a list of `MokaTableColumnState(Title)` in display order, each with `Hidden`, `Width` in pixels and `Pin` (`None`, `Start` or `End`). Keep it with `StorageKey` or `@bind-State`.

```razor
<MokaTable Items="_devices" ShowColumnChooser ColumnReorderable @bind-State="_state">
    <MokaColumn Title="Name" Field="x => x.Name" Fixed />
    <MokaColumn Title="Address" Field="x => x.Address" Mono />
    <MokaColumn Title="Zone" Field="x => x.Zone" />
</MokaTable>

@code {
    MokaTableState? _state;
}
```

An empty `Columns` means the columns as their parameters set them, which is what "Put the columns back" raises. A title no column has is ignored, and kept in the next state the table raises, so a column inside an `@if` doesn't lose its place while it's away. A column the list leaves out goes after the ones it names, and is shown. A new `Visible` or `Pin` from the parent still replaces what someone chose. See [Pinned columns](#pinned-columns) for how pins draw.

With a chooser, a `StateChanged` listener or a `StorageKey`, a heading's right edge is a tab stop: Left and Right change the width by 16 px, Home gives the column its own width back, and so does a double click. Without any of them the edges only take the mouse, so the default keyboard path stays as it was. Moving a column from the keyboard is what the chooser's buttons are for.

## Pinned columns

`Pin` keeps a column at an edge while the rows scroll sideways: `MokaColumnPin.Start`, left in a left-to-right page, or `End`. Any number of columns can pin to each side. They move to their edge in the order they stand, so the start pins come first, then the others, then the end pins.

```razor
<MokaTable Items="_orders" MinWidth="1400px" ColumnMenu>
    <MokaColumn Title="Number" Field="o => o.Number" Pin="MokaColumnPin.Start" />
    <MokaColumn Title="Customer" Field="o => o.Customer" Pin="MokaColumnPin.Start" />
    <MokaColumn Title="Placed" Field="o => o.Placed" Format="d" />
    <MokaColumn Title="Carrier" Field="o => o.Carrier" />
    <MokaColumn Title="Address" Field="o => o.Address" />
    <MokaMoneyColumn Title="Total" Value="o => o.Total" Pin="MokaColumnPin.End" />
    <MokaRowMenuColumn Items="RowItems" Pin="MokaColumnPin.End" />
</MokaTable>
```

The browser measures each pinned column's offset once per layout change, such as a column shown, hidden, moved, resized or pinned, and writes it on the table as a CSS variable. A resize observer keeps the offsets right as widths change, and scrolling sends nothing to .NET. A shadow marks the edge of the pinned columns while there is more to see under them on that side. A table with no pinned column measures nothing.

Pins are part of the state, as `Pin` on each `MokaTableColumnState`. The column menu pins and unpins too, except a `Fixed` column, and a new `Pin` from the parent replaces what the menu or the state chose. Every row the table draws pins its cells the same way: the headings, the filter row, the data rows and the totals. A [heading group](#column-groups) splits where a pinned column leaves it. `Sticky` is gone: `Pin="MokaColumnPin.Start"` does what it did, and more than one column can use it.

## Column menu

`ColumnMenu` draws a menu button in each heading, named "Status column options" (`Text.ColumnMenu`). It opens a menu in the browser's top layer with what the column allows:

| Item | When |
|------|------|
| Sort ascending, Sort descending | The column can sort. The current direction is checked |
| Clear sort | The column is sorted |
| Filter… | The column is `Filterable`. It opens the column's [filter popup](/components/table-filters#heading-menus), under the heading's filter button with the `Menus` control, else under the heading, so the menu filters without that control too |
| Pin to start, Pin to end | The column isn't `Fixed`. The current pin is checked |
| Unpin | The column is pinned |
| Group by this column | The column has a title, and a value or a `GroupKey`. Checked while the rows are grouped by it. A pick adds a grouping level inside the others, or takes it away |
| Hide column | The column isn't `Fixed` and has a title |
| Fit to content | Always. The column takes the width that shows every value on one line, the widest of its heading and the cells on screen, kept in the state like a dragged width |

The menu follows the WAI-ARIA menu button pattern. Focus goes to the first item. The arrows, Home, End and a letter move between items, Enter or Space picks one, and Escape or Tab closes the menu with focus back on its button. Moving between items happens in the browser and sends nothing to .NET. A grouped table with a column menu shows the [group bar](#grouped-rows).

```razor
<MokaTable Items="_orders" ColumnMenu MultiSort FilterControls="MokaTableFilterControls.Chips">
    <MokaColumn Title="Customer" Field="o => o.Customer" Filterable />
    <MokaColumn Title="Region" Field="o => o.Region" Filterable />
    <MokaMoneyColumn Title="Total" Value="o => o.Total" Aggregate="MokaAggregateType.Sum" />
</MokaTable>
```

## Column groups

`MokaColumnGroup` draws a heading over several columns, in a row above theirs. Put the columns inside it.

```razor
<MokaTable Items="_orders" ColumnReorderable>
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumnGroup Title="Shipping">
        <MokaColumn Title="Carrier" Field="o => o.Carrier" />
        <MokaColumn Title="Shipped" Field="o => o.Shipped" Format="d" />
    </MokaColumnGroup>
    <MokaColumnGroup Title="Amounts" KeepTogether>
        <MokaMoneyColumn Title="Subtotal" Value="o => o.Subtotal" />
        <MokaMoneyColumn Title="Total" Value="o => o.Total" />
    </MokaColumnGroup>
</MokaTable>
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Title` | `string?` | -- | The heading's text |
| `ChildContent` | `RenderFragment?` | -- | The columns under the heading |
| `KeepTogether` | `bool` | `false` | A column of the group can only be dragged onto another of the group, and no other column onto one of them |
| `Order` | `int` | `0` | Where the group stands among the table's columns: lower first, and parts with the same `Order` keep the markup's order |
| `Class` | `string?` | -- | Classes on the heading's cell |

Columns that stand together under the same group share one heading, and columns outside any group get an empty cell in that row. A group whose columns are all hidden draws no heading. Pinning a column, or moving it out from among the others, splits the heading in two, one for each run of the group's columns. A table with no group draws no extra row.

## Saved views

`Views` draws named chips over the table, in the toolbar after the search box. A view is a name and a state: `new MokaTableView("Engineers", new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")] })`. Picking a chip puts its state in force, back on page 1, and raises `StateChanged`; the density and what is folded or opened stay as they were. A chip is lit while the table shows the same view (`SameView`), with the parts a view leaves null taken from the table's defaults. Its title says what it does: "Everything", or its filters, search and sort, from `Text.ViewSummary`.

"Save this view" keeps how the table looks now under the name typed, and raises `ViewSaved` with a new view whose state has page 1 and no density, folded groups or open rows. It's offered while the table shows something other than how it starts and no view shows it already. Adding the view to `Views` and storing it is the app's job.

```razor
<MokaTable Items="_people" Views="_views"
           ViewSaved="v => _views = [.. _views, v]"
           ViewForgotten="v => _views = _views.Where(x => x != v).ToList()">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Team" Field="x => x.Team" />
</MokaTable>

@code {
    List<MokaTableView> _views =
    [
        new("Everything", new MokaTableState()),
        new("Engineers", new MokaTableState { Filters = [new MokaTableFilter("Team", MokaFilterOperator.Is, "Engineering")] })
    ];
}
```

Without a `ViewSaved` handler there is no save button, and without a `ViewForgotten` handler chips can't be forgotten. A view of the table as it starts, such as "Everything", can never be forgotten. A name already taken is refused beside the box.

## Footer and aggregates

`Aggregate` adds a footer row with a label and a value under each column that sets it: `Sum`, `Average`, `Count`, `Min` or `Max`. `Count` counts the values that aren't `null`. The others use the values that are numbers, or number text in the invariant culture, and skip the rest, so they don't work on dates. `AggregateFormat` formats the result. It's `N2` when not set, and `Count` has no format. The labels are `Text.Sum`, `Text.Average` and the rest.

Aggregates cover the rows the table has loaded. With `Items` that's every row that passes the search and filters. With `ServerData` it's the current page, or every row once the pager is off, unless the source sends totals for the whole set. Under `Virtualize` the rows loaded are one window, so only the source's totals show: the request's `Aggregates` lists each column that totals something and how, as `MokaTableAggregate(Column, Type)`, and the result's `Totals` answers by column title. A column missing from `Totals` totals the rows the table has.

```csharp
async Task<MokaTableResult<Order>> LoadOrders(MokaTableRequest request)
{
    IQueryable<Order> query = Matching(_db.Orders, request);
    List<Order> items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
        .ToListAsync(request.CancellationToken);

    Dictionary<string, double> totals = [];
    if (request.Aggregates.Any(a => a is { Column: "Total", Type: MokaAggregateType.Sum }))
        totals["Total"] = (double)await query.SumAsync(o => o.Total, request.CancellationToken);

    return new MokaTableResult<Order>
    {
        Items = items,
        TotalItems = await query.CountAsync(request.CancellationToken),
        Totals = totals
    };
}
```

`Matching` stands for your own code that applies the search and the filters. A grouped table also writes each column's total on every group heading; see [Grouped rows](#grouped-rows).

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

`EmptyContent` is shown when there are no rows and no search, filter or quick filter is active. Without it the table says "No data available". A table whose `Items` is still `null` shows it too, so render the table once the data is there, or use `ServerData`.

`EmptyIcon`, `EmptyTitle` and `EmptyText` show a `MokaEmptyState` instead, which announces itself politely. Any one of them is enough. `Empty` is a fragment of your own, filling the row without padding, for when the empty state needs a button. `Empty` wins over `EmptyContent`, and `EmptyContent` over the three parameters.

```razor
<MokaTable Items="_devices" Loading="_loading"
           EmptyIcon="MokaIcons.Action.Search" EmptyTitle="No device matches"
           EmptyText="Try another zone, or clear the search.">
    <MokaColumn Title="Name" Field="x => x.Name" />
</MokaTable>
```

In the preview, click refresh to see the placeholder rows again, or search for a service that doesn't exist to see the [no results](#no-results) state.

```blazor-preview
@code {
    record Deployment(string Service, string Version);

    Deployment[] _deployments =
    [
        new("api", "2.4.1"),
        new("web", "1.9.0"),
        new("worker", "0.7.3"),
    ];

    async Task<MokaTableResult<Deployment>> LoadDeployments(MokaTableRequest request)
    {
        await Task.Delay(800, request.CancellationToken);
        List<Deployment> matches = _deployments
            .Where(d => string.IsNullOrEmpty(request.Search)
                        || d.Service.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
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
</MokaTable>
```

### No results

When a search, a filter or a quick filter is active and leaves no row, the table says so in place of the empty state: "No results for “report”." with a search, "No rows match the filters." without one, and a **Clear search and filters** button that clears the search, every filter and the quick filter. The message is a polite status region. `NoResults` replaces it with your own content, and `ClearSearchAndFiltersAsync()` does what the button does. `Empty`, `EmptyContent` and the empty state are for a table with no rows at all.

```razor
<MokaTable @ref="_table" TItem="Order" Items="_orders" Searchable>
    <ChildContent>
        <MokaColumn Title="Number" Field="o => o.Number" />
    </ChildContent>
    <NoResults>
        <MokaEmptyState Icon="MokaIcons.Action.Search" Title="No order matches" Compact>
            <MokaButton Size="MokaSize.Sm" OnClick="() => _table!.ClearSearchAndFiltersAsync()">Show every order</MokaButton>
        </MokaEmptyState>
    </NoResults>
</MokaTable>
```

### A failed load

When the latest `ServerData` call throws, the table shows the failure inside itself, in place of the rows: "Couldn't load the rows" (or the table's `ItemsName`), the exception's message and a **Try again** button, in a `role="alert"` region. The pager and the counts go back to zero, and `HasLoadFailed` is true until a load works. The failure doesn't reach the error boundary, and a request that a newer one replaced is never reported.

`OnLoadFailed` hears the exception, for logging. `ErrorContent` replaces the message with your own content. Its context is a `MokaTableFailure` with `Error`, the exception, and `Retry`, which loads again.

```razor
<MokaTable TItem="Order" ServerData="LoadOrders" ItemsName="orders" OnLoadFailed="Log">
    <ChildContent>
        <MokaColumn Title="Number" Field="o => o.Number" />
    </ChildContent>
    <ErrorContent Context="failure">
        <MokaAlert Severity="MokaToastSeverity.Error" Title="The order service didn't answer">
            <MokaButton Size="MokaSize.Sm" OnClick="failure.Retry">Try again</MokaButton>
        </MokaAlert>
    </ErrorContent>
</MokaTable>

@code {
    void Log(Exception error) => Logger.LogWarning(error, "Loading orders failed");
}
```

## Pagers

Without anything said, the table draws the numbered pager under the rows. A `MokaTablePager` among the columns says which pager to draw and how. It draws nothing where it stands.

```razor
<MokaTable Items="_orders" PageSize="25">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
    <MokaTablePager Look="MokaPagerLook.Entries" PageSizeOptions="[10, 25, 50]" />
</MokaTable>
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Look` | `MokaPagerLook` | `Numbered` | Which pager to draw. See the list below |
| `Position` | `MokaTablePaginationPosition` | `Bottom` | `Bottom`, `Top` or `Both`, for the numbered, compact, jump and cursor pagers. The entries pager always puts its page size above the rows and its buttons below; load more and endless sit under the rows |
| `Sticky` | `bool` | `false` | Keeps the pager in view at its edge while the page or the table scrolls. The table's wrapper stops clipping its corners while it's on |
| `PageSizeOptions` | `IReadOnlyList<int>` | `[10, 25, 50, 100]` | The page sizes someone can pick, in the numbered and entries pagers. Include `PageSize`, or the menu shows the wrong size |
| `ShowPageSize` | `bool` | `true` | Whether the numbered pager offers the page sizes |
| `ShowFirstLast` | `bool` | `true` | Whether the numbered pager has first and last page buttons |
| `ShowPageInfo` | `bool` | `true` | Whether the numbered pager says "1-10 of 200" |
| `MaxVisiblePages` | `int` | `5` | How many page numbers the numbered pager shows at most |

The seven looks:

| Look | What it draws |
|------|---------------|
| `Numbered` | The default: page numbers with first, previous, next and last, the page sizes and "1-10 of 200" under the rows, and a compact previous, "3 / 25", next above them with `Position="Top"` or `Both` |
| `Entries` | "Show [10] entries" above the rows, and "Showing 1 to 10 of 200 entries" with Previous and Next below |
| `Compact` | Only previous, "3 / 25" and next |
| `Jump` | Previous, "Page [3] of 25" with a box to type a page in, and next. A page out of range goes to the nearest one; anything that isn't a number puts the box back |
| `LoadMore` | The first page of rows, then "Showing 8 of 14", "Show 6 more" and "Show all" under them. See [Load more and endless scroll](#load-more-and-endless-scroll) |
| `Endless` | More rows load by themselves as the end of the rows scrolls into view, with a "Show 10 more" button for the keyboard |
| `Cursor` | Newer and Older, for servers that page by cursor and don't count the rows. See [Cursor paging](#cursor-paging) |

The numbered, entries, compact, jump and cursor pagers show while there is more than one page. They also stay while the user is past page 1, and while the rows would need more than one page at the `PageSize` you set, so a user who picked a size that fits every row can pick a smaller one again. Sorting, search, filters and a new page size take the table back to page 1. A pager part that arrives or changes look after someone paged starts again from the first page.

The pager part joins the table while the table's first render is drawn, and the table draws no pager until it knows its parts, which is the render right after, in the same batch. So the default numbered pager is never drawn and then replaced by the one the part names.

Under a `ServerData` source that leaves `TotalItems` null, the pagers say only what they know: the numbered pager offers a next page while a full page came back, draws no last page and says "21-30 of more", the entries pager says "Showing 21 to 30 of more entries", the jump pager "of more" and the compact pager "Page 3". See [Sources that don't count](#sources-that-dont-count).

The arrows ("First page", "Previous page", "Next page", "Last page") show their names in a `MokaTooltip` above a pager under the rows, below a pager above them, and keep it while greyed out. Every word a pager writes is in `Text.Pager`, a `MokaPaginationText`.

```blazor-preview
@code {
    record Reading(int Id, string Sensor);

    List<Reading> _readings = Enumerable.Range(1, 42)
        .Select(i => new Reading(i, $"Sensor {i % 5 + 1}"))
        .ToList();

    int[] _pageSizes = [5, 10, 25];
}

<MokaTable Items="_readings" PageSize="5">
    <MokaColumn Title="Id" Field="x => x.Id" />
    <MokaColumn Title="Sensor" Field="x => x.Sensor" />
    <MokaTablePager Position="MokaTablePaginationPosition.Both" PageSizeOptions="_pageSizes" />
</MokaTable>
```

```blazor-preview
<MokaTable Items="_readings" PageSize="5">
    <MokaColumn Title="Id" Field="x => x.Id" />
    <MokaColumn Title="Sensor" Field="x => x.Sensor" />
    <MokaTablePager Look="MokaPagerLook.Jump" />
</MokaTable>
```

`Sticky` keeps the pager in view while the page, or a scrolling parent, scrolls past a long table. A pager under the rows sticks to the bottom edge and one above them to the top edge.

```razor
<MokaTable Items="_orders" PageSize="100">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
    <MokaTablePager Position="MokaTablePaginationPosition.Both" Sticky />
</MokaTable>
```

### Load more and endless scroll

`LoadMore` draws the first `PageSize` rows in sort order, then "Showing 8 of 14", "Show 6 more" and "Show all" in the foot, in place of numbered pages. It wins over `ShowPagination="false"`.

```razor
<MokaTable Items="_devices" PageSize="8">
    <MokaColumn Title="Name" Field="x => x.Name" DefaultSort="MokaSortDirection.Ascending" />
    <MokaTablePager Look="MokaPagerLook.LoadMore" />
</MokaTable>
```

With `ItemsName` the foot counts what the rows are: "50 of 312 devices", "Show 50 more" and "Show all 312".

A sort or new rows keep what was asked for, and a new `PageSize` from the parent starts over. Under `ServerData` the table asks for page 1, one page size larger each time, and the rows on screen stay while more load. "Show all" asks for the whole set the way `ShowPagination="false"` does. When the foot goes with the last rows, focus moves to the first new row.

`Endless` loads the next page size of rows by itself once the end of the rows comes into view. Every table on the page shares one `IntersectionObserver`, scrolling sends nothing to .NET, and the observer tells the table once, then waits until the rows it asked for have arrived before it watches again, so a load that adds nothing can't ask over and over. The foot keeps the count and a "Show 10 more" button, so the keyboard and a failed load still have a way on.

A page that loads more rows because someone asked for them, such as older log entries added to the same list, calls `ShowAtLeastAsync` so they are drawn. It reads the list again too, which the table can't notice when the list changes in place.

```razor
<MokaTable @ref="_table" TItem="LogEntry" Items="_entries" PageSize="100">
    <MokaColumn Title="Time" Field="e => e.Time" />
    <MokaTablePager Look="MokaPagerLook.Endless" />
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

### Cursor paging

A server that pages by cursor hands out an opaque token for the next page and the previous one, and often doesn't count the rows. The cursor pager draws Newer and Older. The table puts the cursor in `MokaTableRequest.Cursor`: null for the newest page, then the `NextCursor` your last answer gave for Older, or its `PreviousCursor` for Newer. A null `NextCursor` greys out Older, and a null `PreviousCursor` greys out Newer. `TotalItems` can stay null.

```csharp
async Task<MokaTableResult<Event>> LoadEvents(MokaTableRequest request)
{
    EventPage page = await _events.ReadAsync(after: request.Cursor, take: request.PageSize, request.CancellationToken);
    return new MokaTableResult<Event>
    {
        Items = page.Events,
        NextCursor = page.OlderCursor,
        PreviousCursor = page.NewerCursor
    };
}
```

```razor
<MokaTable TItem="Event" ServerData="LoadEvents" PageSize="50">
    <MokaColumn Title="Time" Field="e => e.Time" />
    <MokaColumn Title="Message" Field="e => e.Message" />
    <MokaTablePager Look="MokaPagerLook.Cursor" />
</MokaTable>
```

A sort, search or filter goes back to the newest page. The cursor isn't part of the state, since cursors expire. With `Items` the cursor pager steps through pages, Newer for the page before and Older for the page after.

## Densities

Rows come in three spacings, `MokaTableDensity.Compact`, `Standard` and `Comfortable`. `Compact` is the tight table Moka has always drawn, and the default on every look but `Airy`, which starts `Comfortable`. `Density` sets the starting density, and the density someone picks is part of the state. The theme can set the density of every table that doesn't say: see [App-wide look and density](#app-wide-look-and-density).

`ShowDensityToggle` adds a toolbar button named "Density" that opens a small menu with the three, as radio buttons, in the top layer like the column menu. A pick spaces the rows at once and raises `StateChanged`.

```blazor-preview
<MokaTable Items="_people" Title="People" ShowDensityToggle Density="MokaTableDensity.Standard">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

On the new design `Compact` is the theme's own cell padding, so the theme's density still decides how tight that is; `Standard` and `Comfortable` are a step and two steps roomier.

## Looks

`Look` picks how the rows are drawn. Without one the table takes the theme's `Tables`, else `Lined`, the table as it has always looked, whose markup doesn't change.

| Look | What it draws |
|------|---------------|
| `Lined` | A line under each row |
| `SoftStripes` | Every other row softly tinted, with no lines between rows |
| `Cards` | Each row its own card, with a gap between them, on the page's background |
| `Sheet` | Boxed cells, a column of row numbers, figures in monospace and tight rows, like a spreadsheet |
| `Airy` | Roomy rows with no lines and quiet headings, for a few rows people read. `Comfortable` unless a density is set |
| `Framed` | A bordered panel with a tinted band behind the headings |
| `List` | No heading row: each row two lines from the columns' phone slots, at every width |

```blazor-preview
<MokaTable Items="_people" Look="MokaTableLook.Sheet">
    <MokaColumn Title="Name" Field="x => x.Name" />
    <MokaColumn Title="Age" Field="x => x.Age" Align="MokaTextAlign.Right" />
    <MokaColumn Title="City" Field="x => x.City" />
</MokaTable>
```

Every look is drawn with the theme's tokens on the new design. The original design, `MokaTheme.Dark` and `Light`, draws `Lined` whatever is asked, with no row numbers either, so an app on it doesn't change. `List` is the exception: it is markup rather than paint, the phone list at every width, so it works on both designs.

The sheet's row numbers count from the page's first row and are hidden from screen readers. The list look uses the columns' `Phone` slots; with none named, the first column is the first line and the others share the second. Below 720 px it is the phone list itself (see [On a phone](#on-a-phone)).

### App-wide look and density

`MokaTheme.WithTables(look)` sets the look of every table that doesn't set its own, and `WithTableDensity(density)` the density of every table whose state and `Density` don't say. A table resolves `Look`, else the theme's `Tables`, else `Lined`; and the state's density, else `Density`, else the theme's `TableDensity`, else the look's own.

```csharp
MokaTheme theme = MokaTheme.Red
    .WithTables(MokaTableLook.SoftStripes)
    .WithTableDensity(MokaTableDensity.Standard);
```

Both are style choices of the new design: on `MokaTheme.Dark` and `Light` they return the same theme and tables keep drawing `Lined`. `MokaTableLook` and `MokaTableDensity` live in the Core package so the theme can name them. Their namespace is still `Moka.Red.Data.Table`, and the table package forwards them, so code built against an earlier version keeps working.

## Words

`Text` takes a `MokaTableText` with every word the table, its pager and its column kinds write. Replace any of them with `with`:

```razor
<MokaTable Items="_orders" Searchable
           Text="@(MokaTableText.Default with
           {
               SearchPlaceholder = "Suchen...",
               NoData = "Keine Bestellungen",
               Pager = MokaPaginationText.Default with { NextPage = "Weiter", PreviousPage = "Zurück" }
           })">
    <MokaColumn Title="Nummer" Field="o => o.Number" />
</MokaTable>
```

A phrase built from values is a function, such as `SearchResults` (`count => $"{count} results"`) or `ShowingCount` (shown, total and the table's `ItemsName`). A phrase about the rows takes `ItemsName`, or null when the table has none. Keep one `MokaTableText` instance in a field or a static: a new instance on every render is a new value, though nothing reloads for it.

The words added in 0.6.0, by where they show:

| Where | Words |
|-------|-------|
| No results | `NoResultsFor` (from the search, or null), `ClearSearchAndFilters` |
| A failed load | `LoadFailed` (from `ItemsName`), `Retry` |
| Column menu | `ColumnMenu` (from the title), `SortAscending`, `SortDescending`, `ClearSort`, `FilterColumn`, `PinToStart`, `PinToEnd`, `Unpin`, `GroupByColumn`, `HideColumn`, `FitToContent` |
| Group bar | `Grouping`, `GroupedBy`, `StopGrouping` (from the title), `ExpandAllGroups`, `CollapseAllGroups` |
| Expand all | `ExpandAllRows`, `CollapseAllRows` |
| Filters | `OperatorName`, `FilterSummary`, `FilterOnColumn`, `FilterDialog`, `RangeFrom`, `RangeTo`, `FilterFrom`, `FilterTo`, `Operator`, `Value`, `Apply`, `ClearFilter`, `NoValues`, the chips', builder's, quick filters' and facets' words. See [Table Filters](/components/table-filters#words) |
| Pager | `RangeOpen`, `PageOpen`, `ShowingEntriesOpen`, `OfMorePages` in `Text.Pager`, for a source that doesn't count. See [Pagination](/components/pagination#words) |
| Columns with no title | `ColumnNumber` (from the place, in the column toggle and an editor's name), `ColumnMenuAt` (the menu button's name). A column kind names itself, such as the actions column's `Actions` |
| Editing | `AddRow`, `EditRow`, `SaveRow`, `CancelEdit`, `DeleteRow`, `DeleteRowTitle`, `DeleteRowMessage`, `Delete`, `InvalidValue`, `Saved`, `Undo`, `DismissSaved`, `UndoFailed`, `Pasted`. See [Table Editing](/components/table-editing) |
| Export and copying | `ExportCsv`, `ExportExcel`, `SheetName`, `CopyRows`, `CopiedRows`, `CopyFailed`. See [Table Export](/components/table-export) |
| Loading | `StreamingRows`, `StopLoading`, `LoadingRow` |

## Server-side data

`ServerData` receives a `MokaTableRequest` and returns a `MokaTableResult<TItem>`. The table calls it when it first loads, after every sort, page, page size, search, filter, quick filter and grouping change, on refresh, from `ReloadAsync()`, when the parent passes a new `PageSize` or `ShowPagination`, and when the parent passes a state that says something new. A state that `@bind-State` hands back after the table raised it isn't new, so a header click makes one call. The table filters, sorts and pages nothing itself, so the callback has to apply the whole request.

```csharp
async Task<MokaTableResult<Order>> LoadOrders(MokaTableRequest request)
{
    var query = _db.Orders.AsQueryable();

    if (!string.IsNullOrEmpty(request.Search))
        query = query.Where(o => o.Reference.Contains(request.Search));

    foreach (MokaTableFilter filter in request.Filters)
    {
        query = (filter.Column, filter.Operator) switch
        {
            ("Reference", MokaFilterOperator.Contains) => query.Where(o => o.Reference.Contains(filter.Value!)),
            ("Status", MokaFilterOperator.Is) => query.Where(o => o.Status == filter.Value),
            ("Amount", MokaFilterOperator.AtLeast) => query.Where(o => o.Amount >= Amount(filter.Values[0])),
            ("Amount", MokaFilterOperator.AtMost) => query.Where(o => o.Amount <= Amount(filter.Values[0])),
            ("Amount", MokaFilterOperator.Between) =>
                query.Where(o => o.Amount >= Amount(filter.Values[0]) && o.Amount <= Amount(filter.Values[1])),
            _ => query
        };
    }

    if (request.Sorts is [{ Column: "Amount" } sort, ..])
        query = sort.Direction == MokaSortDirection.Ascending
            ? query.OrderBy(o => o.Amount)
            : query.OrderByDescending(o => o.Amount);

    int total = await query.CountAsync(request.CancellationToken);
    var items = await query
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .ToListAsync(request.CancellationToken);

    return new MokaTableResult<Order> { Items = items, TotalItems = total };
}

static decimal Amount(string text) => decimal.Parse(text, CultureInfo.InvariantCulture);
```

```razor
<MokaTable TItem="Order" ServerData="LoadOrders" Title="Orders" Searchable>
    <MokaColumn Title="Reference" Field="x => x.Reference" Filterable />
    <MokaColumn Title="Amount" Field="x => x.Amount" Format="C2" Align="MokaTextAlign.Right" Filterable
                Aggregate="MokaAggregateType.Sum" AggregateFormat="C2" />
    <MokaColumn Title="Status" Field="x => x.Status" Filterable FilterType="MokaColumnFilterType.Select" />
</MokaTable>
```

This source applies every filter as one more `Where`, which is `FilterJoin.All`. A table with the filter builder can ask for `Any`, where a row needs to pass one filter; see [Server-side filtering](/components/table-filters#server-side-filtering).

`MokaTableRequest` carries:

| Property | Type | Description |
|----------|------|-------------|
| `Page` | `int` | Page number, starting at 1 |
| `PageSize` | `int` | Rows per page. While the pager is off, `Page` is 1 and `PageSize` covers the whole result set |
| `StartIndex` | `int` | The first row asked for, from 0: `(Page - 1) * PageSize` for a page, or where a window starts. Read rows by this and `PageSize` |
| `Search` | `string?` | Text in the search box, or `null` when there is none |
| `Sorts` | `IReadOnlyList<MokaTableSort>` | Every sorted column, first sort first, each a `MokaTableSort(Column, Direction)` naming the column by `Title`. Each call gets its own list, which later sorts don't change |
| `Filters` | `IReadOnlyList<MokaTableFilter>` | The filters in force, typed: see [Typed filters](/components/table-filters#typed-filters). The filter row sends `Contains` for text, `AtLeast`, `AtMost` or `Between` for a range, `Is` for a single list or yes and no, and `IsAnyOf` for a list of several; the facets send `IsAnyOf` too. The menus, the builder and a state can use any operator |
| `FilterJoin` | `MokaFilterJoin` | How the filters combine: `All`, the default, or `Any` |
| `QuickFilter` | `MokaTableQuickFilter?` | The quick filter picked, or null. Its filters narrow on top of `Filters`, whatever `FilterJoin` says |
| `Groups` | `IReadOnlyList<string>` | The titles of the columns the rows are grouped by, outermost first. Order the rows by them before the sorts. See [Grouped rows](#grouped-rows) |
| `Aggregates` | `IReadOnlyList<MokaTableAggregate>` | The totals the footer and the group headings show, each a `MokaTableAggregate(Column, Type)`. Empty when no column totals anything |
| `Facets` | `IReadOnlyList<string>` | Under `FacetsFromServer`, the columns whose values the table wants with counts. See [Facets from the server](/components/table-filters#facets-from-the-server) |
| `QuickFilterCounts` | `IReadOnlyList<MokaTableQuickFilter>` | Under `FacetsFromServer`, the quick filters to count |
| `Cursor` | `string?` | Under the cursor pager, the cursor for the page wanted. See [Cursor paging](#cursor-paging) |
| `RowsOnly` | `bool` | True when the table asks for more rows of a result it already has the count, totals, group totals and facets of: a later window, or the rows a run of picks reaches. Answer with `Items` alone. See [Rows a server sends window by window](#rows-a-server-sends-window-by-window) |
| `CancellationToken` | `CancellationToken` | Cancelled once the table has asked for something newer, or has gone away. Pass it on to whatever fetches the rows |

`MokaTableResult<TItem>` hands back:

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<TItem>` | The rows of the requested page. Required |
| `TotalItems` | `int?` | The row count across all pages, which drives the pager. Null when the source doesn't count: see [Sources that don't count](#sources-that-dont-count) |
| `NextCursor`, `PreviousCursor` | `string?` | For the cursor pager |
| `Totals` | `IReadOnlyDictionary<string, double>?` | Totals over the whole result set, by column title, for the footer. See [Footer and aggregates](#footer-and-aggregates) |
| `GroupTotals` | `IReadOnlyList<MokaTableGroupTotals>?` | The row count and totals of each whole group, for the group headings. See [Grouped rows](#grouped-rows) |
| `Facets` | `IReadOnlyDictionary<string, IReadOnlyList<MokaFacetValue>>?` | Every value of each column asked for in `Facets`, with counts |
| `QuickFilterCounts` | `IReadOnlyDictionary<string, int>?` | How many rows each quick filter asked for keeps, by name |

When a narrower result leaves the current page past the end, the table asks again for the last page. The table ignores the answer to a replaced request, failure included, so the token only saves the work. The loading state shows while the call runs (see [Loading and empty states](#loading-and-empty-states)), and a failure of the latest call shows in the table (see [A failed load](#a-failed-load)). Without `Totals`, `Facets` and `GroupTotals`, the footer, the list filters and the group headings only see the rows of the current page, and under [windows](#rows-a-server-sends-window-by-window) the footer and the headings show no totals at all.

### The first request

Columns register with the table while its first render is drawn. A `ServerData` table waits for that render, through a `Task.Yield`, before its first request, so the columns' `DefaultSort` goes out with it and no second request is needed. Rows in memory load at once and are in the first render.

The rows of a `ServerData` table arrive after the first render, so a bUnit test waits for them:

```csharp
IRenderedComponent<OrdersPage> page = Render<OrdersPage>();
page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("tr[data-row-index]")));
```

### Sources that don't count

A source that leaves `TotalItems` null pages honestly. While a full page comes back, the numbered pager offers a next page, draws no last page and says "21-30 of more". A page with fewer rows than `PageSize` is the last, and an empty page past page 1 steps back a page. The table passes this to `MokaPagination` as [`HasMore`](/components/pagination#sources-that-dont-count), and the entries, jump and compact pagers say "of more" in their own words.

```csharp
async Task<MokaTableResult<LogLine>> LoadLines(MokaTableRequest request)
{
    List<LogLine> lines = await _log.ReadAsync(skip: (request.Page - 1) * request.PageSize, take: request.PageSize,
        request.CancellationToken);
    return new MokaTableResult<LogLine> { Items = lines };
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

`Virtualize` over `Items` saves rendering, not loading: every row is in memory and only those in view are drawn. With `ServerData` the table loads window by window instead; see the next section.

## Rows a server sends window by window

With `ServerData` and `Virtualize` the table asks for the rows as they scroll into view, the way Blazor's `Virtualize` asks an `ItemsProvider`: each request names `StartIndex` and `PageSize` for the window, along with the search, sorts and filters, and the source answers with those rows and `TotalItems`, which sizes the scroll. A placeholder row stands in for each row still on its way, and the first window waits under placeholder rows. When the view scrolls on before a window arrives, its request's `CancellationToken` is cancelled; pass it on to whatever reads the rows. A sort, a filter or a search asks again from the top. The pager is hidden, and the table holds only the window last loaded. A source that doesn't count grows the scroll a window at a time while full windows come back.

The first window after a reload asks for the whole picture: the count, the totals, the group totals and the facets. The windows after it carry `RowsOnly`, and the table keeps what it has, so the source can skip the counting, which is often what a query spends most on:

```razor
<MokaTable ServerData="LoadWindowAsync" Virtualize Height="600px" ItemKey="o => o.Id">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
</MokaTable>

@code {
    async Task<MokaTableResult<Order>> LoadWindowAsync(MokaTableRequest request)
    {
        IQueryable<Order> query = Orders.Apply(request.Search, request.Filters, request.Sorts);
        List<Order> rows = await query.Skip(request.StartIndex).Take(request.PageSize).ToListAsync(request.CancellationToken);
        if (request.RowsOnly)
            return new() { Items = rows };

        return new() { Items = rows, TotalItems = await query.CountAsync(request.CancellationToken) };
    }
}
```

`StartIndex` is on every request: `(Page - 1) * PageSize` for a page, so one source serves both. Whatever a `RowsOnly` answer sends anyway, a count or totals, replaces what the table has.

The table holds one window, so it never totals rows itself here: the footer and the group headings show the source's `Totals` and `GroupTotals` (see [Footer and aggregates](#footer-and-aggregates)), and nothing where the source sent none. The list filters and the facet panel show the source's values under `FacetsFromServer`; without it they gather the values of every window loaded since the last reload, so a list grows as rows scroll into view rather than changing with each window, but it lists only what has been seen.

The scroll is as tall as every row together, `TotalItems` times the row height. Browsers cap how tall an element can be, about 33.5 million pixels in Chromium and Safari and 17.9 million in Firefox, so at the default row height a table that scrolls through every row reaches about 900,000 rows in Chromium and Safari and 480,000 in Firefox. Rows past the cap can't be scrolled to, and near it the window keeps asking for rows that never show. For more rows, page the table, or narrow the set with a search or filters first.

### Every matching row

Without more, the select-all box picks the rows loaded, which under windows is the one window. `SelectAllMatching` makes it pick every row that matches the search, the filters and the quick filter, the rows the table never loaded included. It keeps the selection the way data grids over server-side rows do: a flag for every matching row, and the rows unticked since. Unticking a row leaves it out, ticking it again puts it back, and every row in a window that scrolls in shows picked unless it was unticked. The selection bar and the bulk bar count the source's rows less those unticked.

`SelectedItems` holds only the rows picked one by one, so read what is picked from `SelectionChanged` or `CurrentSelection()`, a `MokaTableSelection<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `AllMatching` | `bool` | Whether every matching row is picked, but for `Except` |
| `Items` | `IReadOnlyList<TItem>` | The rows picked one by one. Empty while `AllMatching` |
| `Except` | `IReadOnlyList<TItem>` | Under `AllMatching`, the rows unticked since, as the table last loaded them |
| `Count` | `int?` | How many rows are picked. Null when every matching row is picked from a source that doesn't count |
| `Matching` | `MokaTableRequest?` | Under `AllMatching`, the request every picked row matches, from the first row and sized to the count |

Act on it the way the source reads rows, without the paging:

```razor
<MokaTable ServerData="LoadWindowAsync" Virtualize Height="600px" ItemKey="o => o.Id" Selectable SelectAllMatching
           SelectionChanged="s => _picked = s">
    <SelectionActions>
        <MokaButton OnClick="ArchiveAsync">Archive</MokaButton>
    </SelectionActions>
    <ChildContent>
        <MokaColumn Title="Number" Field="o => o.Number" />
    </ChildContent>
</MokaTable>

@code {
    MokaTableSelection<Order>? _picked;

    async Task ArchiveAsync()
    {
        if (_picked is { AllMatching: true, Matching: { } request })
        {
            HashSet<int> kept = [.. _picked.Except.Select(o => o.Id)];
            await Orders.Apply(request.Search, request.Filters, request.Sorts)
                .Where(o => !kept.Contains(o.Id))
                .ExecuteUpdateAsync(o => o.SetProperty(x => x.Archived, true));
        }
        else if (_picked is not null)
        {
            await Orders.ArchiveAsync(_picked.Items.Select(o => o.Id));
        }
    }
}
```

The rows unticked are known by `ItemKey`, else by the item itself, so give a class without its own `Equals` an `ItemKey`. A new search, filter or quick filter clears the selection, since what matched has changed; a sort keeps it. A plain click on a row under `SelectOnRowClick` picks that row alone, and passing a new `SelectedItems` ends it too. Copying rows copies the rows picked one by one, so under `AllMatching` the copy button isn't shown.

### Runs past the window

Shift-click and Shift+Space pick a run from the last box touched, also when that box has scrolled out of the window. The rows the window holds take the state at once, and the table asks the source for the rest, one `RowsOnly` request per stretch of rows the window leaves out, with `StartIndex` and `PageSize` covering it. A newer run, a reload or the table going away cancels the request. A run of 50,000 rows loads 50,000 rows into the selection, so for a whole result use `SelectAllMatching`.

### Keys

The keys reach rows that aren't loaded. Page Up and Page Down move a view at a time, and Ctrl+Home and Ctrl+End go to the first and last row: the table scrolls to the row and focuses its cell once its window has come and drawn it. An arrow onto a row still on its way, shown as a placeholder or past the rows drawn, waits for it the same way. A key waits up to five seconds for its row, and a newer key or a click elsewhere replaces the wait.

### Grouped windows

A grouped table windows too, through lines: each group's heading is a line and each row is a line. The request's `Groups` names the grouped columns, and the source orders the rows by them before the sorts, as it does for pages. With the first window it sends `GroupTotals` for every group of the whole result, at every level, in the order the rows come, each with its row count. From those the table works out where each group's rows start, so it knows which rows a window of lines needs, and it asks only for those, with `StartIndex` and `PageSize` in rows. A folded group leaves its rows out of the lines and out of the requests: folding or opening a heading draws the window again from the rows it holds and asks only for rows it hasn't seen.

```csharp
if (!request.RowsOnly && request.Groups is ["Region"])
{
    groupTotals = await query.GroupBy(o => o.Region).OrderBy(g => g.Key)
        .Select(g => new MokaTableGroupTotals(new[] { g.Key }, g.Count()))
        .ToListAsync(request.CancellationToken);
}
```

Group totals that don't add up to `TotalItems`, leave a group out, list a group twice or don't match the first window's rows can't place the rows, so that grouping pages instead, with headings over each page as under [Grouping on the server](#grouping-on-the-server), until the grouping changes. A heading's totals are its group's `Totals`, and its `MokaTableGroup.Items` are the group's rows in the window. Up to 0.8.1 a grouped table with `ServerData` and `Virtualize` drew its rows window by window without headings.

## Streamed rows

`ItemStream` takes an `IAsyncEnumerable<TItem>`, such as the results of a long query or a log being read. Rows show as they arrive, gathered into one render a frame rather than one per row, with a row under them saying how many have come and a Stop button. The search, filters, sort and pager work on what has arrived, and rows that need none of them join the end without running through the pipeline again. A new stream starts over and the table cancels the old one, through `WithCancellation`, so an iterator marks its token `[EnumeratorCancellation]`. A stream that throws keeps the rows that came, still searched, filtered, sorted and paged, with the failure and Try again under them, which reads the stream again from the start. A stream that throws before any row came shows the failure in their place. `OnLoadFailed` and `ErrorContent` cover a stream as they do `ServerData`. `StopStreaming()` and `IsStreaming` do it from code. While rows arrive the grid says its row count is unknown.

```razor
<MokaTable ItemStream="ReadLogAsync()" Height="480px" Virtualize ShowPagination="false">
    <MokaColumn Title="Time" Field="e => e.Time" Format="HH:mm:ss" />
    <MokaColumn Title="Message" Field="e => e.Message" />
</MokaTable>
```

Keep the stream in a field rather than calling the method in the markup when the parent renders often: a new stream starts over.

## Columns in view

`VirtualizeColumns` draws only the cells of the columns in view, for tables of about 50 columns or more. The header keeps every heading, a row of cheap cells; `moka-table.js` measures their places once and again when a size changes, and scrolling sideways tells .NET the columns in view, with three beyond each edge, only when that set changes. Each row draws those columns, the pinned ones, and one empty cell spanning each run of columns out of view. The columns are laid out at fixed widths, each column's `Width` or 10rem, so rows stay lined up as cells come and go. The table has to scroll sideways in its own box, as it does with `Responsive` on (the default) or a `MinWidth`. The arrow keys scroll to a column out of view and focus its cell once it is drawn. In a grouped table a heading with totals draws the totals of the columns in view, and an empty cell over each run out of view.

## Grouped rows

`GroupBy` names a column by its `Title`. Rows that show the same text in that column, or the same `GroupKey` when the column has one, are shown together under a collapsible heading that spans the table. The column can be hidden, so a table can group by something it doesn't show as a column, and a column with a `GroupKey` needs no `Field`: the key alone is enough.

```razor
<MokaTable Items="_tasks" GroupBy="Status">
    <MokaColumn TItem="TaskItem" Title="Name" Field="@(t => t.Name)" />
    <MokaColumn TItem="TaskItem" Title="Owner" Field="@(t => t.Owner)" />
    <MokaColumn TItem="TaskItem" Title="Status" Field="@(t => t.Status)" Visible="false" />
</MokaTable>
```

Grouping gathers; it does not reorder. A group appears where its first row landed and keeps its rows in the order they already had, so the sort and the filters still decide the order. Group by a column the rows are not sorted by and you get each group once, where it first appeared, rather than one heading per run.

### Nested groups

The grouping is part of the state as `Groups`, a list of titles, outermost first, so groups nest: `Groups = ["Region", "Status"]` puts a heading per status inside each region, indented one step per level. `GroupBy` is the one level a table starts with when the state doesn't say. With `ColumnMenu`, "Group by this column" adds a level inside the others, and picking it again takes the level away.

```razor
<MokaTable Items="_orders" ColumnMenu State="@(new MokaTableState { Groups = ["Region", "Status"] })">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Region" Field="o => o.Region" />
    <MokaColumn Title="Status" Field="o => o.Status" />
    <MokaMoneyColumn Title="Total" Value="o => o.Total" Aggregate="MokaAggregateType.Sum" />
</MokaTable>
```

A grouped table with `ColumnMenu` shows a group bar above the rows: "Grouped by", a chip per level with a button that stops grouping by it, and **Expand all** and **Collapse all** while groups can collapse. From code, `ExpandAllGroupsAsync()` opens every group and `CollapseAllGroupsAsync()` folds every group at every level.

### Headings, counts and totals

A heading shows its key and how many rows the whole group holds. Under every column with an `Aggregate` it writes the group's total, formatted as the footer formats it.

For rows in memory the groups are built across every page, so a group's count and totals cover all its rows. The pager counts headings and rows alike, as lines: a page of 10 holds 10 lines. A page that starts inside a group draws that group's headings again first, so a row never shows without the groups it belongs to.

### Grouping on the server

With `ServerData` the request's `Groups` names the grouped columns. Order the rows by them before the sorts, so each group's rows come together across pages. Without more, a heading counts and totals the group's rows on the page. With `Virtualize` the headings and rows come window by window instead; see [Grouped windows](#grouped-windows). Send `GroupTotals` for whole groups: one `MokaTableGroupTotals(Path, Count)` per group, `Path` being its keys outermost first, with `Totals` by column title.

```csharp
List<MokaTableGroupTotals> groupTotals = await query
    .GroupBy(o => o.Region)
    .Select(g => new MokaTableGroupTotals(new[] { g.Key }, g.Count())
    {
        Totals = new Dictionary<string, double> { ["Total"] = (double)g.Sum(o => o.Total) }
    })
    .ToListAsync(request.CancellationToken);
```

A key is what the column groups by: its `GroupKey`, else its text as the cell shows it. Send paths written the same way.

### The group record

`GroupHeaderTemplate` replaces what a heading shows. Its context is a `MokaTableGroup<TItem>`:

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | The group's own key |
| `Count` | `int` | How many rows the whole group holds: across every page for rows in memory, as `GroupTotals` say under a server source, else the rows on this page |
| `Collapsed` | `bool` | Whether the group's rows are hidden |
| `Items` | `IReadOnlyList<TItem>` | The group's rows the table has, in order: every row of the group for rows in memory |
| `Level` | `int` | How deep the group sits: 0 for the outermost groups |
| `Path` | `IReadOnlyList<string>` | The group's keys and its parents', outermost first |
| `Id` | `string` | The keys of `Path` joined by `MokaTableGroup<TItem>.IdSeparator`, U+001F: how the state names the group |
| `Totals` | `IReadOnlyDictionary<string, string>` | The group's totals by column title, written as the footer writes them |

```razor
<GroupHeaderTemplate Context="group">
    <strong>@group.Key</strong>
    <MokaText Mono>@group.Count</MokaText>
    @if (group.Totals.TryGetValue("Total", out string? total))
    {
        <MokaCaption>@total</MokaCaption>
    }
</GroupHeaderTemplate>
```

The groups someone folded away are in the state's `CollapsedGroups`, by `Id`. `InitiallyCollapsedGroups` names them the same way, and `OnGroupToggled` reports the `Id`. A group of the first level is named by its key alone, as before.

Rows in memory don't combine grouping and `Virtualize`: grouping wins, because a virtualized list of rows has no room for headings between them. A `ServerData` source windows headings and rows together; see [Grouped windows](#grouped-windows).

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
<MokaCard Title="Latest sign-ins">
    <MokaTable Items="_signIns" Bleed>
        <MokaColumn Title="Address" Field="x => x.Address" Mono />
    </MokaTable>
</MokaCard>
```

## On a phone

A table stays a table that scrolls sideways until one of its columns names a `Phone` slot. Then, below 720 px, each row becomes a list item and the header goes out of sight. The `List` look draws the same list at every width (see [Looks](#looks)).

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

## Accessibility

The table follows the WAI-ARIA grid pattern. The `table` is a `grid`, or a `treegrid` while its rows are grouped, with headings at `aria-level` 1 and rows one level below each heading's own. It says how many rows and columns the whole result set has (`aria-rowcount`, `aria-colcount`), headings included, and every drawn row and cell says where it stands (`aria-rowindex`, `aria-colindex`), so the numbers stay right on page 3, under `Virtualize`, with rows loaded window by window and with columns drawn only in view. An open detail row counts as a row. A source that doesn't count, and a stream still arriving, give `aria-rowcount="-1"`, which means unknown. A selectable table is `aria-multiselectable` and each row says `aria-selected`; with `CellRanges` the marked cells do. The row numbers of the sheet look are hidden from screen readers and don't count as a column. Without groups or open details every number is worked out from the row's place, so a table of a million rows pays nothing per render for them.

## Keyboard

The data cells form a grid with one tab stop: the first cell until a cell takes focus, then the last cell that had it. When a page change, a filter or a hidden column takes that cell away, the tab stop moves to the nearest cell left. With `Virtualize`, while that cell's row is scrolled out of the rendered rows, the first rendered cell stands in for it. Tab moves into the grid and out again, and buttons, checkboxes and links inside the rows keep their own place in the tab order. Each sortable header's button is a tab stop of its own, before the grid, and so are a heading's menu and filter buttons.

| Key | Action |
|-----|--------|
| Arrow keys | Move to the neighbouring cell. They stop at the edges |
| Home / End | Move to the first or last cell of the row |
| Page Up / Page Down | Move up or down by as many rows as the scroll area shows. Under `Virtualize` the rows are drawn first |
| Ctrl+Home / Ctrl+End | Move to the first cell of the first row, or the last cell of the last row |
| Enter / F2 | On an editable cell, start editing. Enter on any other cell of a row with `OnRowClick` raises `OnRowClick` |
| Enter / Escape in an editor | Save, or put the value back. See [Table Editing](/components/table-editing#cell-and-row-modes) |
| Tab / Shift+Tab in a cell editor | Save, and open the next or the previous editable cell |
| Ctrl+Z | Undo the last save |
| Shift+arrows | With `CellRanges`, mark a block of cells from the focused one. A plain arrow or Escape lets go |
| Ctrl+C | Copy the marked block, else the picked rows with `CopySelectedRows`, else the cell with `CellRanges` |
| Ctrl+V | With `CellRanges`, paste a block into the editable cells |
| Space | Toggle the row's selection when the table is `Selectable` |
| Shift+Space | Pick the run from the last box touched to this row |
| Left / Right / Home on a heading's right edge | Change the column's width by 16 px, or give it its own back. The edges take focus only in a table whose arrangement is kept. See [Arranging columns](#arranging-columns) |
| Context-menu key / Shift+F10 | Raise `OnRowContextMenu` for the row, when it is set |
| Enter / Space on a header's button | Sort by that column, like a click. With Shift held, add the column to a `MultiSort` sort |
| Enter / Space on a heading's menu button | Open the [column menu](#column-menu) with focus on its first item |
| Arrows, Home, End, a letter in the column menu | Move between the items. Enter or Space picks one |
| Escape / Tab in the column menu | Close it, with focus back on its button |
| Escape in a filter popup | Close it, with focus back on what opened it. See [Table Filters](/components/table-filters#keyboard) |

A table inside another table's detail row keeps its keys to itself: Enter and the arrows there act on the inner table only.

Enter raises `OnRowClick` by clicking the focused cell, so the keyboard takes the same path as a mouse click on that row. Only a key pressed on the cell itself counts: Enter on a button inside a cell still presses that button, and arrows in a text box inside a cell stay in the box. An editable cell keeps Enter for editing, where Enter saves the value and Escape drops it, and both return focus to the cell.

While a cell is being edited the grid keys are off: the arrows move the caret in the editor, and Tab or a click elsewhere saves the value. Keys pressed in a control inside a `CellTemplate`, such as a text box or a button, stay with that control, so the arrows don't move to another cell, Enter doesn't start an edit and Space doesn't select the row. Moving rows, and dragging a header to move its column, have no keyboard equivalent: the chooser's buttons move columns.

```razor
<MokaTable TItem="Order" Items="_orders" OnRowClick="OpenOrder">
    <MokaColumn Title="Reference" Field="x => x.Reference" />
    <MokaColumn Title="Notes" Field="x => x.Notes" Editable />
</MokaTable>
```

Up to 0.1.12 `OnRowClick` could only be raised with the mouse, and closing the editor with Enter or Escape left focus nowhere, so the arrow keys stopped working. Keys pressed in a control inside a `CellTemplate` reached the cell as well.

## Context menu

`OnRowContextMenu` receives a `MokaItemContextMenuArgs<TItem>` with the row's `Item` and the `MouseEvent`. While a handler is set, right-clicking a row doesn't open the browser's menu, and the context-menu key or Shift+F10 on a focused cell raises it too. Pass the event to `IMokaContextMenuService.Show` to open the shared menu at the pointer. The layout needs a `MokaContextMenuHost` (see [Context Menu](context-menu)). For a button that opens the same menu in every row, use the [row menu column](/components/table-columns).

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

`ItemKey` gives each row an identity. The table uses it as the row's `@key`, so when sorting, filtering or a reload moves a row, Blazor moves the row's DOM and component state with it instead of rebuilding it. It also records which rows are expanded, by the key's invariant text in the state, and a picked row whose data changed stays picked. Without `ItemKey` the item itself is the key, and records compare by value.

```razor
<MokaTable Items="_orders" ItemKey="o => o.Id" Expandable>
    <MokaColumn Title="Reference" Field="x => x.Reference" />
    <MokaColumn Title="Status" Field="x => x.Status" />
</MokaTable>
```

Keys should be unique. A key that more than one row returns cannot tell those rows apart, so the table renders them without a key instead of letting Blazor throw its duplicate-key error. Blazor then matches those rows by position, as it does for a table without `ItemKey`, and every other row keeps its key. Rows that share a key also share their expanded state: expanding one expands them all.

## Export

`Exportable` adds an export button that writes every matching row, not just the page, as a CSV file or an Excel workbook the table writes itself (`ExportFormats`), named by `ExportFileName`. `CopySelectedRows` copies the picked rows for a spreadsheet, and `CellRanges` marks, copies and pastes blocks of cells. All of it is on [Table Export](/components/table-export).

## Breaking changes in 0.6.0

- **`ShowFilters` is gone.** Use `FilterControls`. Its default, `Row`, draws the filter row once a shown column is `Filterable`, so a table that had `ShowFilters` and filterable columns needs nothing more. A table that marks columns `Filterable` without wanting a filter row sets another control, or `None`. Filters a page sets through `State` on columns that aren't `Filterable` stay while no filter row draws. Once the row draws, they go when a column is hidden or changes, since nothing on screen shows them; `FilterControls="MokaTableFilterControls.None"` keeps them (see [Which filters stay](/components/table-filters#which-filters-stay)).
- **`MokaColumn.Sticky` is gone.** Use `Pin="MokaColumnPin.Start"`. Any number of columns can pin, to either edge.
- **`MokaColumnFilterType` gained `Auto` as its first value, and `Number`, `Date`, `MultiSelect` and `YesNo`.** `Auto` is the new default, so a filterable column of numbers, dates, yes or no, or an enum gets a range, a date range, a yes or no choice or a list instead of a text box. Set `FilterType="MokaColumnFilterType.Text"` for the old box. A stored number of the enum means something else now, since every value moved up one.
- **A failed `ServerData` load no longer reaches the error boundary.** The table shows it with Try again. Listen to `OnLoadFailed` to log it, or replace the message with `ErrorContent`.
- **`SelectedItems` is an owned value.** A parent that passes it and handles `SelectedItemsChanged` without passing the new set back sees the change undone. `@bind-SelectedItems` works as before. `MokaPagination.CurrentPage` and `PageSize` work the same way.
- **The first `ServerData` request happens after the first render.** It waits for the columns, so their `DefaultSort` reaches it. A bUnit test waits for the rows with `WaitForAssertion`.
- **No results replaces the empty state while a search, a filter or a quick filter is active.** `Empty`, `EmptyContent` and the empty state show only when there are no rows at all. Use `NoResults` for your own content.
- **`MokaTableGroup.Count` is the whole group's count**, across every page for rows in memory, where it was the rows on the page. Grouped rows in memory are paged by lines, headings included, instead of by rows.
- **A `MokaTablePager` no longer draws a default pager first.** The table draws no pager until its parts have registered, the render right after the first, so code that looked for the numbered pager in the first render finds the right pager one render later.
- **The actions column's buttons are plain buttons**, not `MokaButton`s, with no click handler of their own: one listener on the table runs them. Styles that targeted `MokaButton`'s classes inside the column target `.moka-actions-btn` instead.

- **Editing has one path, `OnRowSave`.** The table's `OnCellEdit`, `MokaTableCellEditResult<TItem>` and the column's `OnCellEdited` are gone. `OnRowSave` hands over the row as it was, the row with the changes written through the columns' setters, and the changes, each value of the column's own type where `OnCellEdited` gave the entered text. Editors are the library's inputs that fit the column, not one text box, so a number column opens a number field and saves a number.
- **The table is a grid.** The `table` carries `role="grid"` (`treegrid` when grouped) with `aria-rowcount`, `aria-colcount`, and `aria-rowindex` and `aria-colindex` on rows and cells. On a phone list the cells' role is `gridcell` where it was `cell`. Heading cells carry `data-col-index`.
- **`Virtualize` with `ServerData` loads window by window**, where it loaded every row and virtualized only the drawing. The pager is hidden, requests carry `StartIndex` and a window's `PageSize`, and a source that reads only `Page` has to read `StartIndex`, which every request now carries.
- **Exports reach the browser as a stream.** The `downloadCsv` script function is now `downloadFile`, taking the file name and a `DotNetStreamReference`. An export writes the columns in the order on screen, pinned columns first.
- **`MokaTableText.Unnamed` is gone.** A column with no title is named for what its kind shows, or by its place: `ColumnNumber` and `ColumnMenuAt`.

`MokaTableLook` and `MokaTableDensity` moved to the Core package with their namespace kept and a type forward in place, which breaks nothing.
