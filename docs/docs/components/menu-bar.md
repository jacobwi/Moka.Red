---
title: Menu Bar
description: The File/Edit/View row along the top of an app, with the keyboard an application menu bar is expected to have.
---

# Menu Bar

`MokaMenuBar` is the row of menus along the top of an app. It is one tab stop for the whole bar, with the arrow
keys between and inside the menus, the way a desktop menu bar works.

It is not `MokaMenu`, which is sidebar navigation, and not `MokaDropdown`, which is one button's menu. Reach for
this when you want File, Edit and View.

## Setup

```razor
@using Moka.Red.Navigation.MenuBar
```

No service registration, and no host component.

## MokaMenuBar Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | The menus, as `MokaMenuBarMenu` children |
| `AriaLabel` | `string` | `"Main menu"` | Name of the bar for screen readers. Set it on a second bar so the two are told apart |
| `OpenOnHover` | `bool` | `true` | With a menu open, moving the pointer onto another switches to it. Hovering opens nothing while the bar is closed |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

| Method | Description |
|--------|-------------|
| `CloseAll()` | Closes whatever is open, for a shell that closes its menus when something else takes over |

## MokaMenuBarMenu Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Label` | `string?` | **required** | The menu's name in the bar, such as "File" |
| `Icon` | `MokaIconDefinition?` | - | Icon before the label |
| `ChildContent` | `RenderFragment?` | - | The items, as `MokaMenuBarItem` and `MokaMenuBarDivider` children |

## MokaMenuBarItem Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Text` | `string?` | **required** | The item's label |
| `Icon` | `MokaIconDefinition?` | - | Icon before the label. Items without one still line up with those that have one |
| `Shortcut` | `string?` | - | Hint on the right, such as `"Ctrl+N"`, drawn as one key cap per key. A label only: see [Shortcuts](#shortcuts) |
| `OnClick` | `EventCallback` | - | Runs when the item is chosen, after the menu has closed |
| `Disabled` | `bool` | `false` | Dimmed, cannot be chosen, and skipped by the arrow keys |

## Basic Usage

```razor
<MokaMenuBar>
    <MokaMenuBarMenu Label="File">
        <MokaMenuBarItem Text="New" Icon="MokaIcons.Action.Add" Shortcut="Ctrl+N" OnClick="New" />
        <MokaMenuBarItem Text="Open" Shortcut="Ctrl+O" OnClick="Open" />
        <MokaMenuBarDivider />
        <MokaMenuBarItem Text="Exit" OnClick="Exit" />
    </MokaMenuBarMenu>
    <MokaMenuBarMenu Label="Edit">
        <MokaMenuBarItem Text="Undo" Shortcut="Ctrl+Z" OnClick="Undo" />
        <MokaMenuBarItem Text="Redo" Shortcut="Ctrl+Shift+Z" Disabled />
    </MokaMenuBarMenu>
</MokaMenuBar>
```

## Keyboard

The bar is one tab stop. Tab moves into it and back out; everything else happens with the arrow keys, which is
what separates a menu bar from a row of buttons.

| Key | Where | What it does |
|-----|-------|--------------|
| Left, Right | Anywhere in the bar | Moves between the menus, wrapping at the ends. With a menu open, the next one opens too |
| Down, Enter, Space | On a closed menu | Opens it on its first item |
| Up | On a closed menu | Opens it on its last item, for the item at the bottom |
| Up, Down | In an open menu | Moves between items, wrapping, stepping over disabled ones |
| Home, End | In the bar | Jumps to the first or last menu |
| Home, End | In an open menu | Jumps to the first or last item |
| Enter, Space | In an open menu | Chooses the item |
| A letter | In the bar | Jumps to the menu whose label starts with it |
| A letter | In an open menu | Jumps to the item whose text starts with it |
| Escape | In an open menu | Closes it and puts focus back on its label |
| Tab | In an open menu | Closes it and leaves the bar |

Choosing an item closes the menu and then runs `OnClick`, in that order, so an action that opens a dialog does
not fight the menu for focus.

## Shortcuts

An item's `Shortcut` is a hint. It is drawn as key caps, the same ones the context menu and the command palette
use, but the item does not bind the key: a menu that is closed cannot hear a key press.

Bind it with the [hotkey registry](/components/hotkeys) and give the registration the same string, so the two
cannot drift apart:

```razor
@inject IMokaHotkeyService Hotkeys

<MokaMenuBarItem Text="New" Shortcut="Ctrl+N" OnClick="New" />

@code {
    protected override void OnInitialized() =>
        _registration = Hotkeys.Register(new MokaHotkey
        {
            Keys = "Ctrl+N",
            Description = "New file",
            Group = "File",
            OnInvokeSync = New
        });
}
```

## Accessibility

The bar is a `role="menubar"`, each label a `role="menuitem"` with `aria-haspopup="menu"` and `aria-expanded`,
and each popup a `role="menu"` named by its label. One label at a time has `tabindex="0"` and it follows the
arrow keys, so coming back to the bar with Tab returns to where you were.
