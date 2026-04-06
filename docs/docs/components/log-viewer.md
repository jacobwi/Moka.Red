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
        new MokaLogEntry { Timestamp = DateTime.Now.AddSeconds(-30), Level = MokaLogLevel.Info, Message = "Application started.", Source = "Host" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddSeconds(-20), Level = MokaLogLevel.Debug, Message = "Loading configuration from appsettings.json", Source = "Config" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddSeconds(-10), Level = MokaLogLevel.Warning, Message = "Cache miss for key 'user:42'", Source = "Cache" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddSeconds(-5), Level = MokaLogLevel.Error, Message = "Connection refused: db-primary:5432", Source = "Database" },
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Info, Message = "Retry succeeded, connected to db-replica.", Source = "Database" }
    };
}
```

## With Search

```blazor-preview
<MokaLogViewer Entries="@_entries" ShowSearch="true" ShowLevelFilter="true" MaxHeight="300px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry { Timestamp = DateTime.Now.AddMinutes(-5), Level = MokaLogLevel.Info, Message = "Request received: GET /api/users", Source = "API" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddMinutes(-4), Level = MokaLogLevel.Info, Message = "Request received: POST /api/orders", Source = "API" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddMinutes(-3), Level = MokaLogLevel.Warning, Message = "Slow query detected (1200ms)", Source = "Database" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddMinutes(-2), Level = MokaLogLevel.Error, Message = "Unhandled exception in OrderService", Source = "API" },
        new MokaLogEntry { Timestamp = DateTime.Now.AddMinutes(-1), Level = MokaLogLevel.Info, Message = "Health check passed", Source = "Monitor" }
    };
}
```

## Filtered

```blazor-preview
<MokaLogViewer Entries="@_entries" ShowSearch="false" ShowLevelFilter="true" ShowTimestamp="false" MaxHeight="250px" />

@code {
    private IReadOnlyList<MokaLogEntry> _entries = new[]
    {
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Trace, Message = "Entering method ProcessOrder", Source = "Orders" },
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Debug, Message = "Order ID: 12345", Source = "Orders" },
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Info, Message = "Order processed successfully", Source = "Orders" },
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Warning, Message = "Inventory low for SKU-789", Source = "Inventory" },
        new MokaLogEntry { Timestamp = DateTime.Now, Level = MokaLogLevel.Error, Message = "Payment gateway timeout", Source = "Payments" }
    };
}
```
