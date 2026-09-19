---
title: Command Palette
description: Searchable command overlay opened with Ctrl+K or Cmd+K, with groups, icons, shortcut hints and a configurable key.
order: 91
---

# Command Palette

`MokaCommandPalette` is a search overlay for running commands. You register commands with `IMokaCommandPaletteService`, and the palette lists them, filters them as the user types, and runs the one they choose. It opens with Ctrl+K (Cmd+K on a Mac) or from code. For a bar that stays on screen, see [Command Bar](command-bar).

## Setup

`AddMokaRed()` registers the service. With the individual Feedback package, `AddMokaFeedback()` does:

```csharp
// Program.cs
using Moka.Red.Feedback.Extensions;

builder.Services.AddMokaFeedback(); // registers IMokaCommandPaletteService
```

Place one `MokaCommandPalette` in the layout:

```razor
@* MainLayout.razor *@
<MokaDialogHost />
<MokaCommandPalette />
```

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Placeholder` | `string` | `"Search commands..."` | Search box placeholder |
| `NoResultsText` | `string` | `"No commands found"` | Shown when nothing matches |
| `MaxResults` | `int` | `20` | Most commands listed at once |
| `ShowShortcuts` | `bool` | `true` | Shows each command's `Shortcut` hint |
| `ShowGroups` | `bool` | `true` | Lists commands under their `Group` headings |
| `Shortcut` | `string?` | `"Mod+K"` | Key combination that opens and closes the palette. Null or empty turns it off. See [Shortcut](#shortcut) |

## MokaCommand

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | **required** | Unique key |
| `Title` | `string` | **required** | Display text. Searched |
| `Description` | `string?` | - | Second line under the title. Searched |
| `Icon` | `MokaIconDefinition?` | - | Icon before the title |
| `Group` | `string?` | - | Heading the command is listed under. Searched |
| `Keywords` | `string?` | - | Extra search terms, not shown |
| `Shortcut` | `string?` | - | Hint on the right, such as `"Ctrl+S"`. A label only: the palette does not bind the key |
| `OnExecute` | `Func<Task>?` | - | Runs when the command is chosen |
| `OnExecuteSync` | `Action?` | - | Runs when the command is chosen and `OnExecute` is not set |
| `Href` | `string?` | - | Navigates here after the action runs |
| `Disabled` | `bool` | `false` | Dimmed. Can be highlighted but doesn't run |

## IMokaCommandPaletteService

| Member | Description |
|--------|-------------|
| `Register(command)` | Adds a command. An `Id` that is already registered is ignored |
| `RegisterMany(commands)` | Registers each command in turn |
| `Unregister(id)` | Removes a command |
| `Commands` | Snapshot of the registered commands, in registration order |
| `Open()`, `Close()`, `Toggle()` | Show, hide or flip the palette |
| `IsOpen` | Whether the palette is open. Setting it opens or closes the palette |
| `OnToggle` | Raised when the palette opens or closes. `MokaCommandPalette` listens to it |

## Example

Click inside the preview first so it has keyboard focus, then press the shortcut, or use the button.

```blazor-preview
@inject IMokaCommandPaletteService Palette

@code {
    string _last = "nothing yet";

    protected override void OnInitialized() =>
        Palette.RegisterMany(
        [
            new MokaCommand { Id = "save", Title = "Save", Group = "Edit", Icon = MokaIcons.Action.Save, Shortcut = "Ctrl+S", OnExecuteSync = () => Run("Save") },
            new MokaCommand { Id = "find", Title = "Find in files", Group = "Edit", Icon = MokaIcons.Action.Search, Keywords = "search grep", OnExecuteSync = () => Run("Find in files") },
            new MokaCommand { Id = "home", Title = "Go to home", Group = "Go", Icon = MokaIcons.Navigation.Home, OnExecuteSync = () => Run("Go to home") },
            new MokaCommand { Id = "settings", Title = "Open settings", Group = "Go", Icon = MokaIcons.Action.Settings, Description = "Theme, density and font size", OnExecuteSync = () => Run("Open settings") },
            new MokaCommand { Id = "theme", Title = "Toggle dark mode", Group = "View", Icon = MokaIcons.Action.Moon, Keywords = "theme light", OnExecuteSync = () => Run("Toggle dark mode") },
            new MokaCommand { Id = "export", Title = "Export data", Group = "View", Icon = MokaIcons.Action.Download, Disabled = true }
        ]);

    // Commands run in the palette's handlers, so this component re-renders itself.
    void Run(string title)
    {
        _last = title;
        StateHasChanged();
    }
}

<div style="display:flex;flex-direction:column;align-items:center;justify-content:center;gap:8px;width:100%;min-height:420px">
    <MokaButton StartIcon="MokaIcons.Action.Search" OnClick="() => Palette.Open()">Open command palette</MokaButton>
    <MokaCaption>Or press <MokaKbd>Ctrl</MokaKbd> + <MokaKbd>K</MokaKbd> (<MokaKbd>Cmd</MokaKbd> + <MokaKbd>K</MokaKbd> on a Mac)</MokaCaption>
    <MokaCaption>Last command: @_last</MokaCaption>
</div>

<MokaCommandPalette />
```

## Registering Commands

Register app-wide commands once, for example in the layout. `Register` ignores an `Id` that is already taken, so the first registration wins. A component that registers its own commands should remove them when it goes away. Otherwise they keep pointing at the disposed component, and its next instance can't register replacements under the same ids.

```razor
@implements IDisposable
@inject IMokaCommandPaletteService Palette

@code {
    protected override void OnInitialized() =>
        Palette.RegisterMany(
        [
            new MokaCommand { Id = "orders.new", Title = "New order", Group = "Orders", Icon = MokaIcons.Action.Add, OnExecute = CreateOrderAsync },
            new MokaCommand { Id = "orders.export", Title = "Export orders", Group = "Orders", Keywords = "csv", OnExecute = ExportAsync },
            new MokaCommand { Id = "orders.settings", Title = "Order settings", Group = "Orders", Href = "/orders/settings" }
        ]);

    public void Dispose()
    {
        Palette.Unregister("orders.new");
        Palette.Unregister("orders.export");
        Palette.Unregister("orders.settings");
    }
}
```

## Search and Groups

The search is a case-insensitive substring match on `Title`, `Description`, `Keywords` and `Group`. With an empty search, the palette lists commands in registration order. Either way it shows at most `MaxResults`. Opening the palette clears the search.

With `ShowGroups`, commands appear under their `Group` headings. Groups are sorted by name, and commands without a group come first, with no heading.

## Running a Command

A click or Enter closes the palette, then runs `OnExecute` (or `OnExecuteSync` when `OnExecute` is not set), then navigates to `Href` if there is one. A command can do its work and then route.

The action runs inside the palette's event handler. When it changes something another component shows, that component calls `StateHasChanged()` itself.

## Keyboard

| Key | Action |
|-----|--------|
| `Shortcut` (Ctrl+K or Cmd+K by default) | Open or close the palette from anywhere on the page |
| Down / Up | Move the highlight, wrapping at the ends |
| Enter | Run the highlighted command |
| Escape | Close the palette |

The search box has focus while the palette is open. A click on the backdrop also closes it.

## Shortcut

`Shortcut` sets the key combination that opens and closes the palette. The default, `"Mod+K"`, is Cmd+K on macOS and iOS and Ctrl+K everywhere else.

```razor
<MokaCommandPalette Shortcut="Mod+Shift+P" />
```

Write the modifiers, then one key, joined with `+`. Case doesn't matter, and spaces around `+` are ignored.

| Modifier | Meaning |
|----------|---------|
| `Mod` | Cmd on macOS and iOS, Ctrl elsewhere |
| `Ctrl`, `Control` | Ctrl |
| `Cmd`, `Command`, `Meta` | Cmd, or the Windows key on Windows |
| `Alt`, `Option` | Alt, or Option on a Mac |
| `Shift` | Shift |

The key comes last. A letter or digit (`K`, `1`) matches the physical key as well as the character, so it still matches when Alt or Shift changes the character typed. `Space` means the space bar. Write any other key the way the browser names it in `KeyboardEvent.key`, such as `/`, `F1` or `Enter`. Code names such as `Slash` or `Period` don't match.

- The modifiers must match exactly. With the default, Ctrl+Shift+K does not open the palette, and the page keeps that key.
- Only a matching press has its default action cancelled.
- Holding the keys down toggles the palette once. Key repeats are ignored.
- A shortcut that types a character (no Ctrl, Cmd or Alt, such as `/` or `Shift+K`) is ignored while focus is in a text field, so the character still reaches the field. Other shortcuts work there too.
- A modifier name the palette doesn't know turns the shortcut off and logs a warning in the browser console, so `Win+K` never acts like a bare `K`.
- `null` or an empty string turns the shortcut off, and the palette opens only from code.
- Changing `Shortcut` at runtime rebinds it:

```razor
<MokaCommandPalette Shortcut="@_paletteKey" />

@code {
    // From the user's settings. Null turns the shortcut off.
    string? _paletteKey = "Mod+K";
}
```

Up to 0.1.11 the shortcut was fixed: Ctrl+K or Cmd+K opened the palette even with other modifiers held, so Ctrl+Shift+K opened it too. Every such press had its default action cancelled, and holding the keys toggled the palette on each repeat. With `ShowGroups` on, Enter could also run a different command than the highlighted one, since it counted in registration order while the list showed groups.
