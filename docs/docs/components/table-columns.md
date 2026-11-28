---
title: Table Column Kinds
description: Ready-made MokaTable columns for people, icons, statuses, progress, yes or no, dates, numbers, money, sizes, links, row menus, actions, editing buttons, copyable values, sparklines, ratings and images.
order: 163
---

# Table Column Kinds

A column kind is a `MokaColumn<TItem>` that knows what its values are. It draws them with the library's own components, and it tells the table's sort, filters, search and export what to read, so you don't keep a `Field` and a `CellTemplate` in step. Each kind takes every [MokaColumn parameter](/components/table#mokacolumn-parameters) (`Title`, `Width`, `Phone`, `Visible`, `Filterable` and the rest) plus its own, and `CellTemplate` still wins over what a kind draws. They all live in `Moka.Red.Data.Table`, and a table that uses none of them pays nothing for them.

```blazor-preview
@code {
    record Member(string Name, string Email, string Role, double Load, bool Active, DateTimeOffset Joined, decimal Spent);

    Member[] _members =
    [
        new("Ada Lovelace", "ada@example.com", "Owner", 72, true, DateTimeOffset.Now.AddDays(-40), 1234.5m),
        new("Grace Hopper", "grace@example.com", "Editor", 35, true, DateTimeOffset.Now.AddHours(-5), 99m),
        new("Alan Turing", "alan@example.com", "Viewer", 91, false, DateTimeOffset.Now.AddDays(-400), 5000m),
    ];
}

<MokaTable Items="_members" Title="Members">
    <MokaPersonColumn Title="Name" Name="m => m.Name" Detail="m => m.Email" />
    <MokaStatusColumn Title="Role" Status="m => m.Role" />
    <MokaProgressColumn Title="Load" Value="m => m.Load" ShowFigure WarnAt="80" />
    <MokaYesNoColumn Title="Active" Value="m => m.Active" />
    <MokaDateColumn Title="Joined" Value="m => m.Joined" Relative />
    <MokaMoneyColumn Title="Spent" Value="m => m.Spent" Currency="EUR" />
</MokaTable>
```

What a kind tells the table:

- **Sort** and **value filters** (`Is`, `GreaterThan`, `Between` and the rest) read its value: a number, a date, yes or no, or text.
- **Filter controls** follow the value too: with `FilterType` left at `Auto`, a number, money, bytes, progress or rating column filters by a range, a date column by a range of days, a yes or no column by yes or no, and a status column by a list of several. See [Table Filters](/components/table-filters#what-a-columns-filter-takes).
- **Search** and **text filters** (`Contains`, `StartsWith`) read its text, as the cell shows it.
- **Export** writes its export text. Numbers, amounts and sizes export as plain numbers in the invariant culture, and dates in ISO 8601, so a spreadsheet reads them as numbers and dates.
- **Grouping** uses its text, unless the column has a `GroupKey`.

A kind with nothing to sort or export, the row menu and the actions column, takes no part in any of them.

## Person

`MokaPersonColumn<TItem>` draws an avatar, the person's name and an optional second line. The avatar is the picture when there is one, else initials or an identicon. It sorts, filters, searches and exports by the name.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Name` | `Func<TItem, string?>?` | -- | The person's name: the first line, and the column's value. Required |
| `Detail` | `Func<TItem, string?>?` | -- | A quieter second line, such as an email address or a role. Null draws one line |
| `Picture` | `Func<TItem, string?>?` | -- | The address of the person's picture |
| `Initials` | `Func<TItem, string?>?` | -- | The initials to draw. Null takes the first letters of the first two words of the name |
| `Fallback` | `MokaAvatarFallback` | `Initials` | What a row without a picture draws: `Initials` or `Identicon` |

```razor
<MokaPersonColumn Title="Owner" Name="f => f.OwnerName" Detail="f => f.OwnerEmail" Picture="f => f.OwnerPhoto" />
```

## Icon and text

`MokaIconTextColumn<TItem>` draws an icon before the text, such as a file type before a file name. It sorts, filters, searches and exports by the text.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `Func<TItem, string?>?` | -- | The row's text, and the column's value. Required |
| `Icon` | `Func<TItem, MokaIconDefinition?>?` | -- | The icon before the text, or null for none in that row |
| `IconColor` | `Func<TItem, MokaColor?>?` | -- | The icon's color, or null for the text's |

```razor
<MokaIconTextColumn Title="File" Text="f => f.Name"
                    Icon="f => f.IsFolder ? MokaIcons.File.Folder : MokaIcons.File.Document" />
```

## Status

`MokaStatusColumn<TItem>` draws a status as a tag in a label color or tone, or as a dot with its name. It sorts, filters, searches and exports by the status's name. With `Filterable`, its filter is a list of several statuses (`MultiSelect`) unless `FilterType` says otherwise, and it is a facet in the facet panel.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Status` | `Func<TItem, string?>?` | -- | The status as its name ("Running"), and the column's value. Required |
| `LabelColor` | `Func<TItem, MokaLabelColor?>?` | -- | The tag's label color, from the status. It wins over `Tone` on a tag |
| `Tone` | `Func<TItem, MokaColor?>?` | -- | The status's tone: the tag's color without a label color, and the dot's color |
| `Look` | `MokaStatusLook` | `Tag` | `Tag`, or `Dot` with the name beside it |

```razor
<MokaStatusColumn Title="State" Status="j => j.State" Look="MokaStatusLook.Dot"
                  Tone="j => j.State == "Failed" ? MokaColor.Error : MokaColor.Success" />
```

## Progress

`MokaProgressColumn<TItem>` draws a meter per row, named by the column's title, with an optional figure and warning and critical levels. It sorts and filters by the value and exports it as a plain number.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, double?>?` | -- | The row's value, or null to draw nothing. Required |
| `Min` | `double` | `0` | The value of an empty bar |
| `Max` | `double` | `100` | The value of a full bar |
| `ShowFigure` | `bool` | `false` | Writes the value beside the bar |
| `FigureFormat` | `string` | `"0"` | How the figure, and the column's text, is written |
| `WarnAt` | `double?` | -- | From this value up the bar is a warning, with an icon and a word |
| `CriticalAt` | `double?` | -- | From this value up the bar is critical |

```razor
<MokaProgressColumn Title="Disk" Value="s => s.DiskUsed" ShowFigure WarnAt="80" CriticalAt="95" />
```

## Yes or no

`MokaYesNoColumn<TItem>` draws a check or a switch for yes and a dash or an off switch for no. It's a picture with the word for screen readers, not a box per row: a control in every row would be a tab stop in every row of a grid that has one. It defaults to the middle of the column. It sorts and filters by the answer (`Is` with `true` or `false`), searches "Yes" and "No" from `Text.Yes` and `Text.No`, and exports `true` or `false`. With `Filterable`, its filter is Yes, No or All.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, bool?>?` | -- | The row's answer, or null to draw nothing. Required |
| `Look` | `MokaYesNoLook` | `Check` | `Check` or `Switch` |

```razor
<MokaYesNoColumn Title="Enabled" Value="r => r.Enabled" Look="MokaYesNoLook.Switch" />
```

## Date and relative time

`MokaDateColumn<TItem>` writes a moment with `Format`, by default the culture's short date and time, or with `Relative` how long ago it was ("5 minutes ago") through `MokaRelativeTime`, with the exact time in a tooltip. It sorts and filters by the moment and exports it in ISO 8601 with its offset.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, DateTimeOffset?>?` | -- | The row's moment, or null for none. A `DateTime` converts by itself. Required |
| `Relative` | `bool` | `false` | Writes how long ago or how far off the moment is, with the exact time in a tooltip |
| `Live` | `bool` | `false` | Keeps relative times up to date as time passes. The cells share one timer, and only a cell whose text changes redraws |
| `TooltipFormat` | `string` | `"F"` | How the tooltip of a relative time writes the exact moment |
| `TimeZone` | `TimeZoneInfo?` | -- | The zone moments are written in. Null is the moment's own offset, or the viewer's for relative times |

```razor
<MokaDateColumn Title="Modified" Value="f => f.Modified" Relative />
<MokaDateColumn Title="Due" Value="t => t.Due" Format="d" />
```

A date filter takes ISO 8601: `new MokaTableFilter("Due", MokaFilterOperator.AtLeast, "2026-01-01")`. With `Filterable`, the filter row shows a range of two date boxes. A day alone compares each moment by its day, so a range up to the 14th keeps all of the 14th.

## Number

`MokaNumberColumn<TItem>` writes a number right aligned, in tabular figures so the digits line up, in the current culture. `Format` sets how, by default with group separators and up to two decimals (`"#,0.##"`). It sorts and filters by the number and exports it in the invariant culture.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, double?>?` | -- | The row's number, or null for none. An `int` or `long` converts by itself; cast a `decimal`. Required |

```razor
<MokaNumberColumn Title="Orders" Value="c => c.OrderCount" Format="N0" Aggregate="MokaAggregateType.Sum" />
```

## Money

`MokaMoneyColumn<TItem>` writes an amount as currency in the current culture, in the culture's own currency or the one `Currency` names, right aligned in tabular figures. It sorts and filters by the amount and exports the plain number.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, decimal?>?` | -- | The row's amount, or null for none. Required |
| `Currency` | `string?` | -- | The ISO 4217 code ("EUR", "USD"), written with its usual symbol in the current culture's layout. Null is the culture's currency |
| `Decimals` | `int?` | -- | How many decimals to write. Null is the currency's usual number |

```razor
<MokaMoneyColumn Title="Total" Value="o => o.Total" Currency="EUR" />
```

## Bytes

`MokaBytesColumn<TItem>` writes a byte count in the largest unit that fits ("1.5 GB"), through the same formatter as `MokaBytes`, right aligned in tabular figures. It sorts and filters by the count and exports the plain count.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, long?>?` | -- | The row's byte count, or null for none. Required |
| `Units` | `MokaByteUnits` | `Decimal` | Powers of 1000 (KB, MB) or 1024 (`Binary`: KiB, MiB) |
| `Decimals` | `int?` | -- | How many decimals to write. Null writes one under 100 and none from there up |
| `PerSecond` | `bool` | `false` | Writes the count as a rate, "12 MB/s" |

```razor
<MokaBytesColumn Title="Size" Value="f => f.Length" />
```

## Link

`MokaLinkColumn<TItem>` draws a link through `MokaLink`. A row without an address, or with a script address such as `javascript:`, shows plain text. It sorts, filters, searches and exports by the text.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `Func<TItem, string?>?` | -- | The link's text, and the column's value. Required |
| `Href` | `Func<TItem, string?>?` | -- | Where the link goes. Required |
| `External` | `bool` | `false` | Opens the link in a new tab, with an icon saying so |

```razor
<MokaLinkColumn Title="Order" Text="o => o.Number" Href="@(o => $"/orders/{o.Id}")" />
```

## Row menu

`MokaRowMenuColumn<TItem>` draws a button in each row that opens a menu of that row's items. There is one menu for the whole page: the button opens it through `IMokaContextMenuService`, so the layout needs a `MokaContextMenuHost` (see [Context Menu](context-menu)), and `AddMokaRed()` registers the service. A click from the keyboard opens the menu under the button. The items are built when the menu opens, not as rows draw. The buttons have no click handler of their own: one listener on the table hears every row's. Each button is named in a `MokaTooltip`, "Actions for" and the row's name, or "Row actions". The column sits at the row's end and takes no part in sorting, filters, search or export.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `Func<TItem, IReadOnlyList<MokaContextMenuItem>>?` | -- | The menu's items for a row. Required |
| `ItemName` | `Func<TItem, string?>?` | -- | What a row is called, for its button's name. Null names every button the same |

```razor
<MokaRowMenuColumn ItemName="f => f.Name" Items="@(f => [
    new MokaContextMenuItem { Text = "Rename", OnClick = () => RenameAsync(f) },
    new MokaContextMenuItem { Text = "Delete", DividerBefore = true, OnClick = () => DeleteAsync(f) }
])" />
```

Without the service registered, a click throws an `InvalidOperationException` that says what to register.

## Actions

`MokaActionsColumn<TItem>` draws quiet icon buttons in each row, each named in a `MokaTooltip`. They are plain `<button>` elements with the class `moka-actions-btn`, not `MokaButton`s, and none has a click handler of its own: one listener on the table hears every row's and runs the action, so a long list costs no handler per row. With `Reveal="MokaActionsReveal.OnHover"` they show while the pointer is over the row or focus is inside it, by CSS alone with no listener per row, and always on a touch screen, which has no hover. A click in the buttons doesn't raise `OnRowClick`. The column sits at the row's end and takes no part in sorting, filters, search or export.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Actions` | `IReadOnlyList<MokaTableAction<TItem>>?` | -- | The buttons, in the order they are drawn. Required |
| `Reveal` | `MokaActionsReveal` | `Always` | `Always`, or `OnHover` |

`MokaTableAction<TItem>(Label, Icon, OnClick)` is one button: `Label` names it, `Icon` draws it, and `OnClick` (`Func<TItem, Task>`) runs with the row. `Disabled` and `Hidden` (`Func<TItem, bool>?`) disable it or leave it out for a row, and `Color` (`MokaColor?`) colors its hover, such as `MokaColor.Error` for a delete.

```razor
<MokaActionsColumn Reveal="MokaActionsReveal.OnHover" Actions="_actions" />

@code {
    IReadOnlyList<MokaTableAction<Order>> _actions = [];

    protected override void OnInitialized() => _actions =
    [
        new("Edit", MokaIcons.Action.Edit, EditAsync),
        new("Delete", MokaIcons.Action.Delete, DeleteAsync)
        {
            Hidden = order => order.Shipped,
            Color = MokaColor.Error
        }
    ];
}
```

## Edit buttons

`MokaEditColumn<TItem>` draws the buttons that edit a row: Edit and Delete, and while the row is open Save and Cancel. Edit opens the row's editable cells, the whole row in `MokaTableEditMode.Row` and the first editable cell in `Cell`; Save and Cancel close it; Delete raises the table's `OnRowDelete`, asking first through `IMokaDialogService` when `ConfirmDelete` is on. A new row from the table's Add button shows Save and Cancel here too. It is an actions column, so its buttons have no handler or component per row. `ShowDelete="false"` leaves Delete out, and Delete shows only while the table has an `OnRowDelete`. See [Table Editing](/components/table-editing).

```razor
<MokaTable Items="_parts" EditMode="MokaTableEditMode.Row" OnRowSave="Save" OnRowDelete="Delete" ItemName="p => p.Name">
    <MokaColumn Title="Name" Field="p => p.Name" Editable SetField="@((p, v) => p with { Name = (string?)v ?? "" })" />
    <MokaEditColumn TItem="Part" />
</MokaTable>
```

## Editing in a column kind

Every kind takes `Editable` and the other [editing parameters](/components/table-editing#parameters), and the kinds whose values can be edited have a setter of their own type, which writes the value into a copy of the row and returns it:

| Kind | Editor | Setter |
|------|--------|--------|
| `MokaNumberColumn` | Number field | `SetValue`: `Func<TItem, double?, TItem>` |
| `MokaMoneyColumn` | Number field | `SetValue`: `Func<TItem, decimal?, TItem>` |
| `MokaBytesColumn` | Number field | `SetValue`: `Func<TItem, long?, TItem>` |
| `MokaDateColumn` | Date picker, keeping the time of day and the offset | `SetValue`: `Func<TItem, DateTimeOffset?, TItem>` |
| `MokaYesNoColumn` | Check box, or a switch with `Look="MokaYesNoLook.Switch"`. A paste reads the table's Yes and No too | `SetValue`: `Func<TItem, bool?, TItem>` |
| `MokaStatusColumn` | List of the statuses the rows have, or `EditOptions` | `SetStatus`: `Func<TItem, string?, TItem>` |
| `MokaRatingColumn` | Stars, up to `Max`. A paste takes a number from 0 to `Max` | `SetValue`: `Func<TItem, double?, TItem>` |

In a spreadsheet export these kinds write their values as numbers, dates and booleans; see [Table Export](/components/table-export#formats).

## Copyable

`MokaCopyableColumn<TItem>` draws a value in monospace with a copy button, through `MokaCopyable`. The copy happens in the browser inside the click, as every copy button in the library does. With `Secret` the cell shows dots and the value stays off the page: the button reads it only when pressed, and the search leaves the column out. It sorts and filters by the value and exports it.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, string?>?` | -- | The row's value. Required |
| `Secret` | `bool` | `false` | Shows dots in place of the value, for keys and tokens |

```razor
<MokaCopyableColumn Title="Id" Value="o => o.Id.ToString()" />
<MokaCopyableColumn Title="Key" Value="k => k.Token" Secret />
```

## Sparkline

`MokaSparklineColumn<TItem>` draws a `MokaSparkline` per row, oldest value first. It sorts and filters by the latest value, writes that as its text, and exports every value separated by spaces in the invariant culture.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Values` | `Func<TItem, IReadOnlyList<double>?>?` | -- | The row's values, oldest first. A `double.NaN` is a gap. Required |
| `Label` | `Func<TItem, string?>?` | -- | The sparkline's name for screen readers. Null uses the column's title |
| `Height` | `string` | `"1.5rem"` | The sparkline's height, as a CSS length |
| `ShowArea` | `bool` | `false` | Fills the area under the line |

```razor
<MokaSparklineColumn Title="Last 24 hours" Values="s => s.HourlyRequests" />
```

## Rating

`MokaRatingColumn<TItem>` draws stars from a number, to the nearest star, with the rating in words for screen readers ("4.2 of 5", from `Text.RatingOf`). They are a picture, not `MokaRating`, which is an input and would put a tab stop in every row. It sorts and filters by the rating and exports it.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `Func<TItem, double?>?` | -- | The row's rating, from 0 to `Max`, or null to draw nothing. Required |
| `Max` | `int` | `5` | How many stars a full rating has |

```razor
<MokaRatingColumn Title="Score" Value="r => r.Average" />
```

## Image

`MokaImageColumn<TItem>` draws a thumbnail that opens a `MokaLightbox`. There is one viewer for the column, drawn beside the rows only once someone opens an image, and it steps through every image the table shows now, starting at the one pressed. Addresses that aren't pictures, such as `javascript:`, draw nothing. Each thumbnail is a button named "Open" and its `Alt`, in a `MokaTooltip`, with no click handler of its own: one listener on the table hears every row's. It sorts, filters, searches and exports by `Alt`.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Src` | `Func<TItem, string?>?` | -- | The address of the full image, shown in the viewer. Required |
| `Thumbnail` | `Func<TItem, string?>?` | -- | A smaller picture for the cell. Null draws the full image small |
| `Alt` | `Func<TItem, string?>?` | -- | What the image shows, for screen readers and the viewer, and the column's value |
| `ThumbnailSize` | `string` | `"2rem"` | The thumbnail's size, as a CSS length |

```razor
<MokaImageColumn Title="Photo" Src="p => p.PhotoUrl" Thumbnail="p => p.ThumbUrl" Alt="p => p.Name" />
```

## Writing your own kind

A kind derives from `MokaColumn<TItem>`. A `.razor` file with `@inherits MokaColumn<TItem>`, `@typeparam TItem` and no markup of its own is the simplest form: its templates written in `@code` carry its scoped CSS. Override what it changes:

| Member | What it does |
|--------|--------------|
| `CellContent` | A `RenderFragment<TItem>` for the cell when no `CellTemplate` is set. Build it once, in a field, so one fragment serves every row. Null writes `TextOf` |
| `ValueOf(TItem)` | The row's value: what the sort orders by, value filters compare and the footer totals |
| `TextOf(TItem)` | The row's text: what the search and text filters read and the default cell writes |
| `ExportTextOf(TItem)` | What an export writes. `TextOf` by default |
| `GroupKeyFor(TItem)` | The heading a row goes under when grouped by the column. `GroupKey`, else `TextOf`, by default |
| `HasValue` | Whether the column has a value to sort, filter, search and group by. True with a `Field` by default |
| `KindSettings` | Your own parameters that change what the column draws, as one value that compares equal when nothing changed: a tuple or a record, leaving delegates out. The table draws again in the same render when it changes |
| `KindCellClass` | A class every data cell of the column carries, for the kind's own look of the cell itself |
| `Overlay` | Content the table draws once, beside its rows, for the column: a viewer or dialog its cells open. Call `Redraw()` after changing what it shows |
| `Align` | A different default alignment: override the property with its own initializer |
| `AutoFilterType` | What `FilterType="Auto"` means for the column when its values can't say, such as `MultiSelect` for text that reads as a list. `Auto` by default, which goes by the values |
| `HandleCellActionAsync(TItem, string, MouseEventArgs)` | Runs an action a control in one of the column's cells asked for. Nothing by default. The table draws again afterwards |
| `KindName` | What the column is called when it has no `Title`, in its column menu, the column toggle and its editor's name, such as the actions column's "Actions". Null names it by its place |
| `ExportValueOf(TItem)` | What a spreadsheet export writes in the cell: a number, a date, a `bool` or text. The `Field` value when it is one of those, else `ExportTextOf` |
| `EditValueType(TItem)` | The type of the column's values, for its editor and for reading pasted text |
| `AutoEditor(TItem)` | The editor `Editor="Auto"` means for the column |
| `CanWrite`, `WriteValue(TItem, object?)` | Whether the column can write a value into a row, and the write: `SetField` by default, a kind's own setter for the kinds above |
| `TryParseText(string, TItem, out object?)` | Reads pasted text as a value of the column's type, in the current culture or the invariant one |

`Words` is the table's `MokaTableText`, for words of your own kind, and `ShownItems` the rows the table shows now.

```razor
@namespace Shop.Columns
@typeparam TItem
@inherits MokaColumn<TItem>

@code {
    private RenderFragment<TItem>? _cell;

    [Parameter, EditorRequired] public Func<TItem, string?>? Sku { get; set; }

    protected override bool HasValue => Sku is not null;

    protected override RenderFragment<TItem> CellContent => _cell ??= item =>
        @<code class="sku-cell">@Sku?.Invoke(item)</code>;

    protected override object? ValueOf(TItem item) => Sku?.Invoke(item);

    protected override string TextOf(TItem item) => Sku?.Invoke(item) ?? string.Empty;
}
```

The members are `protected internal`, so a kind in another assembly overrides them as `protected`.

### Controls in cells

A control in a cell needs no click handler of its own. Mark it with `data-moka-act="@ActionAttribute("name")"`: one listener on the table hears a click, or Enter or Space on the control, in every row, and calls `HandleCellActionAsync` with the row, the action's name and where the click was. A click from the keyboard reports no position and a detail of 0. The row's own click doesn't run, so the control doesn't also open or select the row. Pinned rows work the same way. This is how the actions, row menu and image columns draw a button in every row without a handler per row.

```razor
@namespace Shop.Columns
@typeparam TItem
@inherits MokaColumn<TItem>

@code {
    private RenderFragment<TItem>? _cell;

    [Parameter, EditorRequired] public Func<TItem, bool>? Starred { get; set; }

    [Parameter, EditorRequired] public Func<TItem, Task>? Toggle { get; set; }

    protected override bool HasValue => Starred is not null;

    protected override object? ValueOf(TItem item) => Starred?.Invoke(item);

    protected override RenderFragment<TItem> CellContent => _cell ??= item =>
        @<MokaTooltip Text="Star" Position="MokaTooltipPosition.Bottom">
            <button type="button" class="star-cell" aria-pressed="@(Starred!(item) ? "true" : "false")"
                    aria-label="Star" data-moka-act="@ActionAttribute("star")">
                <MokaIcon Icon="MokaIcons.Toggle.Star" />
            </button>
        </MokaTooltip>;

    protected override Task HandleCellActionAsync(TItem item, string action, MouseEventArgs click) =>
        action == "star" && Toggle is not null ? Toggle(item) : Task.CompletedTask;
}
```

Action names only need to be unique within the column. A disabled control's clicks are ignored.
