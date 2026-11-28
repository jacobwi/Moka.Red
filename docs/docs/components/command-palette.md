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
| `Label` | `string` | `"Command palette"` | Accessible name of the palette's dialog and of its list of commands. See [Accessibility](#accessibility) |
| `SearchLabel` | `string` | `"Search commands"` | Accessible name of the search box |
| `NoResultsText` | `string` | `"No commands found"` | Shown when nothing matches, and announced to screen readers |
| `MaxResults` | `int` | `20` | Most commands listed at once |
| `ShowShortcuts` | `bool` | `true` | Shows each command's `Shortcut` hint as key caps |
| `ShowGroups` | `bool` | `true` | Lists commands under their `Group` headings |
| `Shortcut` | `string?` | `"Mod+K"` | Key combination that opens and closes the palette. Null or empty turns it off. See [Shortcut](#shortcut) |
| `NewTabText` | `string` | `"opens in a new tab"` | What screen readers hear after the title of a command whose `Target` opens another tab, after a comma |
| `Matching` | `MokaCommandMatching` | `Contains` | How typed text matches: `Contains` finds it in one piece and keeps registration order, `Fuzzy` finds the letters in order and ranks the results. See [Search and Groups](#search-and-groups) |
| `Filter` | `Func<MokaCommand, string, int?>?` | `null` | The app's own matching: a score for each command, higher first, or null to leave it out. Wins over `Matching` |
| `GroupOrder` | `IReadOnlyList<string>?` | `null` | Group names to list first, in this order. The other groups follow by name |
| `ShowRecent` | `bool` | `false` | Lists the commands run last first, under a Recent heading, while the search box is empty. See [Recent Commands](#recent-commands) |
| `RecentCount` | `int` | `5` | Most recent commands `ShowRecent` lists |
| `Text` | `MokaCommandPaletteText` | defaults | The other words the palette writes. See [Words](#words) |

## MokaCommand

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | **required** | Unique key |
| `Title` | `string` | **required** | Display text. Searched |
| `Description` | `string?` | - | Second line under the title. Searched |
| `Icon` | `MokaIconDefinition?` | - | Icon before the title |
| `Group` | `string?` | - | Heading the command is listed under. Searched |
| `Keywords` | `string?` | - | Extra search terms, not shown |
| `Shortcut` | `string?` | - | Hint on the right, such as `"Ctrl+S"`, drawn as one `MokaKbd` cap per key. A label only: the palette does not bind the key. Split on `+`, so `"Ctrl++"` reads as Ctrl and the plus key |
| `OnExecute` | `Func<Task>?` | - | Runs when the command is chosen |
| `OnExecuteSync` | `Action?` | - | Runs when the command is chosen and `OnExecute` is not set |
| `Href` | `string?` | - | Navigates here after the action runs. A `javascript:`, `vbscript:` or `data:` URL is never navigated to |
| `Target` | `string?` | - | Where `Href` opens: `_blank` for a new tab, or a window's name. See [Opening Another Tab](#opening-another-tab). `_self`, `_parent` and `_top` navigate in the page as without one |
| `Disabled` | `bool` | `false` | Dimmed. Can be highlighted but doesn't run |
| `Children` | `IReadOnlyList<MokaCommand>?` | - | Commands one level down, opened in the palette when this one is picked. Such a command runs nothing itself. See [Commands With Children](#commands-with-children) |

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
| `Recent` | Ids of the commands run from the palette, the most recent first, each once |
| `SetRecent(ids)` | Replaces `Recent`, to restore a stored list or clear it. A repeated id keeps its first place |
| `RecentChanged` | Raised when `Recent` changes, so the app can store it |

`Recent`, `SetRecent` and `RecentChanged` have default bodies, so a service of your own written before them still compiles. It just keeps no recent commands.

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

By default the search is a case-insensitive substring match on `Title`, `Description`, `Keywords` and `Group`, and results keep registration order. With an empty search, the palette lists commands in registration order. Either way it shows at most `MaxResults`. Opening the palette clears the search.

With `ShowGroups`, commands appear under their `Group` headings. Groups are sorted by name, and commands without a group come first, with no heading. `GroupOrder` names groups to list first, in its order; the others follow by name.

```razor
<MokaCommandPalette GroupOrder="@(["Go", "Edit"])" />
```

### Ranked search

`Matching="MokaCommandMatching.Fuzzy"` finds the typed letters in order with anything between them, so `opst` finds "Open settings", and spaces in the search are ignored. The best matches come first: letters next to each other, letters that start a word (after a space or punctuation, or a capital inside a name) and a match in the title count for more. Equal matches keep registration order, and with `ShowGroups` the groups follow the order of their best result. Commands without a group still come first.

`Filter` puts the app's own matching in charge. It gets each command and the search as typed, and answers a score (higher comes first) or null to leave the command out. It wins over `Matching`, and it isn't asked while the search box is empty.

```razor
<MokaCommandPalette Filter="ScoreCommand" />

@code {
    // Commands the person uses often rank higher when the title matches.
    int? ScoreCommand(MokaCommand command, string search) =>
        command.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ? Uses(command.Id) : null;
}
```

## Recent Commands

The service remembers the commands run from the palette, the most recent first and each once, in `Recent`. With `ShowRecent`, the palette lists up to `RecentCount` of them first under a Recent heading while the search box is empty at the top level, and they leave their own groups while they show there. Ids whose command is gone are skipped, and stay in the list so a command registered again comes back. Commands with children aren't remembered, since they run nothing.

The list lives in the service for the session (scoped, so per circuit on Server). To keep it longer, store it when it changes and restore it with `SetRecent`:

```razor
@implements IDisposable
@inject IMokaCommandPaletteService Palette
@inject IRecentStore Store

@code {
    protected override void OnInitialized()
    {
        Palette.SetRecent(Store.Load());
        Palette.RecentChanged += Save;
    }

    void Save() => Store.Save(Palette.Recent);

    public void Dispose() => Palette.RecentChanged -= Save;
}
```

## Commands With Children

A command with `Children` opens them in the palette in its place, the way "Change theme" can open a list of themes. The search box clears, the command's title shows before it, and the search finds only these commands. Escape, or Backspace in an empty search box, goes back a level, with the command it came from highlighted. Escape at the top level closes the palette, and opening the palette again starts at the top.

A command with children runs nothing and goes nowhere itself: its `OnExecute`, `OnExecuteSync`, `Href` and `Target` are ignored, and its row ends in an arrow. Children can have children of their own. A child run from a lower level is remembered in `Recent` like any other, and `ShowRecent` finds it there.

```csharp
Palette.Register(new MokaCommand
{
    Id = "theme",
    Title = "Change theme",
    Icon = MokaIcons.Action.Moon,
    Children =
    [
        new MokaCommand { Id = "theme.dark", Title = "Dark", OnExecuteSync = () => Look.Use(Themes.Dark) },
        new MokaCommand { Id = "theme.light", Title = "Light", OnExecuteSync = () => Look.Use(Themes.Light) }
    ]
});
```

## Words

`Text` holds the words the palette writes besides the ones it has parameters for. Set the ones to change, or all of them to translate it.

| Property | Default | Where |
|----------|---------|-------|
| `Navigate` | `"Navigate"` | Footer hint beside the arrow keys |
| `Select` | `"Select"` | Footer hint beside Enter |
| `Close` | `"Close"` | Footer hint beside Escape at the top level |
| `Back` | `"Back"` | Footer hint beside Escape on a lower level |
| `Recent` | `"Recent"` | Heading over the recent commands |
| `PageOpened` | the title | What screen readers hear when a command's children open, given its title |
| `ShortcutDescription` | `"Open the command palette"` | How a shortcut list names the palette's `Shortcut`, when it's registered with the app's hotkeys |

```razor
<MokaCommandPalette Text="@(new MokaCommandPaletteText { Navigate = "Bewegen", Select = "Wählen", Close = "Schließen", Back = "Zurück", Recent = "Zuletzt" })" />
```

## Running a Command

A click or Enter closes the palette, then runs `OnExecute` (or `OnExecuteSync` when `OnExecute` is not set), then navigates to `Href` if there is one. A command can do its work and then route.

The action runs inside the palette's event handler. When it changes something another component shows, that component calls `StateHasChanged()` itself.

## Opening Another Tab

A command with `Href` and `Target="_blank"` (or a window's name) opens the page in another tab, for a page outside the app. Its row is a link with `rel="noopener noreferrer"` and an external-link mark, and screen readers hear ", opens in a new tab" after its title (`NewTabText`).

- A click, Ctrl+click or middle click is the link's own, so the browser opens the tab. A click also closes the palette and runs `OnExecute`.
- Enter opens the tab first, then closes the palette and runs `OnExecute`. The palette keeps its highlight in .NET, which knows which row Enter meant after a run of arrow presses, and opens the tab as soon as it hears the key. Browsers let a page open a tab for five seconds after a key press (Chromium, Firefox and WebKit alike), so the popup blocker allows it.
- The page itself stays where it is.

```csharp
Palette.Register(new MokaCommand
{
    Id = "router-admin",
    Title = "Open the router's admin page",
    Href = "https://192.168.1.1/",
    Target = "_blank"
});
```

## Keyboard

| Key | Action |
|-----|--------|
| `Shortcut` (Ctrl+K or Cmd+K by default) | Open or close the palette from anywhere on the page |
| Down / Up | Move the highlight, wrapping at the ends |
| Enter | Run the highlighted command, or open its children |
| Escape | Close the palette, or go back a level from a command's children |
| Backspace | In an empty search box on a lower level, go back a level |

The search box has focus while the palette is open. A click on the backdrop also closes it. The list scrolls itself to keep the highlighted command in view as the arrow keys move it, with its group's heading when it's the first in its group, and goes back to its top when the search changes. Only the list scrolls, never the page behind it. Up to 0.12.1 the highlight could move out of view in a long list.

## Accessibility

The palette follows the WAI-ARIA combobox pattern with a listbox popup, as in the APG's editable combobox with list autocomplete. Focus stays in the search box while the arrow keys move the highlight, and the search box tells screen readers which command is highlighted, so they read each command as the highlight reaches it.

- The palette is a modal dialog (`role="dialog"`, `aria-modal="true"`) named by `Label`. While it's open, Tab stays inside it, and when it closes focus goes back to where it was, as with `MokaDialog`.
- The search box is a combobox named by `SearchLabel`. It has `aria-autocomplete="list"`, `aria-controls` naming the list, `aria-expanded` true while commands show, and `aria-activedescendant` naming the highlighted command, left out when nothing is highlighted.
- The commands are a listbox named by `Label`. Each command is an option with an id built from the list's id and its place in the list, `aria-selected="true"` on the highlighted one only, and `aria-disabled="true"` when it's `Disabled`. A command that opens another tab is still a link, so a middle click and the browser's copy-link work, with the option role.
- With `ShowGroups`, each group is a `role="group"` named by its heading through `aria-labelledby`. Commands without a group stand in the list itself.
- On a command's children, the command's title before the search box describes it (`aria-describedby`), and the live region says `Text.PageOpened` for the title as the level opens. Going back to the top level says `Label`.
- When nothing matches, a polite live region (`role="status"`) says `NoResultsText`. The region is on the page from the moment the palette opens and fills one render later, since screen readers skip a region that arrives with its text already in it. The visible "No commands found" is hidden from screen readers, so it isn't read twice.

Name the palette in the app's language:

```razor
<MokaCommandPalette Label="Befehlspalette"
                    SearchLabel="Befehle suchen"
                    Placeholder="Befehl suchen..."
                    NoResultsText="Keine Befehle gefunden" />
```

The rendered structure, with ids shortened:

```html
<div class="moka-command-palette" role="dialog" aria-modal="true" aria-label="Command palette">
  <input role="combobox" aria-label="Search commands" aria-expanded="true"
         aria-controls="listbox" aria-autocomplete="list" aria-activedescendant="listbox-option-1">
  <div id="listbox" role="listbox" aria-label="Command palette">
    <div role="option" id="listbox-option-0">Help</div>
    <div role="group" aria-labelledby="listbox-group-1">
      <div id="listbox-group-1" role="presentation">Edit</div>
      <div role="option" id="listbox-option-1" aria-selected="true">Save</div>
      <a role="option" id="listbox-option-2" href="https://example.com/" target="_blank">Open the docs</a>
    </div>
  </div>
  <div class="moka-visually-hidden" role="status"></div>
</div>
```

Up to 0.11.3 nothing in the palette carried a role or a name: screen readers heard a text box, but not the list, the highlighted command or how many there were, and an empty list went unannounced.

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
- With the app's hotkeys registered (`AddMokaRed()` or `AddMokaHotkeys()`) and a `MokaHotkeyHost` in the layout, the palette registers its shortcut there as a hotkey instead of listening itself. It then shares the host's one listener and shows in `MokaCheatsheet` under `Text.ShortcutDescription`, and it still toggles over other dialogs and stays out of a text field when it types a character. Without a host the palette keeps its own listener, and it moves between the two when a host comes or goes.
- Changing `Shortcut` at runtime rebinds it:

```razor
<MokaCommandPalette Shortcut="@_paletteKey" />

@code {
    // From the user's settings. Null turns the shortcut off.
    string? _paletteKey = "Mod+K";
}
```

Up to 0.1.11 the shortcut was fixed: Ctrl+K or Cmd+K opened the palette even with other modifiers held, so Ctrl+Shift+K opened it too. Every such press had its default action cancelled, and holding the keys toggled the palette on each repeat. With `ShowGroups` on, Enter could also run a different command than the highlighted one, since it counted in registration order while the list showed groups.
