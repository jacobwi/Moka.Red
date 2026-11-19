---
title: Log Viewer
description: Streaming log display with level filtering, search, row detail and virtualization.
order: 80
---

# Log Viewer

`MokaLogViewer` renders a scrollable, filterable log output. Feed it structured log entries and it handles timestamps, color-coded severity levels, search, level filters and auto-scroll. A row whose entry carries an exception or named values opens to show them. With `Virtualize` it draws only the rows in view, and with `ItemsProvider` it loads them from a server as they scroll in. It can show the newest line first, page older lines in, filter by a lowest level and search only when asked.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Entries` | `IReadOnlyList<MokaLogEntry>?` | `null` | Log entries to display, oldest first. Set this or `ItemsProvider` |
| `ItemsProvider` | `ItemsProviderDelegate<MokaLogEntry>?` | `null` | Loads the entries in view on demand, in `Order`. Set this or `Entries`. Rows are virtualized whatever `Virtualize` says |
| `MaxEntries` | `int` | `500` | Keeps the newest entries and drops the oldest. Under `Virtualize`, and once `OnLoadOlder` has a listener, it applies only when set |
| `Virtualize` | `bool` | `false` | Draws only the rows in view through Blazor's `Virtualize`, each one line tall |
| `VirtualRowHeight` | `float` | `24` | Each row's height in pixels under `Virtualize`. Exact: rows are drawn and placed at it |
| `Order` | `MokaLogOrder` | `OldestFirst` | `OldestFirst` puts the newest line at the bottom, `NewestFirst` at the top |
| `HasOlder` | `bool` | `false` | Shows a "Show older lines" button at the old end of the list, which raises `OnLoadOlder` |
| `SearchText` | `string?` | `null` | The text searched for. Bind it with `@bind-SearchText` |
| `SearchMode` | `MokaLogSearchMode` | `Live` | `Live` searches as the reader types, `OnSubmit` when they press Enter or the search button |
| `SearchDebounce` | `TimeSpan?` | `null` | Under `Live`, how long typing has to pause before the search runs. Null searches on every key |
| `Levels` | `IReadOnlySet<MokaLogLevel>?` | `null` | The levels shown under `Toggles`, every level when null. Bind it with `@bind-Levels` |
| `LevelFilter` | `MokaLogLevelFilter` | `Toggles` | `Toggles` is a button per level, `AtLeast` one list of the lowest level shown |
| `MinimumLevel` | `MokaLogLevel` | `Trace` | The lowest level shown under `AtLeast`. Bind it with `@bind-MinimumLevel` |
| `TimestampFormat` | `string?` | `null` | A .NET date format for the timestamps, in the current culture. Null writes `HH:mm:ss.fff` |
| `TimestampFormatter` | `Func<DateTime, string>?` | `null` | Writes each timestamp, given the time in `TimeZone`. Wins over `TimestampFormat` and `Hour12` |
| `Hour12` | `bool?` | `null` | A 12-hour clock. Null follows `TimestampFormat`'s hours, and the 24-hour clock without one |
| `TimeZone` | `TimeZoneInfo?` | `null` | The zone timestamps are written in. Null writes them as the entries hold them |
| `DetailTemplate` | `RenderFragment<MokaLogEntry>?` | `null` | Draws an opened row's detail in place of its exception and properties |
| `Text` | `MokaLogViewerText?` | English | Every word the viewer shows or says |
| `ShowTimestamp` | `bool` | `true` | Show timestamp column |
| `ShowLevel` | `bool` | `true` | Show log level badge |
| `ShowSearch` | `bool` | `true` | Show the search bar |
| `ShowLevelFilter` | `bool` | `true` | Show the level filter, buttons or a list as `LevelFilter` says |
| `AutoScroll` | `bool` | `true` | Keep the newest entry in view while the list is at its newest end |
| `MonoFont` | `bool` | `true` | Draw messages, timestamps and sources in the mono font |
| `MaxHeight` | `string` | `"400px"` | Maximum height before scrolling. The detail pane under `Virtualize` takes the same cap |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

| Event | Type | Description |
|-------|------|-------------|
| `SearchTextChanged` | `EventCallback<string>` | Raised with the text searched for whenever it changes: after the pause under `SearchDebounce`, when sent under `OnSubmit`, never with text still being typed |
| `LevelsChanged` | `EventCallback<IReadOnlySet<MokaLogLevel>>` | Raised with a new set of the levels shown whenever a level is turned on or off |
| `MinimumLevelChanged` | `EventCallback<MokaLogLevel>` | Raised with the lowest level the reader picks under `AtLeast` |
| `OnLoadOlder` | `EventCallback` | Raised by the older-lines button. The button is busy until it completes |

| Method | Description |
|--------|-------------|
| `RefreshDataAsync()` | Asks `ItemsProvider` for the rows in view again and draws them. Call it on the renderer's thread when the log behind the provider changes. Under `NewestFirst`, rows counted beyond the last answer are new ones at the top, and a reader further down keeps their place |

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
| `SearchButton` | `Search` | Names the search button under `OnSubmit` and its tooltip |
| `MinimumLevelLabel` | `Minimum level` | Names the level list under `AtLeast` |
| `AtLeast` | `All levels`, `Debug and above` to `Fatal only` | A `Func<MokaLogLevel, string>`: each choice in the level list, given the lowest level it shows |
| `ShowOlder` | `Show older lines` | The button `HasOlder` shows |
| `LoadingOlder` | `Loading older lines` | Said to screen readers while older lines load |

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

For a log kept on a server, give `ItemsProvider` in place of `Entries`. It's asked only for the rows in view, plus a few beyond, and says how many there are in all, which sizes the scroll range. Searching and the level filter are then the provider's job: bind `SearchText` and `Levels`, or `MinimumLevel` under `LevelFilter="MokaLogLevelFilter.AtLeast"`, read them in the provider, and the viewer asks it again whenever one changes, cancelling the request before. A search that costs a round trip suits `SearchMode="MokaLogSearchMode.OnSubmit"` or a `SearchDebounce`. Call `RefreshDataAsync()` when new entries arrive.

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

With `AutoScroll` on (the default) the list starts at its newest entry and stays there as entries arrive, while it's scrolled to the bottom. Scrolling up to read stops it, and scrolling back to the bottom starts it again. The position is read in the browser, so scrolling sends nothing to .NET. Under `NewestFirst` the newest end is the top, and the same holds there. Turn it off for a log read from the top.

## Timestamps

By default a timestamp is the time of day, `HH:mm:ss.fff`, written as the entry holds it. `TimestampFormat` takes any .NET date format, written in the current culture: add the day for a log that covers a week, or the whole date for one of any age. `Hour12` puts the time on a 12-hour clock, moving `HH` in your format to `h` and adding the AM or PM designator, as a chart's time axis does. `TimeZone` writes every timestamp in that zone: a `Utc` or `Local` timestamp is moved into it, and one whose `Kind` is `Unspecified` is read as a time there already. `TimestampFormatter` writes the text itself, given the time in the zone.

With any of these set, the timestamp is a `time` element whose `datetime` holds the moment in full, with its offset, so the date a short format leaves out is still in the page. Without them the markup is what it always was.

```blazor-preview
<MokaLogViewer Entries="@_entries" TimestampFormat="ddd d MMM, h:mm:ss tt" TimeZone="@TimeZoneInfo.Utc" MaxHeight="200px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.UtcNow.AddDays(-6), MokaLogLevel.Info, "Backup finished", "Backup"),
        new MokaLogEntry(DateTime.UtcNow.AddDays(-2), MokaLogLevel.Warning, "Disk 81% full", "Host"),
        new MokaLogEntry(DateTime.UtcNow, MokaLogLevel.Info, "Backup finished", "Backup")
    };
}
```

On Blazor Server, `TimeZoneInfo.Local` is the server's zone. To write the reader's own times, set a scoped `TimeProvider` from the browser's zone as [Relative Time](relative-time) shows, and pass its `LocalTimeZone` as `TimeZone`. The viewer doesn't pick that zone up by itself, so a log written in UTC stays in UTC unless you ask.

## Newest First and Older Lines

`Order="MokaLogOrder.NewestFirst"` puts the newest line at the top, as a feed does. `Entries` are still given oldest first, so a log that appends needs no change. A reader scrolled down stays on the line they're reading while new lines arrive above it.

`HasOlder` puts a "Show older lines" button at the old end of the list, the top for `OldestFirst` and the bottom for `NewestFirst`. It raises `OnLoadOlder`: add the older entries to the start of `Entries`, and turn `HasOlder` off once the start of the log is in. The button shows it's busy until the callback completes and ignores more presses meanwhile. Lines added above the row the reader is on leave that row where it is, and when the button goes, focus goes to the list. With `OnLoadOlder` set, `MaxEntries` applies only when you set it, so the lines you page in stay.

```blazor-preview
<MokaLogViewer Entries="@_entries" HasOlder="@(_oldest > 0)" OnLoadOlder="LoadOlderAsync" MaxHeight="220px" />

@code {
    private int _oldest = 80;
    private List<MokaLogEntry> _entries = Lines(80, 20);

    private static List<MokaLogEntry> Lines(int from, int count) => Enumerable.Range(from, count)
        .Select(i => new MokaLogEntry(DateTime.Today.AddMinutes(i), MokaLogLevel.Info, $"Line {i}", "Worker"))
        .ToList();

    private async Task LoadOlderAsync()
    {
        await Task.Delay(400);
        int from = Math.Max(0, _oldest - 20);
        _entries.InsertRange(0, Lines(from, _oldest - from));
        _oldest = from;
    }
}
```

With `ItemsProvider`, the provider already pages: `Virtualize` asks it for whichever rows are in view, so a provider that serves the whole log needs no button. It returns rows in `Order`, so under `NewestFirst` row 0 is the newest entry. `HasOlder` still works with a provider that serves only what has been fetched so far: once `OnLoadOlder` completes, the viewer asks the provider again and takes the rows it counts beyond its last answer as the older ones.

## Level Threshold

`LevelFilter="MokaLogLevelFilter.AtLeast"` replaces the six level buttons with one list of the lowest level shown, which shows that level and every worse one, as `journalctl -p` does. It's a native `select`: it says the threshold in words, takes little room in a toolbar that wraps on a phone, and leaves the keys and the phone's picker to the browser. Bind `MinimumLevel` to keep or set it, or to read it in a provider. `Levels` is left alone in this mode.

```blazor-preview
<MokaLogViewer Entries="@_entries" LevelFilter="MokaLogLevelFilter.AtLeast" @bind-MinimumLevel="_minimum" MaxHeight="200px" />
<p>Showing @_minimum and above.</p>

@code {
    private MokaLogLevel _minimum = MokaLogLevel.Warning;

    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Debug, "Cache warmed", "Cache"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Info, "Request handled", "Api"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Warning, "Slow query (1200 ms)", "Database"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Payment gateway timeout", "Payments")
    };
}
```

## Search Timing

By default the viewer searches on every key, which suits `Entries` held in memory. `SearchDebounce` waits until typing pauses that long, and `SearchMode="MokaLogSearchMode.OnSubmit"` waits for Enter or the search button, for a provider whose search costs a round trip. In every mode `SearchTextChanged` reports the text the viewer searches for, never text still being typed, so a provider bound to `SearchText` is asked once per search. The box keeps every key the reader types: it binds the browser's own value, and typing a search that waits draws nothing.

```blazor-preview
<MokaLogViewer Entries="@_entries" SearchDebounce="TimeSpan.FromMilliseconds(300)" @bind-SearchText="_debounced" MaxHeight="160px" />
<p>Searched for: "@_debounced"</p>
<MokaLogViewer Entries="@_entries" SearchMode="MokaLogSearchMode.OnSubmit" @bind-SearchText="_sent" MaxHeight="160px" />
<p>Searched for: "@_sent"</p>

@code {
    private string? _debounced;
    private string? _sent;

    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Info, "Order 12345 received", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Warning, "Order 12345 waits for stock", "Orders"),
        new MokaLogEntry(DateTime.Now, MokaLogLevel.Error, "Payment declined", "Payments")
    };
}
```

## Narrow Widths

A row keeps the time, level, source and message on one line while the message has at least 16em beside them. With less, as on a phone or with a long source name, the message takes a line of its own at the row's full width, and a source longer than the row breaks inside it. A virtualized row can't wrap, since every row is one height, so there the message keeps at least 16em or 30% of the row and a longer source is cut at the end, its whole name in the source's tooltip. Where the message has room, rows look as they did. The toolbar wraps too: the level filter moves to a row of its own when it doesn't fit beside the search, and where it does, the search box shrinks to the room left, with its button under `OnSubmit`.
