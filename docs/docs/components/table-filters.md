---
title: Table Filters
description: MokaTable's search box, its five filter controls (a filter row, heading menus, chips, facets and a builder), quick filters with counts, typed filters, server-side filtering and the no results state.
order: 165
---

# Table Filters

A `MokaTable` narrows its rows three ways: a search box, column filters and quick filters. Column filters come from five controls, which you pick and combine. Every control reads and writes the same filters, so a filter set in a heading's menu shows as a chip and in the builder too. The search, the filters, how they join and the quick filter are all part of the table's [state](/components/table#the-tables-state). With `Items` the table filters the rows itself; with `ServerData` it passes everything in the request and your source applies it.

```razor
<MokaTable Items="_orders" Searchable HighlightSearch
           FilterControls="MokaTableFilterControls.Row | MokaTableFilterControls.Chips"
           QuickFilters="_quickFilters">
    <MokaColumn Title="Number" Field="o => o.Number" Filterable />
    <MokaColumn Title="Customer" Field="o => o.Customer" Filterable />
    <MokaDateColumn Title="Placed" Value="o => o.Placed" Filterable />
    <MokaMoneyColumn Title="Total" Value="o => o.Total" Filterable />
    <MokaStatusColumn Title="Status" Status="o => o.Status" Filterable />
</MokaTable>

@code {
    IReadOnlyList<MokaTableQuickFilter> _quickFilters =
    [
        new("Open", [new MokaTableFilter("Status", MokaFilterOperator.IsAnyOf, ["Open", "Held"])]),
        new("Large", [new MokaTableFilter("Total", MokaFilterOperator.AtLeast, "1000")])
    ];
}
```

Everything here lives in `Moka.Red.Data.Table`.

## Parameters

On `MokaTable`:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Searchable` | `bool` | `false` | Shows the search box in the toolbar |
| `Search` | `string?` | -- | The text in the search box. Two-way bindable with `@bind-Search`. A new value seeds the table, as the state does |
| `SearchChanged` | `EventCallback<string?>` | -- | Raised when the search changes |
| `Debounce` | `TimeSpan` | 300 ms | How long the table waits after the last key in the search box or a filter box before it loads |
| `HighlightSearch` | `bool` | `false` | Marks what the search found in the cells that write their text |
| `FilterControls` | `MokaTableFilterControls` | `Row` | Which filter controls the table draws, combined with `\|` |
| `QuickFilters` | `IReadOnlyList<MokaTableQuickFilter>?` | -- | Preset filters drawn above the table, with an "All" choice first |
| `QuickFilterLook` | `MokaQuickFilterLook` | `Segments` | `Segments`, a segmented control, or `Tabs` |
| `FacetsFromServer` | `bool` | `false` | Asks a `ServerData` source for the list values with counts, and the quick filters' counts |
| `NoResults` | `RenderFragment?` | -- | Your own content for a search or filters that leave no row |

On `MokaColumn` and every column kind:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Filterable` | `bool` | `false` | The filter controls offer this column. Needs a `Title` |
| `FilterType` | `MokaColumnFilterType` | `Auto` | What the column's filter takes. See [What a column's filter takes](#what-a-columns-filter-takes) |
| `FilterOptions` | `IReadOnlyList<MokaFilterOption>?` | -- | The choices a list filter offers, in place of the values found in the rows |

`ClearSearchAndFiltersAsync()` on the table clears the search, the filters and the quick filter, and loads the rows again.

## Search

`Searchable` adds a search box to the toolbar. A row matches when the text of any column with a value holds the search, ignoring case: the text the cell shows, `Format` applied, and a column kind's own text. Hidden columns count too, and a [secret copyable column](/components/table-columns#copyable) doesn't. The table waits `Debounce` after the last key press, then loads, goes back to page 1 and shows "12 results" next to the box. A clear button sits in the box while a search is in force.

`@bind-Search` binds the search, for a box somewhere else on the page or a search kept in the address. A new value from the parent shows in the box and loads the rows, and `SearchChanged` reports what someone types once it loads.

`HighlightSearch` wraps what the search found in a `<mark class="moka-table-mark">`, in every cell that writes its text, with no component per cell. A cell drawn by a `CellTemplate` or a column kind keeps its own content and isn't marked. On the new design the mark takes the warning tint.

```razor
<MokaTable Items="_files" Searchable HighlightSearch @bind-Search="_search" Debounce="TimeSpan.FromMilliseconds(150)">
    <MokaColumn Title="Name" Field="f => f.Name" />
    <MokaColumn Title="Folder" Field="f => f.Folder" />
</MokaTable>

@code {
    string? _search;
}
```

One wait serves the search box and the filter row's boxes, so typing in both loads once. It runs on a registered `TimeProvider`, else the system clock, so a test can drive it with a fake clock.

## Filter controls

`FilterControls` is a set of flags. Combine them with `|`:

| Control | What it draws |
|---------|---------------|
| `Row` | A row under the headings with an input that fits each filterable column. The default. Drawn only while a shown column is `Filterable` |
| `Menus` | A filter button in each filterable heading that opens the operator and the value over the table |
| `Chips` | The filters in force as chips above the table, each with a remove button, and Clear all |
| `Facets` | A side panel listing the values of each filterable list or yes and no column, with how many rows have each |
| `Builder` | A toolbar button that opens a panel of conditions, joined by all or by any |
| `None` | No filter control. Filters still come from the state |

Quick filters draw whenever the table has `QuickFilters`, whatever `FilterControls` says.

```razor
<MokaTable Items="_users" ColumnMenu
           FilterControls="MokaTableFilterControls.Menus | MokaTableFilterControls.Chips | MokaTableFilterControls.Builder">
    <MokaColumn Title="Name" Field="u => u.Name" Filterable />
    <MokaColumn Title="Role" Field="u => u.Role" Filterable FilterType="MokaColumnFilterType.MultiSelect" />
    <MokaDateColumn Title="Last seen" Value="u => u.LastSeen" Relative Filterable />
    <MokaYesNoColumn Title="Admin" Value="u => u.IsAdmin" Filterable />
</MokaTable>
```

### The filter row

Each filterable column gets the input that fits [what its filter takes](#what-a-columns-filter-takes):

| Takes | Input | Filter it sets |
|-------|-------|----------------|
| Text | A text box | `Contains` |
| A number | Two boxes, from and to, either one optional | `Between` with both, `AtLeast` with the first, `AtMost` with the second |
| A date | Two date boxes, from and to, either one optional, compared by the day | `Between`, `AtLeast` or `AtMost` |
| One value from a list | A dropdown of the choices, with All first | `Is` |
| Several values from a list | A button showing what is picked, or All, that opens a popup of boxes, one per choice | `IsAnyOf` |
| Yes or no | A dropdown of All, Yes and No | `Is` with `true` or `false` |

Typed boxes wait `Debounce` after the last key; a pick from a dropdown applies at once. Each box is named "Filter by Customer", and a range's boxes "Total from" and "Total to". A filter set elsewhere, from a chip, a menu, the builder or the state, shows in the row's boxes.

### Heading menus

`Menus` puts a filter button in each filterable heading. It opens a popup in the browser's top layer, under the button, holding what the column's filter takes:

- A list: a box per choice, with its count when the counts are known. Ticked choices set one `IsAnyOf` filter.
- Yes or no: Yes and No as radio buttons.
- Anything else: an operator list and one or two value boxes, as the operator needs. The operators are listed under [Typed filters](#typed-filters).

**Apply** sets the filter and **Clear** removes it. The popup is a non-modal dialog named "Filter Status". Escape and a click outside close it, and focus goes back to the button. While its column is filtered, the button is lit and named with the filter in words: "Filter by Status, now Status is any of Open, Held".

The [column menu](/components/table#column-menu)'s "Filter…" opens the same popup, under the filter button when the `Menus` control is on, else under the heading. A column menu filters even without `Menus`.

### Chips

`Chips` lists the filters in force above the table, each in words ("Total is between 100 and 500") with a remove button named "Remove the filter Total is between 100 and 500". **Clear all** removes every filter and the quick filter. When the filters join by any and there are two or more, "Matching any" stands beside the chips. Removing a chip moves focus to the first chip left, or to the first sort button once none is left.

### Facets

`Facets` adds a side panel beside the rows, named "Refine". It lists each shown, filterable column whose filter is a list or yes and no, with a box per value and how many rows have it. A column shows its first eight values, then a "Show all 12" button. Ticking values sets one `IsAnyOf` filter on the column, so a row may have any of them.

Each column is counted with every filter but its own, with the search and the quick filter too. So the other values of a column you filtered still show how many rows they would give. With `Items` the table counts the rows in memory itself. With `ServerData` they come from the source only under `FacetsFromServer` (see [Facets from the server](#facets-from-the-server)); without it the panel lists the values on the rows loaded, with no counts.

```razor
<MokaTable Items="_products" FilterControls="MokaTableFilterControls.Facets | MokaTableFilterControls.Chips">
    <MokaColumn Title="Name" Field="p => p.Name" />
    <MokaColumn Title="Brand" Field="p => p.Brand" Filterable FilterType="MokaColumnFilterType.MultiSelect" />
    <MokaColumn Title="Category" Field="p => p.Category" Filterable FilterType="MokaColumnFilterType.MultiSelect" />
    <MokaYesNoColumn Title="In stock" Value="p => p.InStock" Filterable />
</MokaTable>
```

`Auto` gives a column of text a text box, which isn't a facet, so a text column names `Select` or `MultiSelect` to be listed.

### Builder

`Builder` adds a **Filters** button to the toolbar, "Filters (2)" while two filters are in force. It opens a panel named "Filter conditions": "Rows match" with **all conditions** or **any condition**, then one line per condition, each a column, an operator and its values. **Add a condition** adds a line for a filterable column, a cross removes one, **Clear all** removes them all and **Done** closes the panel with focus back on the button.

The panel applies the conditions as they become whole, and the table loads once for them. A condition still being written stays in the panel until it has its values. Filters set elsewhere come in as conditions. How the filters join is `MokaFilterJoin`, `All` or `Any`, kept in the state as `FilterJoin` and sent to a server source in the request.

### Which filters stay

A filter that no control on screen can show or clear is dropped, and the rows it hid come back:

- With `Chips` or `Builder` every filter stays, since both show every filter.
- With `Row` or `Menus` and neither of those, a filter stays while its column is shown and `Filterable`. Hiding the column, taking its `Filterable` away or removing it drops its filter. While no shown column is `Filterable`, so the row and the menus have never drawn, the filters the state names stay.
- With `Facets` alone, a filter stays while its column is a shown facet.
- With `None`, filters come from the state, which is their control, so a page can filter through `State` with buttons of its own.

The check runs when a column is hidden, removed or changes, when the arrangement is reset and when `FilterControls` changes. Changing `FilterControls` to `None` drops every filter the controls set. A page that filters only through `State`, on columns that aren't `Filterable`, keeps its filters under the default `Row`, since no row draws. Once one column is `Filterable` and the row draws, a filter on a column the row can't show goes at the next check; set `FilterControls="MokaTableFilterControls.None"` to keep it.

## Quick filters

`QuickFilters` draws preset filters above the table: "All" first, then one choice per `MokaTableQuickFilter(Name, Filters)`, each with how many rows it keeps. A quick filter's filters always narrow on top of the others, whatever the join says. The one picked is the state's `QuickFilter`, by name, so keep names unique.

`QuickFilterLook` draws them as `Segments`, a segmented control of radio buttons, or `Tabs` along the top of the table. The group is named "Show".

```razor
<MokaTable Items="_tickets" QuickFilters="_views" QuickFilterLook="MokaQuickFilterLook.Tabs" Searchable>
    <MokaColumn Title="Subject" Field="t => t.Subject" />
    <MokaColumn Title="Assignee" Field="t => t.Assignee" />
    <MokaStatusColumn Title="Status" Status="t => t.Status" />
</MokaTable>

@code {
    static readonly IReadOnlyList<MokaTableQuickFilter> _views =
    [
        new("Unassigned", [new MokaTableFilter("Assignee", MokaFilterOperator.IsEmpty, [])]),
        new("Waiting", [new MokaTableFilter("Status", MokaFilterOperator.Is, "Waiting")]),
        new("Done", [new MokaTableFilter("Status", MokaFilterOperator.Is, "Done")])
    ];
}
```

With `Items`, each choice counts the rows that pass the search and the filters with that quick filter and no other, and "All" counts them with none. With `ServerData` the counts come from the source under `FacetsFromServer`, and without it the choices show no counts. A quick filter's column needn't be `Filterable` or shown. Two quick filters are equal when their names and filters match.

## What a column's filter takes

`FilterType` says what a column's controls take, in the filter row, the heading menu, the builder and the facets:

| Type | Takes |
|------|-------|
| `Auto` | Worked out from the column's values. The default |
| `Text` | Free text the cell's text holds |
| `Number` | A range of numbers, either end optional |
| `Date` | A range of dates, either end optional, compared by the day |
| `Select` | One value picked from a list of the column's values |
| `MultiSelect` | Any number of values picked from the list |
| `YesNo` | Yes, no or either |

`Auto` looks at the first value the rows have: yes or no takes `YesNo`, a date (`DateTime`, `DateTimeOffset` or `DateOnly`) takes `Date`, an enum takes `Select`, a number takes `Number`, and text or anything else takes `Text`. A column with `FilterOptions` takes `Select`. A column kind can say what its values mean when they can't: the [status column](/components/table-columns#status) takes `MultiSelect`, since its text reads as a list. The yes or no, date, number, money, bytes, progress and rating columns hand over values of their own kind, so `Auto` gives them the right control. The guess is kept until the rows load again.

### List choices

A list filter, in the row, a menu, the builder or the facets, offers:

1. The column's `FilterOptions`, when set, with the counts the last load found.
2. Else, with `Items`, the values of the rows that pass the search, the quick filter and every other column's filter, sorted by their text.
3. Else, with `ServerData` under `FacetsFromServer`, what the source sent in `Facets`.
4. Else the values on the rows loaded.

Yes and no always offer the two. Each choice is a `MokaFilterOption(Value, Text)`: the value filters write, in the invariant culture with dates in ISO 8601, and the text people read. `FilterOptions` serves a server source that doesn't send facets, or values no row has yet:

```razor
<MokaColumn Title="Priority" Field="t => t.Priority" Filterable FilterOptions="_priorities" />

@code {
    static readonly IReadOnlyList<MokaFilterOption> _priorities =
    [
        new("1", "Urgent"), new("2", "High"), new("3", "Normal"), new("4", "Low")
    ];
}
```

## Typed filters

The state's `Filters` are `MokaTableFilter(Column, Operator, Values)` records. Values are text in the invariant culture: numbers as `12.5`, dates in ISO 8601 (`2026-11-14`, or a full time with its offset), yes or no as `true` or `false`. A filter with one value can be written `new MokaTableFilter("Age", MokaFilterOperator.AtLeast, "18")`, and `Value` reads the first.

| Operator | Matches |
|----------|---------|
| `Contains`, `DoesNotContain` | The cell's text holds the value, or doesn't, ignoring case |
| `StartsWith`, `EndsWith` | The cell's text starts or ends with the value, ignoring case |
| `Is`, `IsNot` | The value equals it, or doesn't: text ignoring case, numbers, dates and yes or no by value |
| `IsEmpty`, `IsNotEmpty` | The value is missing or empty text, or isn't |
| `GreaterThan`, `AtLeast`, `LessThan`, `AtMost` | The value orders after or before it: a number, a date, or text |
| `Between` | The value lies between the first and second values, both included |
| `IsAnyOf`, `IsNoneOf` | The value equals one of the values, or none of them |

Text operators read the cell's text. The others read the column's value and take the filter's values as the value's own kind. A value that can't be compared, such as text in a number filter, matches no ordering test and equals nothing. A date written as a day alone (`2026-11-14`) compares a time by its day, so "up to the 14th" keeps all of the 14th; a full time compares exactly.

The menus and the builder offer the operators that fit the column:

| Takes | Operators |
|-------|-----------|
| Text | `Contains`, `DoesNotContain`, `Is`, `IsNot`, `StartsWith`, `EndsWith`, `IsEmpty`, `IsNotEmpty` |
| A number or a date | `Is`, `IsNot`, `GreaterThan`, `AtLeast`, `LessThan`, `AtMost`, `Between`, `IsEmpty`, `IsNotEmpty`. For dates they read "is after", "is on or after" and so on |
| A list | `IsAnyOf`, `IsNoneOf`, `IsEmpty`, `IsNotEmpty` |
| Yes or no | `Is` |

A page can set filters through the state, with controls of its own:

```razor
<MokaTable Items="_orders" State="_openBigOrders" FilterControls="MokaTableFilterControls.None">
    <MokaColumn Title="Status" Field="o => o.Status" />
    <MokaColumn Title="Total" Field="o => o.Total" Format="C2" />
</MokaTable>

@code {
    MokaTableState _openBigOrders = new()
    {
        Filters =
        [
            new MokaTableFilter("Status", MokaFilterOperator.IsAnyOf, ["Open", "Held"]),
            new MokaTableFilter("Total", MokaFilterOperator.AtLeast, "1000")
        ],
        FilterJoin = MokaFilterJoin.All
    };
}
```

## Server-side filtering

With `ServerData` the table filters nothing itself. The request carries everything:

| Property | What to do with it |
|----------|--------------------|
| `Search` | Match it against the columns your search covers |
| `Filters` | Apply each one to its column |
| `FilterJoin` | `All`: a row passes every filter. `Any`: a row passes at least one |
| `QuickFilter` | Apply its filters on top, whatever `FilterJoin` says |
| `Facets` | Under `FacetsFromServer`, send these columns' values with counts |
| `QuickFilterCounts` | Under `FacetsFromServer`, send how many rows each quick filter keeps |

```csharp
IQueryable<Order> Matching(IQueryable<Order> orders, MokaTableRequest request, string? leaveOut = null)
{
    if (!string.IsNullOrEmpty(request.Search))
        orders = orders.Where(o => o.Customer.Contains(request.Search) || o.Number.Contains(request.Search));

    List<MokaTableFilter> filters = request.Filters.Where(f => f.Column != leaveOut).ToList();
    orders = request.FilterJoin == MokaFilterJoin.Any && filters.Count > 1
        ? orders.Where(AnyOf(filters))
        : filters.Aggregate(orders, (query, filter) => query.Where(Test(filter)));

    if (request.QuickFilter is { } quick)
        orders = quick.Filters.Aggregate(orders, (query, filter) => query.Where(Test(filter)));

    return orders;
}
```

`Test` and `AnyOf` are your own code: `Test` turns one filter into an `Expression<Func<Order, bool>>`, and `AnyOf` joins the tests of several filters with or.

### Facets from the server

Under `FacetsFromServer` the request's `Facets` lists the list and yes or no columns the table counts, and `QuickFilterCounts` the quick filters. Count each column with every filter but its own, so its other values still say what they would give, and each quick filter with the search and the filters but no other quick filter. Answer by column title and by name:

```csharp
Dictionary<string, IReadOnlyList<MokaFacetValue>> facets = [];
if (request.Facets.Contains("Status"))
{
    facets["Status"] = await Matching(_db.Orders, request, leaveOut: "Status")
        .GroupBy(o => o.Status)
        .Select(g => new MokaFacetValue(g.Key, g.Key, g.Count()))
        .ToListAsync(request.CancellationToken);
}

Dictionary<string, int> quickCounts = [];
foreach (MokaTableQuickFilter quick in request.QuickFilterCounts)
    quickCounts[quick.Name] = await Matching(_db.Orders, request with { QuickFilter = quick }).CountAsync(request.CancellationToken);

return new MokaTableResult<Order>
{
    Items = items,
    TotalItems = total,
    Facets = facets,
    QuickFilterCounts = quickCounts
};
```

A `MokaFacetValue(Value, Text, Count)` writes its value the way filters write theirs: the invariant culture, dates in ISO 8601, `true` or `false`. Without `FacetsFromServer` the list filters and facets show the values on the rows loaded, with no counts, and the quick filters show no counts; set `FilterOptions` to offer every value.

## No results

When a search, a filter or a quick filter is active and leaves no row, the table says "No results for “report”." with a search, or "No rows match the filters." without one, with a **Clear search and filters** button that clears all three. It is a polite status region, in place of the empty state. `NoResults` replaces it with your own content, and `ClearSearchAndFiltersAsync()` does what the button does. `Empty`, `EmptyContent` and the empty state are for a table with no rows at all.

```razor
<MokaTable @ref="_table" TItem="Order" Items="_orders" Searchable>
    <ChildContent>
        <MokaColumn Title="Customer" Field="o => o.Customer" Filterable />
    </ChildContent>
    <NoResults>
        <MokaEmptyState Icon="MokaIcons.Action.Search" Title="No order matches" Compact>
            <MokaButton Size="MokaSize.Sm" OnClick="() => _table!.ClearSearchAndFiltersAsync()">Show every order</MokaButton>
        </MokaEmptyState>
    </NoResults>
</MokaTable>

@code {
    MokaTable<Order>? _table;
}
```

## Keyboard

| Key | Action |
|-----|--------|
| Tab | Reaches the search box, the row's boxes and dropdowns, the heading filter buttons, the chips' remove buttons, the facet boxes and the builder's controls in page order |
| Enter in a filter popup's box | Applies the filter, like **Apply** |
| Escape in a filter popup | Closes it with focus back on what opened it |
| Space on a facet or a list box | Ticks or unticks the value |

Typing in the search box or a filter box renders nothing until the wait ends, so fast typing never loses keys.

## Words

Every word is in the table's `Text`, a `MokaTableText`. The filter words:

| Property | Default | Where |
|----------|---------|-------|
| `SearchPlaceholder`, `ClearSearch`, `SearchResults` | `"Search..."`, `"Clear search"`, `"12 results"` | The search box |
| `Filter`, `FilterBy` | `"Filter"`, `"Filter by Customer"` | A filter box's placeholder and name |
| `FilterFrom`, `FilterTo`, `RangeFrom`, `RangeTo` | `"Total from"`, `"Total to"`, `"From"`, `"To"` | A range's boxes |
| `All` | `"All"` | The choice that filters nothing |
| `OperatorName` | `"contains"`, `"is between"`, `"is after"` | An operator in words, with whether the column holds dates |
| `FilterSummary` | `"Total is between 100 and 500"` | A filter in words, on chips and buttons |
| `FilterOnColumn`, `FilterDialog` | `"Filter by Status, now ..."`, `"Filter Status"` | A heading's filter button and its popup |
| `Operator`, `Value`, `Apply`, `ClearFilter`, `NoValues` | `"Operator"`, `"Value"`, `"Apply"`, `"Clear"`, `"No values"` | The popup |
| `FiltersInForce`, `RemoveFilter`, `MatchingAny`, `ClearAll` | `"Filters"`, `"Remove the filter ..."`, `"Matching any"`, `"Clear all"` | The chips |
| `FiltersButton`, `FilterBuilder`, `RowsMatch`, `MatchAll`, `MatchAny`, `NoConditions` | `"Filters (2)"`, `"Filter conditions"`, `"Rows match"`, `"all conditions"`, `"any condition"`, `"No conditions yet."` | The builder |
| `ConditionColumn`, `ConditionOperator`, `ConditionValue`, `ConditionSecondValue`, `RemoveCondition`, `And`, `AddCondition` | `"Column of condition 1"` and the rest | The builder's lines |
| `QuickFilters`, `AllRows` | `"Show"`, `"All"` | The quick filters |
| `Facets`, `ShowAllValues`, `ShowFewerValues` | `"Refine"`, `"Show all 12"`, `"Show fewer"` | The facet panel |
| `NoResultsFor`, `ClearSearchAndFilters` | `"No results for “report”."`, `"Clear search and filters"` | No results |

```razor
<MokaTable Items="_orders" Searchable FilterControls="MokaTableFilterControls.Chips | MokaTableFilterControls.Builder"
           Text="_words">
    <MokaColumn Title="Kunde" Field="o => o.Customer" Filterable />
</MokaTable>

@code {
    static readonly MokaTableText _words = MokaTableText.Default with
    {
        SearchPlaceholder = "Suchen...",
        ClearAll = "Alle entfernen",
        FiltersButton = count => count > 0 ? $"Filter ({count})" : "Filter",
        NoResultsFor = search => search is null ? "Keine Zeile passt zu den Filtern." : $"Nichts gefunden für „{search}“."
    };
}
```
