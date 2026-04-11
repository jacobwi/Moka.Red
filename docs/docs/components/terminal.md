---
title: Terminal
description: Console output display for rendering command-line style text with colored lines.
order: 59
---

# Terminal

`MokaTerminal` renders a terminal/console-style output area with an optional header bar, scrollable content, line numbers, and a copy button. Individual lines can be styled with colors and prefixes using `MokaTerminalLine`.

## Parameters

### MokaTerminal

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Lines` | `IReadOnlyList<MokaTerminalLine>?` | -- | Structured lines to display |
| `ChildContent` | `RenderFragment?` | -- | Free-form content (alternative to `Lines`) |
| `Title` | `string?` | `"Terminal"` | Title shown in the header bar |
| `ShowHeader` | `bool` | `true` | Shows the terminal header with title and window controls |
| `MaxHeight` | `string` | `"300px"` | Maximum height before scrolling |
| `AutoScroll` | `bool` | `true` | Automatically scroll to the bottom when new lines appear |
| `ShowLineNumbers` | `bool` | `false` | Display line numbers in the gutter |
| `ShowCopyButton` | `bool` | `false` | Show a copy-to-clipboard button in the header |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaTerminalLine

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | -- | The line text content |
| `Color` | `string?` | -- | CSS color for the line (e.g., `"var(--moka-color-success)"`) |
| `Prefix` | `string?` | -- | Prefix shown before the text (e.g., `"$"`, `">"`) |

## Basic Output

```blazor-preview
<MokaTerminal Title="Build Output" Lines="@_buildLines" />

@code {
    IReadOnlyList<MokaTerminalLine> _buildLines =
    [
        new("$ dotnet build", Prefix: "$"),
        new("  Determining projects to restore..."),
        new("  Restored 3 projects in 1.2s", Color: "var(--moka-color-success)"),
        new("  Moka.Red.Core -> bin/Debug/net9.0/Moka.Red.Core.dll"),
        new("  Moka.Red.Primitives -> bin/Debug/net9.0/Moka.Red.Primitives.dll"),
        new("Build succeeded.", Color: "var(--moka-color-success)"),
        new("    0 Warning(s)", Color: "var(--moka-color-warning)"),
        new("    0 Error(s)", Color: "var(--moka-color-success)")
    ];
}
```

## Colored Lines

Use the `Color` property for semantic coloring of output lines.

```blazor-preview
<MokaTerminal Title="Deployment Log" Lines="@_deployLines" />

@code {
    IReadOnlyList<MokaTerminalLine> _deployLines =
    [
        new("[INFO]  Starting deployment...", Color: "var(--moka-color-info)"),
        new("[INFO]  Building Docker image...", Color: "var(--moka-color-info)"),
        new("[WARN]  Cache miss for layer 3/7", Color: "var(--moka-color-warning)"),
        new("[INFO]  Pushing to registry...", Color: "var(--moka-color-info)"),
        new("[ERROR] Connection timeout on push", Color: "var(--moka-color-error)"),
        new("[INFO]  Retrying (attempt 2/3)...", Color: "var(--moka-color-info)"),
        new("[OK]    Image pushed successfully", Color: "var(--moka-color-success)")
    ];
}
```

## With Line Numbers

Enable `ShowLineNumbers` for log-style output where line references matter.

```blazor-preview
<MokaTerminal Title="server.log" ShowLineNumbers="true" ShowCopyButton="true" Lines="@_serverLines" />

@code {
    IReadOnlyList<MokaTerminalLine> _serverLines =
    [
        new("Application starting..."),
        new("Listening on https://localhost:5001", Color: "var(--moka-color-success)"),
        new("GET /api/health 200 OK (12ms)"),
        new("POST /api/users 201 Created (45ms)"),
        new("GET /api/users/42 404 Not Found (8ms)", Color: "var(--moka-color-warning)"),
        new("Unhandled exception in middleware", Color: "var(--moka-color-error)"),
        new("  at Server.Middleware.Auth.ValidateToken()", Color: "var(--moka-color-error)")
    ];
}
```
