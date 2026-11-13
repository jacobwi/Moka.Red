---
title: "AI Assistants"
description: "Give AI coding assistants Moka.Red's components, parameters and examples through its MCP server"
order: 6
---

# AI Assistants

`Moka.Red.Mcp` is a Model Context Protocol (MCP) server for AI coding assistants such as Claude Code, GitHub Copilot and Cursor. It answers from the Moka.Red version it ships with, so an assistant looks up real component names, parameters, defaults and examples instead of guessing them.

## Requirements

- .NET 10 SDK or later. Its `dnx` command downloads the server from NuGet and runs it.

## Add it to your assistant

### Claude Code

```bash
claude mcp add moka-red -- dotnet dnx Moka.Red.Mcp --yes
```

Add `--scope user` to have it in every project.

### VS Code with GitHub Copilot

In `.vscode/mcp.json`:

```json
{
  "servers": {
    "moka-red": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["dnx", "Moka.Red.Mcp", "--yes"]
    }
  }
}
```

### Other clients

Any client that starts a stdio MCP server works: the command is `dotnet` and the arguments are `dnx Moka.Red.Mcp --yes`.

## Match your Moka.Red version

`dnx Moka.Red.Mcp` runs the newest release. To answer for the version your app uses, pin it the same way: `dnx Moka.Red.Mcp@x.y.z --yes`, with `x.y.z` your `Moka.Red` package version.

## Tools

| Tool | What it gives |
|------|---------------|
| `search_components` | Components by name or by what they do, plus the docs pages and types that match |
| `list_components` | Every component, by package |
| `get_component` | Parameters with types, defaults and descriptions, events, child content slots and enum values |
| `get_component_docs` | A component's docs page with its examples, whole or one section |
| `get_type` | Records, options, enums, services and icon sets, such as `MokaLineSeries`, `IMokaToastService` or `MokaIcons.Action` |
| `get_getting_started` | These setup guides |

The server reads only what it ships with. It doesn't open your project's files or make network calls.
