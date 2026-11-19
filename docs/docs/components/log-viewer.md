---
title: Log Viewer
description: Streaming log display with level filtering, search, row detail and virtualization.
order: 80
---

# Log Viewer

`MokaLogViewer` renders a scrollable, filterable log output. Feed it structured log entries and it handles timestamps, color-coded severity levels, search, level filters and auto-scroll. A row whose entry carries an exception or named values opens to show them. With `Virtualize` it draws only the rows in view, and with `ItemsProvider` it loads them from a server as they scroll in.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Entries` | `IReadOnlyList<MokaLogEntry>?` | `null` | Log entries to display. Set this or `ItemsProvider` |
| `ItemsProvider` | `ItemsProviderDelegate<MokaLogEntry>?` | `null` | Loads the entries in view on demand. Set this or `Entries`. Rows are virtualized whatever `Virtualize` says |
| `MaxEntries` | `int` | `500` | Keeps the newest entries and drops the oldest. Under `Virtualize` it applies only when set |
| `Virtualize` | `bool` | `false` | Draws only the rows in view through Blazor's `Virtualize`, each one line tall |
| `VirtualRowHeight` | `float` | `24` | Each row's height in pixels under `Virtualize`. Exact: rows are drawn and placed at it |
| `SearchText` | `string?` | `null` | The search box's text. Bind it with `@bind-SearchText` |
| `Levels` | `IReadOnlySet<MokaLogLevel>?` | `null` | The levels shown, every level when null. Bind it with `@bind-Levels` |
| `DetailTemplate` | `RenderFragment<MokaLogEntry>?` | `null` | Draws an opened row's detail in place of its exception and properties |
| `Text` | `MokaLogViewerText?` | English | Every word the viewer shows or says |
| `ShowTimestamp` | `bool` | `true` | Show timestamp column |
| `ShowLevel` | `bool` | `true` | Show log level badge |
| `ShowSearch` | `bool` | `true` | Show the search bar |
| `ShowLevelFilter` | `bool` | `true` | Show level filter buttons |
| `AutoScroll` | `bool` | `true` | Keep the newest entry in view while the list is at the bottom |
| `MonoFont` | `bool` | `true` | Draw messages, timestamps and sources in the mono font |
| `MaxHeight` | `string` | `"400px"` | Maximum height before scrolling. The detail pane under `Virtualize` takes the same cap |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

| Event | Type | Description |
|-------|------|-------------|
| `SearchTextChanged` | `EventCallback<string>` | Raised with the new text whenever the search changes |
| `LevelsChanged` | `EventCallback<IReadOnlySet<MokaLogLevel>>` | Raised with a new set of the levels shown whenever a level is turned on or off |

| Method | Description |
|--------|-------------|
| `RefreshDataAsync()` | Asks `ItemsProvider` for the rows in view again and draws them. Call it on the renderer's thread when the log behind the provider changes |

`MokaLogViewer` inherits from `MokaVisualComponentBase` (see [Base Classes](../api/base-classes)). `Id`, other attributes, `Margin`, `Padding` and `Rounded` land on the bordered frame, which clips its toolbar and list to the corners `Rounded` gives it. `Size`, `SizeValue`, `Color`, `Variant` and `Disabled` have no effect on it.

### MokaLogEntry

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | When the entry was logged |
| `Level` | `MokaLogLevel` | Severity: `Trace`, `Debug`, `Info`, `Warning`, `Error`, `Fatal` |
| `Message` | `string` | Log message text |
| `Source` | `string?` | Optional source/category name |
| `Exception` | `string?` | Init only. The exception logged with the entry as text, usually `exception.ToString()` |
| `Properties` | `IReadOnlyCollection<KeyValuePair<string, string>>?` | Init only. The entry's named values, shown in the order given |
| `HasDetail` | `bool` | Whether the entry has an `Exception` or `Properties`, so its row can be opened |

`Exception` and `Properties` are what structured loggers record beside the message: `Microsoft.Extensions.Logging` passes an exception and the message template's named values, OpenTelemetry a log record's exception and attributes, Serilog an exception and properties. Put an event id, a trace id or a scope's values in `Properties`. The exception is text rather than an `Exception` object, so an entry read back from a log store or sent to a browser carries it too.

### MokaLogViewerText

| Property | Default | Where |
|----------|---------|-------|
| `SearchPlaceholder` | `Search logs...` | The search box |
| `SearchLabel` | `Search logs` | Names the search box for screen readers |
| `NoEntries` | `No log entries` | The empty state |
| `EntriesLabel` | `Log entries` | Names the list, which is a tab stop under `Virtualize` |
| `ShowDetails` | `Show details` | The button that opens a row |
| `HideDetails` | `Hide details` | The same button once the row is open |
| `CloseDetails` | `Close details` | The detail pane's close button |

## Basic

```blazor-preview
<MokaLogViewer Entries="@_entries" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now.AddSeconds(-30), MokaLogLevel.Info, "Application started.", "Host"),
        new MokaLogEntry(DateTime.Now.AddSeconds(-20), MokaLogLevel.Debug, "Loading configuration from appsettings.json", "Config"),
        new MokaLogEntry(DateTime.Now.AddSeconds(-10), MokaLogLevel.Warning, "Cache miss for key 'user:42'", "Cache"),
        new MokaLogEntry(DateTime.Now.AddSeconds(-5), MokaLogLevel.Error, "Connection refused: db-primary:5432", "Database"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Info, "Retry succeeded, connected to db-replica.", "Database")
    };
}
```

## With Search

```blazor-preview
<MokaLogViewer Entries="@_entries" ShowSearch="true" ShowLevelFilter="true" MaxHeight="300px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now.AddMinutes(-5), MokaLogLevel.Info, "Request received: GET /api/users", "API"),
        new MokaLogEntry(DateTime.Now.AddMinutes(-4), MokaLogLevel.Info, "Request received: POST /api/orders", "API"),
        new MokaLogEntry(DateTime.Now.AddMinutes(-3), MokaLogLevel.Warning, "Slow query detected (1200ms)", "Database"),
        new MokaLogEntry(DateTime.Now.AddMinutes(-2), MokaLogLevel.Error, "Unhandled exception in OrderService", "API"),
        new MokaLogEntry(DateTime.Now.AddMinutes(-1), MokaLogLevel.Info, "Health check passed", "Monitor")
    };
}
```

## Filtered

```blazor-preview
<MokaLogViewer Entries="@_entries" ShowSearch="false" ShowLevelFilter="true" ShowTimestamp="false" MaxHeight="250px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Trace, "Entering method ProcessOrder", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Debug, "Order ID: 12345", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Info, "Order processed successfully", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Warning, "Inventory low for SKU-789", "Inventory"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Payment gateway timeout", "Payments")
    };
}
```

## Keeping the Search and Levels

`SearchText` and `Levels` are two-way. Bind them to keep the reader's search and level filter, to set them from the page, or to save them between visits. With `Entries` the viewer still filters on its own: the search matches each message and source, ignoring case.

```blazor-preview
<MokaLogViewer Entries="@_entries" @bind-SearchText="_search" @bind-Levels="_levels" MaxHeight="220px" />
<p>Search: "@_search", levels: @string.Join(", ", _levels ?? new HashSet<MokaLogLevel>())</p>

@code {
    private string? _search = "order";
    private IReadOnlySet<MokaLogLevel>? _levels = new HashSet<MokaLogLevel> { MokaLogLevel.Warning, MokaLogLevel.Error };

    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Info, "Order 12345 received", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Warning, "Order 12345 waits for stock", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Order 12346 payment declined", "Payments"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Disk almost full", "Host")
    };
}
```

## Row Detail

A row whose entry has an `Exception` or `Properties` gets a button at its end that opens the detail under the row: the exception in the mono font, then the properties as a list of names and values. `DetailTemplate` draws something else in their place.

```blazor-preview
<MokaLogViewer Entries="@_entries" MaxHeight="300px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now.AddSeconds(-2), MokaLogLevel.Info, "Charging order 4711", "Payments")
        {
            Properties = new Dictionary<string, string> { ["OrderId"] = "4711", ["Amount"] = "129.00 EUR" }
        },
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Payment failed", "Payments")
        {
            Exception = "System.TimeoutException: The gateway did not answer in 30 seconds.\n" +
                        "   at Payments.Gateway.ChargeAsync(Order order)\n" +
                        "   at Payments.Checkout.CompleteAsync(Order order)",
            Properties = new Dictionary<string, string> { ["OrderId"] = "4711", ["Attempt"] = "3" }
        }
    };
}
```

## Virtualized

`Virtualize` draws only the rows in view through Blazor's `Virtualize`, so a log of a hundred thousand lines costs the few dozen on screen. `Virtualize` places rows by one height, so each row is one line `VirtualRowHeight` pixels tall and a long message is cut at the end, its whole text in the row's tooltip. For the same reason an opened row's detail can't grow its row: it shows in a pane under the list instead, which keeps it in view while the list scrolls on. The list is a tab stop, so the keyboard can scroll rows that hold no button.

`MaxEntries` applies under `Virtualize` only when you set it, so a virtualized log shows every entry it is given.

```blazor-preview
<MokaLogViewer Entries="@_entries" Virtualize="true" MaxHeight="320px" />

@code {
    private static readonly string[] Sources = { "Api", "Worker", "Database", "Cache" };

    private IReadOnlyList<MokaLogEntry> _entries = Enumerable.Range(0, 100_000)
        .Select(i => new MokaLogEntry(DateTime.Today.AddMilliseconds(i * 250),
            i % 97 == 0 ? MokaLogLevel.Error : i % 13 == 0 ? MokaLogLevel.Warning : MokaLogLevel.Info,
            $"Handled request {i} in {i % 400} ms", Sources[i % Sources.Length])
        {
            Exception = i % 97 == 0 ? $"System.InvalidOperationException: Request {i} failed." : null
        })
        .ToList();
}
```

## Loading on Demand

For a log kept on a server, give `ItemsProvider` in place of `Entries`. It's asked only for the rows in view, plus a few beyond, and says how many there are in all, which sizes the scroll range. Searching and the level filter are then the provider's job: bind `SearchText` and `Levels`, read them in the provider, and the viewer asks it again whenever either changes, cancelling the request before. Call `RefreshDataAsync()` when new entries arrive.

```razor
@using Microsoft.AspNetCore.Components.Web.Virtualization

<MokaLogViewer @ref="_log" ItemsProvider="LoadAsync" @bind-SearchText="_search" @bind-Levels="_levels" />

@code {
    private MokaLogViewer _log = default!;
    private string? _search;
    private IReadOnlySet<MokaLogLevel>? _levels;

    private async ValueTask<ItemsProviderResult<MokaLogEntry>> LoadAsync(ItemsProviderRequest request)
    {
        LogPage page = await Logs.QueryAsync(_search, _levels, request.StartIndex, request.Count,
            request.CancellationToken);
        return new ItemsProviderResult<MokaLogEntry>(page.Entries, page.Total);
    }

    private Task OnNewEntries() => InvokeAsync(_log.RefreshDataAsync);
}
```

## Auto-Scroll

With `AutoScroll` on (the default) the list starts at its newest entry and stays there as entries arrive, while it's scrolled to the bottom. Scrolling up to read stops it, and scrolling back to the bottom starts it again. The position is read in the browser, so scrolling sends nothing to .NET. Turn it off for a log read from the top.
