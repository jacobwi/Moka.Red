---
title: Slash Menu
description: The "/" insert menu of note editors, anchored to a textarea. The textarea keeps focus and hands keys to the menu through HandleKeyAsync.
order: 103
---

# Slash Menu

`MokaSlashMenu` is the "/" menu found in note and document editors. The user types a slash, narrows the list by typing, and picks a block with Enter or a click. The editor then inserts it. The menu never takes focus. The host textarea keeps it, opens the menu, passes the text typed after the slash as `Query`, and hands its key presses to the menu through `HandleKeyAsync`. See [Host Contract](#host-contract).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Open` | `bool` | `false` | Shows the menu. Two-way bindable. The menu closes itself after a selection or Escape and reports it through `OpenChanged` |
| `OpenChanged` | `EventCallback<bool>` | -- | Raised with `false` when the menu closes itself |
| `Items` | `IReadOnlyList<MokaSlashMenuItem>?` | -- | Every item the menu can offer. On the new design, items that share a `Category` are listed under it, see [Groups and Shortcuts](#groups-and-shortcuts) |
| `Query` | `string?` | -- | Text typed after the slash. Trimmed, then matched case-insensitively against each item's `Title`, `Keywords` and `Category`. A new query moves the highlight back to the first row |
| `OnSelect` | `EventCallback<MokaSlashMenuItem>` | -- | Raised when the user picks an item with Enter or a click. The menu closes right after |
| `MaxVisible` | `int` | `8` | Most matches listed at once, counted in the order they are shown. Below 1 lists every match |
| `Header` | `string?` | `"Insert"` | Uppercase label above the list. Null or empty hides it. Turn it off when the group headings say enough |
| `ShowHints` | `bool` | `true` | Key hints as `MokaKbd` caps. On the new design, a footer with a word beside each cap (Move, Insert and Close, or New line and Close while nothing matches), shown with or without a header. On `Dark` and `Light`, caps in the header, so only with a header |
| `EmptyText` | `string?` | `Nothing called "{query}"` | New design only. First line of the empty state, shown when the query matches nothing. `{query}` is replaced with what was typed. Null or empty hides the line |
| `EmptyHint` | `string?` | `"Enter adds a new line. Esc closes this and leaves the slash as text."` | New design only. Second line of the empty state, saying what the keys do now. Change it when your editor does something else with Enter. Null or empty hides the line |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles. Without `Anchor`, use them to place the menu, see [Placement](#placement) |
| `Anchor` | `ElementReference?` | -- | The element the menu opens against, usually the textarea. With it the menu opens in the browser's top layer, so a card that hides its overflow can't cut it off. See [Placement](#placement) |
| `Placement` | `MokaPopoverPosition` | `BottomStart` | Where the menu opens against `Anchor`, flipping when there's no room |

## MokaSlashMenuItem

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Title` | `string` | **required** | Row text. Searched |
| `Description` | `string?` | -- | Second line under the title, cut off with an ellipsis. Not searched |
| `Category` | `string?` | -- | The group the item is listed in, drawn once as a heading over the group on the new design, and as a tag on the right of the row on `Dark` and `Light`. Searched |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the title, in a small tile on the new design |
| `Shortcut` | `string?` | -- | What the user could type instead of picking the item, such as `"##"`, drawn as key caps on the right of the row on the new design. `"Ctrl+Alt+2"` is drawn as one cap per key. Not searched, and not drawn on `Dark` and `Light` |
| `Keywords` | `string?` | -- | Extra search terms, never shown |
| `Value` | `object?` | -- | Your payload, such as the text to insert. `OnSelect` hands the item back with it |

## Groups and Shortcuts

This section is the new design, which every theme but `MokaTheme.Dark` and `Light` uses. Those two are the original design and keep its look, so an app that names them upgrades without a change: rows in `Items` order, the category as a tag on each row, bare icons, the key hints in the header, "No matches" when nothing matches, and no shortcut caps. The menu reads the cascaded `MokaTheme` to tell which, and redraws when it changes.

Give items a `Category` and the menu lists them in groups, with the category drawn once as a heading over each group. Groups come in the order their first item appears in `Items`, and an item keeps its place within its group. Items without a category come first, with no heading over them, so they never look like part of another group.

The arrow keys, Enter, `MaxVisible` and the option ids all count rows in the order they are shown, so `{ListboxId}-option-1` is the second row on screen whatever order `Items` was in.

`Shortcut` tells the user what they could type instead next time. Markdown editors usually have one for most blocks:

```razor
<MokaSlashMenu @ref="_menu" @bind-Open="_menuOpen" Items="_items" Query="@_query" OnSelect="Insert"
               Header="@null" />

@code {
    static readonly IReadOnlyList<MokaSlashMenuItem> _items =
    [
        new() { Title = "Heading 1", Description = "Big section title", Category = "Text", Shortcut = "#", Value = "# " },
        new() { Title = "Heading 2", Description = "Smaller heading", Category = "Text", Shortcut = "##", Value = "## " },
        new() { Title = "Quote", Description = "Set a passage apart", Category = "Text", Shortcut = ">", Value = "> " },
        new() { Title = "Bullet list", Description = "A plain list", Category = "Lists", Shortcut = "-", Value = "- " },
        new() { Title = "Checklist", Description = "Things to tick off", Category = "Lists", Shortcut = "[ ]", Value = "- [ ] " },
        new() { Title = "Code block", Description = "Mono, with colors", Category = "Insert", Icon = MokaIcons.File.Code, Shortcut = "```", Value = "```\n\n```" }
    ];
}
```

On the new design the menu is 360px wide, and the highlighted row keeps its text color: only its icon tile takes the accent, since on Red the accent is nearly the color of a destructive action. `MokaTheme.Dark` and `Light` keep the tinted row with the accent rail and accent text.

## Empty State

On the new design, when the query matches nothing, the menu says what was typed and what the keys do now, in place of the list. `Dark` and `Light` show "No matches" in the list, as they always did.

```razor
<MokaSlashMenu Items="_items" Query="@_query" @bind-Open="_menuOpen"
               EmptyText="No block is called {query}"
               EmptyHint="Enter adds a new line. Esc closes the menu." />
```

While nothing matches, `HandleKeyAsync` returns `false` for Enter, so the editor does what it always does with it. A hidden status region says the first line too, since focus is in the textarea and a screen reader would otherwise hear nothing.

## HandleKeyAsync

```csharp
public Task<bool> HandleKeyAsync(KeyboardEventArgs e)
```

The host calls it from its `@onkeydown` handler, before doing anything else with the key. `true` means the menu used the key, so the host should skip its own handling.

| Key | What the open menu does | Returns |
|-----|-------------------------|---------|
| Down / Up | Moves the highlight, wrapping at the ends | `true` |
| Enter | Raises `OnSelect` for the highlighted item, then closes | `true`. With no matches it does nothing and returns `false` |
| Escape | Closes the menu | `true` |
| Any other key | Nothing | `false` |

While the menu is closed it returns `false` for every key.

## ListboxId and ActiveOptionId

```csharp
public string ListboxId { get; }
public string? ActiveOptionId { get; }
```

Focus stays in the textarea, so the textarea is what tells a screen reader which row is highlighted. Two read-only properties give it the ids to point at.

| Property | Description |
|----------|-------------|
| `ListboxId` | Id of the menu's `role="listbox"`. It stays the same for the life of the component. Point the textarea's `aria-controls` at it while the menu is open |
| `ActiveOptionId` | Id of the highlighted `role="option"`, or null while the menu is closed or has no matches. Point the textarea's `aria-activedescendant` at it |

Rows are numbered by their place in the list as it is shown, groups included, so `{ListboxId}-option-0` is always the first row on screen.

A key handed over through `HandleKeyAsync` moves the highlight at once. A new `Open` or `Query` from the host only takes effect when the menu renders, and that happens after the host has rendered. So copy the id in `OnAfterRender` and render again when it changed:

```razor
<textarea value="@_text"
          aria-controls="@(_menuOpen ? _menu?.ListboxId : null)"
          aria-activedescendant="@_activeOption"
          @oninput="OnInput"
          @onkeydown="OnKeyDownAsync"></textarea>

@code {
    string? _activeOption;

    protected override void OnAfterRender(bool firstRender)
    {
        // The menu works out its highlight when it renders, which is after this component
        // has, so pick up a change here and render once more.
        if (_menu?.ActiveOptionId != _activeOption)
        {
            _activeOption = _menu?.ActiveOptionId;
            StateHasChanged();
        }
    }
}
```

## Host Contract

1. **Keep focus in the host.** The rows are not tab stops, and pressing the mouse on a row does not move focus out of the textarea.
2. **Open the menu** when the user types "/", and bind `Open` two-way so the menu can close itself.
3. **Pass the query.** On every input, set `Query` to the text typed after the slash.
4. **Forward every keydown** to `HandleKeyAsync` first, and stop when it returns `true`.
5. **Cancel the browser's default action for those keys.** Blazor can't do this from a .NET handler, and `@onkeydown:preventDefault` applies to every key, so it would also stop the user typing the query. Without it, Enter still adds a newline to the textarea and the arrows move the caret. The example below cancels the keys in the browser with `preventKeys` from Moka.Red.Core's `moka-keys.js`, the helper the library's own components use, and its rules apply only while the menu is on the page. Any script that calls `preventDefault()` on those keys while the menu is open works too.
6. **Insert in `OnSelect`**, replacing the slash and the query with the item's `Value`.
7. **Close the menu** when the textarea loses focus, and when the slash is deleted.
8. **Name the highlighted row** for screen readers: `aria-controls` set to `ListboxId` while the menu is open, and `aria-activedescendant` set to `ActiveOptionId`. See [ListboxId and ActiveOptionId](#listboxid-and-activeoptionid).

## Example

Type "/" in the text box, type to filter, move with the arrow keys and press Enter, or click a row.

```blazor-preview
@inject IJSRuntime JS
@implements IAsyncDisposable

<div style="width:100%;max-width:480px;min-height:420px">
    <div @ref="_editor" style="position:relative">
        <textarea rows="5" placeholder="Type / to insert a block"
                  style="width:100%;box-sizing:border-box;padding:8px;font:inherit;color:var(--moka-color-on-surface);background:var(--moka-color-surface);border:1px solid var(--moka-color-outline);border-radius:var(--moka-radius-md)"
                  value="@_text"
                  @oninput="OnInput"
                  @onkeydown="OnKeyDownAsync"
                  @onblur="@(() => _menuOpen = false)"></textarea>

        <MokaSlashMenu @ref="_menu"
                       @bind-Open="_menuOpen"
                       Items="_items"
                       Query="@_query"
                       OnSelect="Insert"
                       Style="left:0;top:100%;margin-top:4px" />
    </div>
</div>

@code {
    ElementReference _editor;
    MokaSlashMenu? _menu;
    IJSObjectReference? _keys;
    bool _menuOpen;
    string _text = "";
    string _query = "";

    static readonly IReadOnlyList<MokaSlashMenuItem> _items =
    [
        new() { Title = "Heading", Description = "# Section title", Category = "block", Icon = MokaIcons.Action.Edit, Keywords = "h1 title", Value = "# " },
        new() { Title = "Bullet list", Description = "- Item", Category = "block", Icon = MokaIcons.File.FileText, Keywords = "ul bullets", Value = "- " },
        new() { Title = "Checklist", Description = "- [ ] Task", Category = "block", Icon = MokaIcons.Status.Check, Keywords = "todo task", Value = "- [ ] " },
        new() { Title = "Quote", Description = "> Quoted text", Category = "block", Icon = MokaIcons.File.Document, Keywords = "blockquote", Value = "> " },
        new() { Title = "Link", Description = "[text](url)", Category = "inline", Icon = MokaIcons.Content.Link, Keywords = "url href", Value = "[text](https://)" }
    ];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // Cancel the textarea's own action for the keys the menu uses, only while the menu is
        // on the page. Enter only while a row is highlighted, so it still adds a newline when
        // nothing matches.
        _keys = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Moka.Red.Core/moka-keys.js");
        await _keys.InvokeVoidAsync("preventKeys", _editor, new object[]
        {
            new { selector = "textarea", keys = new[] { "ArrowUp", "ArrowDown", "Escape" }, when = ".moka-slash-menu" },
            new { selector = "textarea", keys = new[] { "Enter" }, when = ".moka-slash-menu [aria-selected=true]" }
        });
    }

    async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        // The menu sees every key first. True means it used the key.
        if (_menu is not null && await _menu.HandleKeyAsync(e))
        {
            return;
        }

        if (e.Key == "/" && !_menuOpen)
        {
            _query = "";
            _menuOpen = true;
        }
    }

    void OnInput(ChangeEventArgs e)
    {
        _text = e.Value?.ToString() ?? "";

        if (!_menuOpen)
        {
            return;
        }

        // .NET can't read the caret, so this host takes the query from the last slash and
        // expects typing at the end of the text.
        int slash = _text.LastIndexOf('/');
        if (slash < 0)
        {
            _menuOpen = false; // the slash was deleted
            return;
        }

        _query = _text[(slash + 1)..];
    }

    void Insert(MokaSlashMenuItem item)
    {
        int slash = _text.LastIndexOf('/');
        string before = slash >= 0 ? _text[..slash] : _text;
        _text = before + (item.Value as string ?? item.Title);
    }

    public async ValueTask DisposeAsync()
    {
        if (_keys is not null)
        {
            try
            {
                await _keys.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone, and the module with it.
            }
        }
    }
}
```

`preventKeys` takes a root element and a list of rules. A rule cancels its `keys` when they are pressed on an element inside the root that matches `selector`, and only while `when` matches something inside the root. The menu's root element has the class `moka-slash-menu` and exists only while the menu is open, and the highlighted row carries `aria-selected="true"`. The rules follow the menu's state without any .NET code.

## Placement

Pass the textarea as `Anchor` and the menu opens below it in the browser's top layer, like every other floating panel in the library. Nothing around it can clip it, so a textarea inside a `MokaCard`, a panel or a scrolling box works, and the menu flips above when there's no room below. `Placement` picks another side.

```razor
<textarea @ref="_textarea" ...></textarea>
<MokaSlashMenu @bind-Open="_menuOpen" Items="_items" Query="@_query" OnSelect="Insert" Anchor="_textarea"/>

@code {
    private ElementReference _textarea;
}
```

Without `Anchor` the menu is `position: absolute` with no offsets of its own, as before. Put it and the textarea in a `position: relative` wrapper, and place it with `Style` or `Class`, for example `left: 0; top: 100%; margin-top: 4px` to open it below the textarea. Then a container that hides its overflow cuts it off, which is why `Anchor` is the better choice. On the new design it is 360px wide (less on a window narrower than that) and its list scrolls past 24rem, which holds the default eight rows with descriptions. On `MokaTheme.Dark` and `Light` it is 280 to 360px wide and its list scrolls past 320px.

## Behaviour

These hold on both designs.

- The menu renders nothing while closed.
- The highlight starts on the first row when the menu opens and whenever `Query` changes.
- The highlighted row scrolls into view as the arrow keys move it, with its group's heading when it is the first row of a group. Only the list scrolls, never the page.
- Hovering a row lights it in the browser without a call to .NET, through Core's `moka-hover.js`. The menu hears which row the mouse is on just before the next key, so the arrows carry on from there and Enter inserts the row that is lit. One row is lit at a time: the mouse's while the mouse moved last, else the keyboard's.
- Pressing the mouse anywhere in the menu leaves focus in the textarea.
- With no matches, Enter goes back to the host.
- Up to 0.2.2 the highlighted row was not scrolled into view, so on the new design the arrow keys could light rows 7 and 8 out of sight, and every row the mouse crossed was a round trip to .NET.
- Up to 0.1.12, a one-way `Open` reopened the menu on the parent's next render after Escape closed it. Bind `Open` with `@bind-Open`.

## Accessibility

On the new design the key hints are caps with a word beside each, since focus is in the textarea and a tooltip that needs a hover would never show to someone typing. On `Dark` and `Light` they are caps in the header, two of them arrows, so each says what its key does ("Navigate", "Select", "Close") in a `MokaTooltip` below the header rather than in a `title`.

- The list is a `role="listbox"` with the id `ListboxId`, named by `Header` ("Insert" when the header is hidden). Each row is a `role="option"` with an id, and the highlighted one has `aria-selected="true"`.
- On the new design each group is a `role="group"` named by its heading. Items without a category sit straight in the listbox.
- Focus stays in the textarea. With its `aria-activedescendant` set to `ActiveOptionId`, screen readers announce the highlighted row as it moves.
- On the new design, when nothing matches, a hidden `role="status"` region says the empty state's first line.

Up to 0.1.12 the rows had no id, so the host couldn't point `aria-activedescendant` at the highlighted row, and screen readers said nothing as the highlight moved.

## Looks

The menu follows the theme's popup look, like every other floating panel, and so do its rows, group headings, shortcut caps and footer. The lit row's icon tile keeps the accent under every look. [Popover](/components/popover#looks) lists the seven looks.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithPopups(MokaPopupLook.Terminal)">
    @Body
</MokaThemeProvider>
```

The looks are drawn only on the new design. On `MokaTheme.Dark` and `Light` the menu keeps its original markup and look.
