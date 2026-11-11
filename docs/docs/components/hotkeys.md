---
title: Hotkeys
description: The app-wide keyboard shortcut registry, where a shortcut is bound and listed by the same registration.
---

# Hotkeys

`IMokaHotkeyService` is the one place an app's keyboard shortcuts live. Registering a shortcut both binds it
and lists it, so a shortcut cannot work without being documented or be documented without working.

Before this, three features talked about shortcuts and none of them owned a binding: the command palette bound
exactly one key, `MokaCheatsheet` rendered whatever static list it was handed, and a context menu item's
`Shortcut` was a label. The registry is what ties them together.

## Setup

```csharp
builder.Services.AddMokaHotkeys();   // AddMokaRed() already does this
```

Then put one host in your layout, beside the other hosts. The host is what listens, so without it shortcuts
are registered and listed but nothing fires them:

```razor
@using Moka.Red.Core.Hotkeys

<MokaHotkeyHost />
```

It renders no markup.

## MokaHotkey

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | a new value | Identifies the hotkey, for replacing and unregistering. A hotkey registered once and disposed never needs one |
| `Keys` | `string?` | - | Modifiers then one key, joined by `+`: `"Ctrl+K"`, `"Mod+Shift+P"`. `Mod` is Ctrl, or Cmd on a Mac. See [Keys](#keys) |
| `Description` | `string?` | - | What it does, in the words a shortcut list shows. Required for it to be listed |
| `Group` | `string?` | - | Heading it is listed under. Null groups it under "General" |
| `AllowInInput` | `bool` | `false` | Whether it still fires while focus is in a text field. Only matters for a shortcut that types a character |
| `AllowInModal` | `bool` | `false` | Whether it still fires while a modal (a dialog, a bottom sheet, an overlay drawer) is open. Turn it on for a hotkey that belongs to the modal itself |
| `PreventDefault` | `bool` | `true` | Whether a matching press has its default action cancelled |
| `OnInvoke` | `Func<Task>?` | - | Runs when it fires. Takes precedence over `OnInvokeSync` |
| `OnInvokeSync` | `Action?` | - | Runs when it fires and `OnInvoke` is not set |

## IMokaHotkeyService

| Member | Description |
|--------|-------------|
| `Register(hotkey)` | Registers it, replacing any with the same `Id`. Returns an `IDisposable` that unregisters it |
| `Unregister(id)` | Unregisters by id |
| `Hotkeys` | Every registered hotkey, in registration order |
| `InvokeAsync(id)` | Runs one as a press would, so a menu item and its shortcut share an action |
| `OnChanged` | Raised when the set changes |

## Registering from a component

The registration is disposable, so a component can bind a shortcut and let it go with itself:

```razor
@implements IDisposable
@inject IMokaHotkeyService Hotkeys

@code {
    private IDisposable? _save;

    protected override void OnInitialized() =>
        _save = Hotkeys.Register(new MokaHotkey
        {
            Keys = "Mod+S",
            Description = "Save the file",
            Group = "File",
            OnInvoke = SaveAsync
        });

    public void Dispose() => _save?.Dispose();
}
```

Registering with an `Id` that is already taken replaces it in place, so a component that re-registers on every
parameter set does not grow the list.

## Keys

Modifiers then one key: `Ctrl`, `Control`, `Meta`, `Cmd`, `Command`, `Alt`, `Option`, `Shift`, and `Mod`, which
is Ctrl everywhere except a Mac, where it is Cmd. `Space` is the space bar.

Every modifier has to match exactly, so `Ctrl+Shift+K` is not `Ctrl+K`. A modifier that is not in that list
turns the hotkey off and logs a warning, rather than binding the bare key.

A shortcut that types a character (no Ctrl, Cmd or Alt, such as `/` or `Shift+K`) is skipped while focus is in a
text field, so the character still reaches the field. Set `AllowInInput` to fire it anyway.

While a modal is open, the registry fires nothing unless a hotkey sets `AllowInModal`. A modal is drawn inside the
page's markup, so its keys reach the page, and a page shortcut such as `D` for duplicate would otherwise act on the
page behind it.

## Page shortcuts with @onmokakeydown

An editor or a canvas that handles many keys in one method can listen with `@onmokakeydown` in place of
`@onkeydown`. It is a keydown whose arguments (`MokaKeyEventArgs`, from `Moka.Red.Core.Keys`) say who owns the
press, which only the browser can tell:

| `Owner` | When |
|---------|------|
| `None` | Nothing else uses the key, so it is the page's |
| `Field` | It was pressed in a text field, a select, a text area or editable content |
| `Control` | The focused control uses the key itself: Space and Enter on a button, a link or a check box; the arrows, Home, End and Page Up/Down in a slider, a list, a menu, tabs, a tree or a grid |
| `Modal` | It was pressed inside an open modal |

`Owned` is true for anything but `None`, and `InModal` says whether the press was inside a modal, whatever else
owns it.

```razor
@using Moka.Red.Core.Keys

<div class="editor" tabindex="0" @onmokakeydown="OnKey">
    ...
    <MokaDialog @bind-Open="_help" Title="Keyboard shortcuts">...</MokaDialog>
</div>

@code {
    private void OnKey(MokaKeyEventArgs e)
    {
        if (e.Owned) return; // typing, a button's Space, anything inside the help dialog

        switch (e.Key)
        {
            case "d": Duplicate(); break;
            case "Delete": DeleteSelection(); break;
        }
    }
}
```

`MokaDialog` stops Blazor's `@onkeydown` at its edge, but Blazor checks a custom event such as `@onmokakeydown`, or
an app's own, separately, and a script listening on the page sees every key either way. So check `Owned`. A
handler inside the modal itself checks `Owner is MokaKeyOwner.Field or MokaKeyOwner.Control` instead.

The package registers the event in its JavaScript initializer (`Moka.Red.Core.lib.module.js`), so there is nothing
to set up. A script of the app's own can use the same rules:

```javascript
import { keyOwner, inModal, modalIsOpen } from './_content/Moka.Red.Core/moka-keys.js';

element.addEventListener('keydown', e => {
    if (keyOwner(e) !== 0) return; // 1 field, 2 control, 3 modal
    // ...
});
```

`preventKeys(root, rules)` in the same module takes `unlessOwned: true` on a rule, so a page can cancel Space's
scroll without stopping Space from pressing a button in a dialog.

## The cheatsheet writes itself

`MokaCheatsheet` with no `Groups` lists the registry, one group per `Group` in the order the groups first
appear, with the keys split into caps:

```razor
<MokaCheatsheet @bind-Open="_shortcutsOpen" />
```

Hotkeys with no `Description` are left out, since a row with no words says nothing. Setting `Groups` still wins,
so an app with its own list, or no registry at all, is unaffected.

`MokaCheatsheet.FromRegistry(hotkeys)` does the same conversion on its own, for an app that wants the groups
without opening a cheatsheet.

## Listing without binding

A hotkey with a `Description` and no `Keys` is listed but never bound, which is how you document something the
browser or an element already handles:

```csharp
Hotkeys.Register(new MokaHotkey { Description = "Drag a tab to reorder it", Group = "Tabs" });
```
