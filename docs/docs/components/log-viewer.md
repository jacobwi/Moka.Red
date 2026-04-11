---
title: Log Viewer
description: Streaming log display with level filtering and search.
order: 80
---

# Log Viewer

`MokaLogViewer` renders a scrollable, filterable log output. Feed it structured log entries and it handles timestamps, color-coded severity levels, search highlighting, and auto-scroll.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Entries` | `IReadOnlyList<MokaLogEntry>` | `[]` | Log entries to display |
| `MaxEntries` | `int` | `1000` | Maximum entries retained (oldest are dropped) |
| `ShowTimestamp` | `bool` | `true` | Show timestamp column |
| `ShowLevel` | `bool` | `true` | Show log level badge |
| `ShowSearch` | `bool` | `true` | Show the search bar |
| `ShowLevelFilter` | `bool` | `true` | Show level filter buttons |
| `AutoScroll` | `bool` | `true` | Auto-scroll to latest entry |
| `MaxHeight` | `string?` | `"400px"` | Maximum height before scrolling |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaLogEntry

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | When the entry was logged |
| `Level` | `MokaLogLevel` | Severity: `Trace`, `Debug`, `Info`, `Warning`, `Error`, `Fatal` |
| `Message` | `string` | Log message text |
| `Source` | `string?` | Optional source/category name |

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
