---
title: Cheatsheet
description: Modal keyboard-shortcut overlay that lists shortcuts in titled groups, with key chips, focus trapping and Escape to close.
order: 92
---

# Cheatsheet

`MokaCheatsheet` is a modal overlay that lists your app's keyboard shortcuts. Shortcuts come in titled groups, laid out in two columns (one column below 640px wide). Each row shows what the shortcut does on the left and its keys as `MokaKbd` chips on the right. The panel is at most 80% of the viewport high, and a long list scrolls inside it while the page behind stays still. It renders nothing while closed.

The cheatsheet doesn't listen for a key itself. Open it from a help button, a command palette command or your own shortcut handling.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Open` | `bool` | `false` | Whether the cheatsheet is showing. Bind it two-way with `@bind-Open`. See [Open state](#open-state) |
| `OpenChanged` | `EventCallback<bool>` | - | Raised with `false` when the user closes the cheatsheet |
| `Title` | `string` | `"Keyboard Shortcuts"` | Heading in the header. Also names the dialog for screen readers |
| `Groups` | `IReadOnlyList<MokaCheatsheetGroup>?` | - | The shortcut groups, in order |
| `CloseOnBackdropClick` | `bool` | `true` | A click on the backdrop closes the cheatsheet |
| `CloseOnEscape` | `bool` | `true` | Escape closes the cheatsheet |
| `MaxWidth` | `string` | `"720px"` | Widest the panel gets. Any CSS width |
| `FooterContent` | `RenderFragment?` | - | A hint line under the shortcut grid |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | - | Space the panel keeps from the screen edges. It stays centered |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | - | Padding inside the panel, around the header and body |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | - | Corner radius of the panel |
| `Class` | `string?` | - | Additional CSS classes |
| `Style` | `string?` | - | Additional inline styles |

`Id` and any other attributes go on the element with `role="dialog"`, and so do `Class`, `Style` and the spacing parameters. Up to 0.1.12 the cheatsheet ignored `Margin`, `Padding` and `Rounded`.

## MokaCheatsheetGroup and MokaCheatsheetItem

Both are positional records in `Moka.Red.Feedback.Cheatsheet`:

```csharp
public sealed record MokaCheatsheetGroup(string Title, IReadOnlyList<MokaCheatsheetItem> Items);

public sealed record MokaCheatsheetItem(string Description, IReadOnlyList<string> Keys);
```

**MokaCheatsheetGroup**

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Group heading, shown as an uppercase label |
| `Items` | `IReadOnlyList<MokaCheatsheetItem>` | The rows in the group, in order |

**MokaCheatsheetItem**

| Property | Type | Description |
|----------|------|-------------|
| `Description` | `string` | What the shortcut does, on the left of the row |
| `Keys` | `IReadOnlyList<string>` | The keys, one chip each, in order. `["Ctrl", "Shift", "N"]` shows three chips |

The chips sit side by side with no `+` between them, so a combination (`["Ctrl", "K"]`) and a sequence (`["g", "h"]`) look the same. The example below spells its sequences out in the description.

## Example

```razor
<MokaButton Variant="MokaVariant.Outlined" OnClick="() => _open = true">Keyboard shortcuts</MokaButton>

<MokaCheatsheet @bind-Open="_open" Groups="_groups">
    <FooterContent>Press <MokaKbd>Esc</MokaKbd> to close</FooterContent>
</MokaCheatsheet>

@code {
    bool _open;

    static readonly IReadOnlyList<MokaCheatsheetGroup> _groups =
    [
        new("General",
        [
            new MokaCheatsheetItem("Command palette", ["Ctrl", "K"]),
            new MokaCheatsheetItem("Show this list", ["?"]),
            new MokaCheatsheetItem("Close overlay", ["Esc"])
        ]),
        new("Create",
        [
            new MokaCheatsheetItem("New item", ["Ctrl", "N"]),
            new MokaCheatsheetItem("New from template", ["Ctrl", "Shift", "N"])
        ]),
        new("Navigate",
        [
            new MokaCheatsheetItem("Go home (g, then h)", ["g", "h"]),
            new MokaCheatsheetItem("Go to settings (g, then s)", ["g", "s"])
        ])
    ];
}
```

## Opening from the Command Palette

A [command palette](command-palette) command can open the cheatsheet. The command runs inside the palette's event handler, so the component that owns the cheatsheet re-renders itself:

```razor
@* MainLayout.razor *@
@implements IDisposable
@inject IMokaCommandPaletteService Palette

<MokaCheatsheet @bind-Open="_shortcutsOpen" Groups="Shortcuts.Groups" />

@code {
    bool _shortcutsOpen;

    protected override void OnInitialized() =>
        Palette.Register(new MokaCommand
        {
            Id = "help.shortcuts",
            Title = "Keyboard shortcuts",
            Group = "Help",
            Shortcut = "?",
            OnExecuteSync = ShowShortcuts
        });

    void ShowShortcuts()
    {
        _shortcutsOpen = true;
        StateHasChanged();
    }

    public void Dispose() => Palette.Unregister("help.shortcuts");
}
```

The command's `Shortcut` is only the hint the palette shows. It doesn't bind the `?` key.

## Open State

Bind `Open` two-way. The cheatsheet closes itself (Escape, the close button or the backdrop) and reports it through `OpenChanged`, so with `@bind-Open` your field goes back to `false`. With a one-way `Open="@x"`, `x` stays `true` after the user closes the cheatsheet, and setting it to `true` again doesn't reopen it. A parent that re-renders doesn't reopen a cheatsheet the user closed either.

## Keyboard and Focus

| Key | Action |
|-----|--------|
| Escape | Close, unless `CloseOnEscape` is `false` |
| Tab / Shift+Tab | Move between the controls in the cheatsheet without leaving it |

- The cheatsheet is a modal dialog (`role="dialog"`, `aria-modal="true"`), named by `Title`.
- Opening moves focus into it: to an element in `FooterContent` marked `autofocus` or `data-autofocus` if there is one, otherwise the close button. Content that already took focus inside the cheatsheet keeps it.
- Tab and Shift+Tab stay inside while it is open.
- Closing it, by any of the ways above or by setting `Open` to `false`, returns focus to where it was before it opened. When it opened from inside a drawer or a dialog, focus goes back there, even if the cheatsheet's content had already taken focus before the trap started.
- Keys pressed inside it don't reach Blazor `@onkeydown` handlers on elements around it, so Escape closes only the cheatsheet and not a drawer or dialog it was opened from. A shortcut handler on an element around it, such as one that toggles the cheatsheet with `?`, doesn't hear keys pressed inside it either: close it with Escape. A listener added with JavaScript on the document still hears them.
- The page behind doesn't scroll while it is open. The lock is the one dialogs, drawers and bottom sheets take, counted, so closing the cheatsheet over an open dialog leaves the dialog's lock in place. Every way it closes gives the lock back, and so does removing it while it is open.

Up to 0.1.12 opening the cheatsheet left focus on the page behind it, so Escape did nothing until the user clicked inside it. Escape inside it also closed a drawer or dialog it was opened from, and the page behind it still scrolled.
