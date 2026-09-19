---
title: Context Menu
description: Right-click menus with icons, shortcut hints, checked and disabled items, submenus, and one shared menu opened from code.
order: 90
---

# Context Menu

The `Moka.Red.ContextMenu` package draws a menu at a point on the screen, usually where the user right-clicked. There are two ways to open one:

- **`MokaContextMenuTrigger`** wraps content and opens a menu on right-click, left-click or both.
- **`IMokaContextMenuService`** opens one shared menu from any handler, such as a table row's `OnRowContextMenu`. `MokaContextMenuHost` renders it.

Both take the same `MokaContextMenuItem` list.

## Setup

`AddMokaRed()` registers the service. With the individual package, call `AddMokaContextMenu()`:

```csharp
// Program.cs
using Moka.Red.ContextMenu.Extensions;

builder.Services.AddMokaContextMenu(); // registers IMokaContextMenuService
```

Place one `MokaContextMenuHost` in the layout, next to the other hosts:

```razor
@* MainLayout.razor *@
<MokaToastHost />
<MokaDialogHost />
<MokaContextMenuHost />
```

`MokaContextMenuTrigger` needs neither the service nor the host. Without a host on the page it draws its own menu. With a host, it opens the shared menu instead, so only one menu is open at a time.

## MokaContextMenuItem

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` | **required** | Item label |
| `Icon` | `MokaIconDefinition?` | - | Icon before the text |
| `Shortcut` | `string?` | - | Hint on the right, such as `"Ctrl+C"`. A label only: the menu does not bind the key |
| `Checked` | `bool` | `false` | Shows a check mark in place of the icon |
| `Disabled` | `bool` | `false` | Dimmed, can't be chosen, skipped by the arrow keys |
| `DividerBefore` | `bool` | `false` | Draws a divider above the item |
| `Children` | `IReadOnlyList<MokaContextMenuItem>?` | - | Submenu items. An item with children opens its submenu instead of running an action |
| `CssClass` | `string?` | - | Extra CSS class on the item row |
| `OnClick` | `Func<Task>?` | - | Runs when the item is chosen |
| `OnClickSync` | `Action?` | - | Runs when the item is chosen and `OnClick` is not set |

`MokaContextMenuItems.Divider()` returns a divider item, and an item with an empty `Text` and no children renders as one too. The menu drops dividers at the start and end and merges repeated ones, so a list built from conditions needs no cleanup.

Items are immutable, so state such as a `Checked` flag comes from the list you pass. Build the list from current state, as the examples below do.

## Right-Click Menu

`MokaContextMenuTrigger` wraps its content in an element with `display: contents`, so it adds no box to the layout.

```blazor-preview
@code {
    bool _wrap = true;
    string _last = "nothing yet";

    IReadOnlyList<MokaContextMenuItem> Items =>
    [
        new() { Text = "Cut", Icon = MokaIcons.Action.Edit, Shortcut = "Ctrl+X", OnClickSync = () => Pick("Cut") },
        new() { Text = "Copy", Icon = MokaIcons.Content.Copy, Shortcut = "Ctrl+C", OnClickSync = () => Pick("Copy") },
        new() { Text = "Paste", Icon = MokaIcons.Content.Paste, Shortcut = "Ctrl+V", Disabled = true },
        new() { Text = "Word wrap", Checked = _wrap, DividerBefore = true, OnClickSync = ToggleWrap },
        new()
        {
            Text = "Share",
            Icon = MokaIcons.Content.Link,
            Children =
            [
                new MokaContextMenuItem { Text = "Email", OnClickSync = () => Pick("Share by email") },
                new MokaContextMenuItem { Text = "Copy link", OnClickSync = () => Pick("Copy link") }
            ]
        },
        new() { Text = "Delete", Icon = MokaIcons.Action.Delete, DividerBefore = true, OnClickSync = () => Pick("Delete") }
    ];

    void ToggleWrap()
    {
        _wrap = !_wrap;
        Pick(_wrap ? "Word wrap on" : "Word wrap off");
    }

    // Item actions run in the menu's click handler, so this component re-renders itself.
    void Pick(string action)
    {
        _last = action;
        StateHasChanged();
    }
}

<MokaContextMenuTrigger Items="Items">
    <div style="display:flex;flex-direction:column;align-items:center;justify-content:center;gap:4px;
                width:100%;min-height:240px;border:1px dashed var(--moka-color-outline-variant);
                border-radius:var(--moka-radius-md);cursor:context-menu">
        <span>Right-click anywhere in this box</span>
        <MokaCaption>Last action: @_last</MokaCaption>
    </div>
</MokaContextMenuTrigger>
```

## Left-Click Trigger

Set `Trigger` to `LeftClick` for a menu button, or to `Both`. A left-click trigger leaves the browser's own right-click menu alone.

Enter or Space on a button inside the trigger also opens the menu. A click from the keyboard has no pointer position, so the menu opens under the trigger's content, lined up with its left edge, or above the content when there is no room below. The trigger has no box of its own (`display: contents`), so it measures the first element inside it. A menu opened this way starts with its first enabled item highlighted, so Enter chooses it straight away.

```blazor-preview
@code {
    IReadOnlyList<MokaContextMenuItem> _items =
    [
        new() { Text = "New file", Icon = MokaIcons.File.Document },
        new() { Text = "New folder", Icon = MokaIcons.File.Folder },
        new() { Text = "Upload", Icon = MokaIcons.Action.Upload, DividerBefore = true }
    ];
}

<div style="display:flex;align-items:flex-start;justify-content:center;width:100%;min-height:140px">
    <MokaContextMenuTrigger Items="_items" Trigger="MokaContextMenuTriggerType.LeftClick">
        <MokaButton StartIcon="MokaIcons.Action.Add">New</MokaButton>
    </MokaContextMenuTrigger>
</div>
```

### MokaContextMenuTrigger Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | **required** | The content that opens the menu |
| `Items` | `IReadOnlyList<MokaContextMenuItem>` | **required** | The menu items |
| `Trigger` | `MokaContextMenuTriggerType` | `RightClick` | `RightClick`, `LeftClick` or `Both` |
| `Disabled` | `bool` | `false` | Opens no menu and leaves the browser's menu alone |

## Opening a Menu From Code

Inject `IMokaContextMenuService` and call `Show` from a right-click handler. The shared menu opens at the cursor. Build the items inside the handler, so each action closes over the item that was right-clicked and each flag reflects current state.

```blazor-preview
@inject IMokaContextMenuService Menu

@code {
    string[] _files = ["Report.pdf", "Budget.xlsx", "Notes.txt"];
    HashSet<string> _starred = ["Budget.xlsx"];
    string _last = "Right-click a file.";

    IReadOnlyList<MokaContextMenuItem> MenuFor(string file) =>
    [
        new() { Text = "Open", Icon = MokaIcons.Content.ExternalLink, OnClickSync = () => Log($"Opened {file}") },
        new() { Text = "Rename", Icon = MokaIcons.Action.Edit, Shortcut = "F2", OnClickSync = () => Log($"Renamed {file}") },
        new() { Text = "Starred", Checked = _starred.Contains(file), OnClickSync = () => ToggleStar(file) },
        new() { Text = "Delete", Icon = MokaIcons.Action.Delete, DividerBefore = true, OnClickSync = () => Log($"Deleted {file}") }
    ];

    void ToggleStar(string file)
    {
        if (!_starred.Remove(file))
        {
            _starred.Add(file);
        }

        Log(_starred.Contains(file) ? $"Starred {file}" : $"Unstarred {file}");
    }

    // Item actions run in the menu's click handler, so this component re-renders itself.
    void Log(string message)
    {
        _last = message;
        StateHasChanged();
    }
}

<div style="display:flex;flex-direction:column;gap:8px;width:100%;max-width:360px;min-height:220px">
    <MokaList Bordered>
        @foreach (string file in _files)
        {
            <MokaListItem Text="@file" Icon="MokaIcons.File.Document"
                          SecondaryText="@(_starred.Contains(file) ? "Starred" : null)"
                          OnContextMenu="e => Menu.Show(e, MenuFor(file))" />
        }
    </MokaList>
    <MokaCaption>@_last</MokaCaption>
</div>

<MokaContextMenuHost />
```

An item's action runs inside the menu's click handler, not your component's. When the action changes something your component shows, call `StateHasChanged()`.

### IMokaContextMenuService

| Member | Description |
|--------|-------------|
| `Show(x, y, items)` | Opens the menu at viewport coordinates, replacing any menu already open |
| `Show(x, y, items, openedFromKeyboard, anchorTop)` | The same, saying how the menu was opened. With `openedFromKeyboard` it starts on its first enabled item. `anchorTop` (optional) is the top edge of the element the menu opens under: a menu that does not fit below `y` opens above that line |
| `Show(mouseEvent, items)` | Opens the menu at the event's `ClientX` and `ClientY`. An event the keyboard caused opens it as a keyboard open (see [Keyboard and Focus](#keyboard-and-focus)) |
| `Close()` | Closes the menu |
| `Visible` | Whether the menu is open |
| `X`, `Y`, `Items` | Position and items of the open menu |
| `OpenedFromKeyboard`, `AnchorTop` | How the open menu was opened, as passed to `Show` |
| `HasHost` | Whether a `MokaContextMenuHost` is mounted |
| `OnChanged` | Raised by `Show` and `Close`. The host re-renders on it |

Use the `openedFromKeyboard` overload when your own key handler opens a menu, for example a toolbar that opens one on Shift+F10, since a `KeyboardEventArgs` has no position to pass. An implementation of `IMokaContextMenuService` written before this overload still compiles: the overload falls back to `Show(x, y, items)`.

`MokaContextMenuHost` takes no parameters. Place exactly one: every mounted host renders the same menu.

## Components With Right-Click Hooks

These components raise an event on right-click instead of drawing a menu, so you choose the items. While a handler is attached, each suppresses the browser's own menu.

| Component | Parameter | Argument |
|-----------|-----------|----------|
| `MokaTable<TItem>` | `OnRowContextMenu` | `MokaItemContextMenuArgs<TItem>` |
| `MokaKanbanBoard<TItem>` | `OnCardContextMenu` | `MokaItemContextMenuArgs<TItem>` |
| `MokaSortable<TItem>` | `OnItemContextMenu` | `MokaItemContextMenuArgs<TItem>` |
| `MokaTabStrip<TValue>`, `MokaTabContainer<TValue>` | `OnTabContextMenu` | `MokaItemContextMenuArgs<TabInfo<TValue>>` |
| `MokaListItem` | `OnContextMenu` | `MouseEventArgs` |
| `MokaTreeItem` | `OnContextMenu` | `MouseEventArgs` |
| `MokaMenuItem` | `OnContextMenu` | `MouseEventArgs` |

`MokaItemContextMenuArgs<T>` lives in `Moka.Red.Core.Interactions` and carries the `Item` and the `MouseEvent`. The `MouseEventArgs` hooks sit on a single item, so the handler already knows which one it is. While `OnTabContextMenu` is set, the tab strip's built-in menu stays closed (see [Tabs](tabs)).

```razor
@inject IMokaContextMenuService ContextMenu

<MokaTable TItem="Order" Items="_orders" OnRowContextMenu="ShowRowMenu">
    <MokaColumn Title="Number" Field="o => o.Number" />
    <MokaColumn Title="Customer" Field="o => o.Customer" />
</MokaTable>

@code {
    void ShowRowMenu(MokaItemContextMenuArgs<Order> args) =>
        ContextMenu.Show(args.MouseEvent,
        [
            new MokaContextMenuItem { Text = $"Open order {args.Item.Number}", OnClick = () => OpenAsync(args.Item) },
            new MokaContextMenuItem { Text = "Cancel order", DividerBefore = true, Disabled = args.Item.Shipped, OnClick = () => CancelAsync(args.Item) }
        ]);
}
```

## Actions That Open a Dialog

The menu closes before the chosen item's action runs. An action can await a confirmation or a slow call, and the menu is already gone while it waits:

```csharp
new MokaContextMenuItem
{
    Text = "Delete",
    Icon = MokaIcons.Action.Delete,
    OnClick = async () =>
    {
        if (await Dialog.ConfirmAsync($"Delete {file.Name}?", title: "Delete file"))
            await Files.DeleteAsync(file);
    }
}
```

Up to 0.1.11 the menu waited for the action to finish, so it stayed open over the dialog.

## Keyboard and Focus

The menu takes focus when it opens and remembers which element had it.

| Key | Action |
|-----|--------|
| Down / Up | Move the highlight to the next or previous enabled item, wrapping at the ends |
| Home / End | Highlight the first or last enabled item |
| Enter / Space | Choose the highlighted item, or open its submenu |
| Right | Open the highlighted item's submenu |
| Left | In a submenu, close it and go back to its item. In the top-level menu, close an open submenu |
| Escape | In a submenu, close it and go back to its item. In the top-level menu, close the menu |
| Tab / Shift+Tab | Close the whole menu, submenus included, and put focus back on the element that had it before the menu opened |

A menu opened from the keyboard starts with its first enabled item highlighted, so Enter chooses it straight away. A menu opened with the mouse starts with nothing highlighted. `Show(mouseEvent, items)` and `MokaContextMenuTrigger` tell the two apart from the event:

- Enter or Space on a button fires a click with no click count (`Detail` 0), where a mouse click counts at least 1.
- The context-menu key and Shift+F10 fire `contextmenu` with no button (`Button` 0), where a right click reports 2. `Detail` does not help here: Chromium reports 0 for a right click too.

A held key does not choose an item, only a fresh press does. Enter that opens a menu from a button can still be down when the menu takes focus, and its repeats would otherwise choose the first item.

Opening a submenu from the keyboard moves focus into it and highlights its first enabled item. The same keys work there, and a submenu inside it opens the same way. The item that opened it stays highlighted in the parent menu, and Left or Escape puts focus back on it.

Hovering an item with children opens its submenu after a short delay, and the submenu stays open while the pointer crosses over to it. A submenu opened by hover leaves focus where it was, so the keys keep working in the parent menu until Right, Enter or Space moves focus into the submenu. When the pointer closes a submenu that has focus, the parent menu takes focus back.

A click outside the menu closes it. On close, focus goes back to the element that had it, but only while focus is still in the menu or has fallen back to the page body. When the chosen action has moved focus somewhere else, such as into a dialog it opened, focus stays there. Tab closes the menu without letting the browser move focus first, so focus lands back on that element rather than on whatever follows the menu in the page. A menu without an `OnClose` handler cannot close itself, so there Tab moves focus on as usual.

A right click or the context-menu key on the open menu does nothing: the browser's own menu does not open over it, and the event does not reach a trigger around it.

## Position

The menu uses `position: fixed`, so `X` and `Y` are viewport coordinates. Once rendered, it measures itself and moves inside the viewport, keeping an 8px gap from the edges. It stays hidden until then, so it never flashes at the wrong spot.

A menu that does not fit below `Y` opens above it instead of being pushed up over what it opened from: its bottom edge goes to the pointer, or to `AnchorTop` for a menu that hangs under an element, such as a trigger's button opened from the keyboard. Only when it fits neither below nor above is it moved up as far as it has to go.

A submenu opens against the right edge of its parent menu and flips to the left side when it would overflow. It does not flip up: it sits beside its item, so moving it up covers nothing.

## Accessibility

- The menu has `role="menu"` and each item has `role="menuitem"`. Disabled items carry `aria-disabled="true"`. Items with a submenu carry `aria-haspopup="true"` and `aria-expanded`, which is `"true"` while the submenu is open.
- Focus stays on a menu element while the arrows move the highlight, and `aria-activedescendant` points at the highlighted item, so screen readers announce it. Each open menu keeps its own highlight.
- A submenu takes its name from the item that opened it, through `aria-labelledby`.

## MokaContextMenu

The host and the trigger both render `MokaContextMenu`. Render it yourself when you control when it shows:

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaContextMenuItem>` | **required** | The menu items |
| `Visible` | `bool` | `false` | Shows the menu |
| `X` | `double` | `0` | Left edge in viewport pixels |
| `Y` | `double` | `0` | Top edge in viewport pixels |
| `OpenedFromKeyboard` | `bool` | `false` | The keyboard opened the menu, so it starts with its first enabled item highlighted. Read when the menu opens |
| `AnchorTop` | `double?` | `null` | Top edge of the element the menu opens under. A menu that does not fit below `Y` opens above this line. Null means a pointer: it opens above `Y` |
| `OnClose` | `EventCallback` | - | Fired on Escape in the top-level menu, Tab, a click outside, or when an item is chosen. Set `Visible` to `false` here |

`IsSubmenu` and `ParentLeft` are set by a menu on its own submenus. Leave them unset.
