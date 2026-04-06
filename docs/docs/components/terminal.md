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
<MokaTerminal Title="Build Output">
    <MokaTerminalLine Text="$ dotnet build" Prefix="$" />
    <MokaTerminalLine Text="  Determining projects to restore..." />
    <MokaTerminalLine Text="  Restored 3 projects in 1.2s" Color="var(--moka-color-success)" />
    <MokaTerminalLine Text="  Moka.Red.Core -> bin/Debug/net9.0/Moka.Red.Core.dll" />
    <MokaTerminalLine Text="  Moka.Red.Primitives -> bin/Debug/net9.0/Moka.Red.Primitives.dll" />
    <MokaTerminalLine Text="Build succeeded." Color="var(--moka-color-success)" />
    <MokaTerminalLine Text="    0 Warning(s)" Color="var(--moka-color-warning)" />
    <MokaTerminalLine Text="    0 Error(s)" Color="var(--moka-color-success)" />
</MokaTerminal>
```

## Colored Lines

Use the `Color` property for semantic coloring of output lines.

```blazor-preview
<MokaTerminal Title="Deployment Log">
    <MokaTerminalLine Text="[INFO]  Starting deployment..." Color="var(--moka-color-info)" />
    <MokaTerminalLine Text="[INFO]  Building Docker image..." Color="var(--moka-color-info)" />
    <MokaTerminalLine Text="[WARN]  Cache miss for layer 3/7" Color="var(--moka-color-warning)" />
    <MokaTerminalLine Text="[INFO]  Pushing to registry..." Color="var(--moka-color-info)" />
    <MokaTerminalLine Text="[ERROR] Connection timeout on push" Color="var(--moka-color-error)" />
    <MokaTerminalLine Text="[INFO]  Retrying (attempt 2/3)..." Color="var(--moka-color-info)" />
    <MokaTerminalLine Text="[OK]    Image pushed successfully" Color="var(--moka-color-success)" />
</MokaTerminal>
```

## With Line Numbers

Enable `ShowLineNumbers` for log-style output where line references matter.

```blazor-preview
<MokaTerminal Title="server.log" ShowLineNumbers="true" ShowCopyButton="true">
    <MokaTerminalLine Text="Application starting..." />
    <MokaTerminalLine Text="Listening on https://localhost:5001" Color="var(--moka-color-success)" />
    <MokaTerminalLine Text="GET /api/health 200 OK (12ms)" />
    <MokaTerminalLine Text="POST /api/users 201 Created (45ms)" />
    <MokaTerminalLine Text="GET /api/users/42 404 Not Found (8ms)" Color="var(--moka-color-warning)" />
    <MokaTerminalLine Text="Unhandled exception in middleware" Color="var(--moka-color-error)" />
    <MokaTerminalLine Text="  at Server.Middleware.Auth.ValidateToken()" Color="var(--moka-color-error)" />
</MokaTerminal>
```
