---
title: Terminal
description: Console output display for rendering command-line style text with colored lines.
order: 59
---

# Terminal

`MokaTerminal` renders a terminal/console-style output area with an optional header bar, scrollable content, line numbers, and a copy button. Individual lines can be styled with colors and prefixes using `MokaTerminalLine`, it can decode ANSI colors, and it can draw commands as blocks with their state and buttons.

## Parameters

### MokaTerminal

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Lines` | `IReadOnlyList<MokaTerminalLine>?` | -- | Structured lines to display |
| `Commands` | `IReadOnlyList<MokaTerminalCommand>?` | -- | Commands drawn as blocks. A separate mode from `Lines`, and it wins when both are set |
| `ChildContent` | `RenderFragment?` | -- | Free-form content, shown when there are no lines or commands |
| `Ansi` | `bool` | `false` | Decodes ANSI escape sequences in each line's text |
| `OnEdit` | `EventCallback<MokaTerminalCommand>` | -- | Raised by a block's Edit button, which shows only when this is set and the command has ended |
| `OnRunAgain` | `EventCallback<MokaTerminalCommand>` | -- | Raised by a block's Run again button, which shows only when this is set and the command has ended |
| `CopyLabel` | `string` | `"Copy"` | Text of a block's Copy button |
| `EditLabel` | `string` | `"Edit"` | Text of a block's Edit button |
| `RunAgainLabel` | `string` | `"Run again"` | Text of a block's Run again button |
| `StateLabel` | `Func<MokaTerminalCommandState, string>?` | -- | Names a command's state for a screen reader. Defaults to "Running", "Succeeded" and "Failed" |
| `Title` | `string?` | `"Terminal"` | Title shown in the header bar |
| `ShowHeader` | `bool` | `true` | Shows the terminal header with title and window controls |
| `MaxHeight` | `string` | `"400px"` | Maximum height before scrolling |
| `AutoScroll` | `bool` | `true` | Automatically scroll to the bottom when new lines appear |
| `ShowLineNumbers` | `bool` | `false` | Display line numbers in the gutter |
| `ShowCopyButton` | `bool` | `true` | Show a copy-to-clipboard button in the header |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaTerminalLine

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string` | -- | The line text content |
| `Color` | `string?` | -- | CSS color for the line (e.g., `"var(--moka-color-success)"`). A value that is not a CSS color is ignored |
| `Prefix` | `string?` | -- | Prefix shown before the text (e.g., `"$"`, `">"`) |

### MokaTerminalCommand

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Command` | `string` | -- | The command as typed |
| `State` | `MokaTerminalCommandState` | -- | `Running`, `Succeeded` or `Failed` |
| `Lines` | `IReadOnlyList<MokaTerminalLine>?` | -- | What it wrote so far |

## Long Output

Output is drawn in chunks of 200 lines, and a chunk whose lines haven't changed doesn't render again. Adding a line to a long log only redraws the last chunk, and Copy builds its text when clicked rather than on every render.

Pass the whole list each time, or the same list with lines added. A line rewritten long after it was written still shows, redrawing its own chunk. Dropping lines from the front moves every line into a different chunk, so a log trimmed that way redraws them all.

## ANSI Colors

With `Ansi` on, colors and weight (bold, dim, italic, underline, inverse) are drawn, and every other escape sequence and control character is dropped, so output can't move the cursor, clear the screen or set a title. It's off by default, which shows the text as it is. Copy takes the codes out.

```razor
<MokaTerminal Ansi Lines="@_lines" />

@code {
    IReadOnlyList<MokaTerminalLine> _lines =
    [
        new("\u001b[32mPassed!\u001b[0m 3477 tests in 41 s"),
        new("\u001b[33mwarn:\u001b[0m 1 test took over 5 s"),
        new("\u001b[31merror:\u001b[0m nothing to publish yet")
    ];
}
```

A color carries on to the next line until it is reset, as it does on a terminal.

Colors are classes that read theme tokens, never inline values, so red, green, amber and blue mean what they mean elsewhere and output written for a dark screen stays readable on a light theme. A 256-color or 24-bit color draws as the nearest of the 16 classic colors, so a gradient comes out in steps. A line with a background has rows that touch and doesn't wrap, which keeps a QR code drawn in half blocks whole.

## Command Blocks

`Commands` draws each command as a block: a status dot, the command, Copy, and its output below. The app runs commands, and the terminal only draws them, so pass a new record when the state or the lines change.

```razor
<MokaTerminal Ansi Commands="@_commands" OnEdit="PutOnTheLine" OnRunAgain="RunAsync" />

@code {
    IReadOnlyList<MokaTerminalCommand> _commands =
    [
        new("dotnet test", MokaTerminalCommandState.Succeeded, [new("\u001b[32mPassed!\u001b[0m 3477 tests in 41 s")]),
        new("dotnet nuget push", MokaTerminalCommandState.Failed, [new("\u001b[31merror:\u001b[0m nothing to publish yet")]),
        new("dotnet pack -c Release", MokaTerminalCommandState.Running)
    ];
}
```

Copy takes the command and its output, and the header's Copy takes every block. Edit and Run again show only when their callback is set and the command has ended, and each callback receives the command. A failed block has a red border. A running one's dot pulses in the info color rather than the accent, which on the red preset would read as failed. Each block's colors start fresh.

## Basic Output

The header's copy button is a `MokaCopyButton`, so its tooltip ("Copy", then "Copied!") opens below the header, lined up
with its right end.

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
