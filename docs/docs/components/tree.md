---
title: Tree
description: Expandable tree view with nested items, icons, selection, right-click hooks and keyboard navigation.
order: 89
---

# Tree

`MokaTree` shows hierarchical data as nested `MokaTreeItem` elements. An item with child content gets an expand toggle, and its children render only while it is expanded. Set `Selectable` to let clicks select items. To pick a value in a form, use [Tree Select](tree-select).

## MokaTree Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | The top-level `MokaTreeItem` elements |
| `Selectable` | `bool` | `false` | A click, Enter or Space on a row toggles that item's `Selected` |

## MokaTreeItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Text` | `string?` | - | Row label |
| `Icon` | `MokaIconDefinition?` | - | Icon before the text |
| `ChildContent` | `RenderFragment?` | - | Nested `MokaTreeItem` elements. Any child content adds the expand toggle |
| `Expanded` | `bool` | `false` | Shows the children. Two-way bindable |
| `ExpandedChanged` | `EventCallback<bool>` | - | Fired when the item expands or collapses |
| `Selected` | `bool` | `false` | Highlights the row. Clicks change it only in a `Selectable` tree. Two-way bindable |
| `SelectedChanged` | `EventCallback<bool>` | - | Fired when a click, Enter or Space toggles the selection |
| `Disabled` | `bool` | `false` | Dims the item and every item under it. None of them can be selected, expanded or collapsed, and `OnContextMenu` doesn't fire |
| `OnContextMenu` | `EventCallback<MouseEventArgs>` | - | Right-click on the row. While set, the browser's own menu is suppressed |

An item flips its own `Expanded` and `Selected` when the user acts on it, then raises the matching `Changed` callback. A value set without a binding is only the starting state: when the parent re-renders, it can pass that value back in and undo the user's change. Use `@bind-Expanded` and `@bind-Selected` when the parent re-renders.

## Basic Tree

```blazor-preview
<MokaTree Style="width:280px">
    <MokaTreeItem Text="src" Icon="MokaIcons.File.Folder" Expanded>
        <MokaTreeItem Text="Components" Icon="MokaIcons.File.Folder">
            <MokaTreeItem Text="App.razor" Icon="MokaIcons.File.Code" />
            <MokaTreeItem Text="Routes.razor" Icon="MokaIcons.File.Code" />
        </MokaTreeItem>
        <MokaTreeItem Text="Program.cs" Icon="MokaIcons.File.Code" />
        <MokaTreeItem Text="appsettings.json" Icon="MokaIcons.File.FileText" />
    </MokaTreeItem>
    <MokaTreeItem Text="README.md" Icon="MokaIcons.File.Document" />
</MokaTree>
```

## Expanding From Code

Bind `Expanded` to open and close items from outside the tree.

```blazor-preview
@code {
    bool _src = true;
    bool _tests;

    void SetAll(bool open) => (_src, _tests) = (open, open);
}

<div style="display:flex;flex-direction:column;gap:8px;width:280px">
    <div style="display:flex;gap:8px">
        <MokaButton Size="MokaSize.Xs" OnClick="() => SetAll(true)">Expand all</MokaButton>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Outlined" OnClick="() => SetAll(false)">Collapse all</MokaButton>
    </div>
    <MokaTree>
        <MokaTreeItem Text="src" Icon="MokaIcons.File.Folder" @bind-Expanded="_src">
            <MokaTreeItem Text="Program.cs" Icon="MokaIcons.File.Code" />
            <MokaTreeItem Text="App.razor" Icon="MokaIcons.File.Code" />
        </MokaTreeItem>
        <MokaTreeItem Text="tests" Icon="MokaIcons.File.Folder" @bind-Expanded="_tests">
            <MokaTreeItem Text="TreeTests.cs" Icon="MokaIcons.File.Code" />
        </MokaTreeItem>
    </MokaTree>
</div>
```

## Loading Children on Expand

Collapsed children are not rendered, and child content that renders nothing yet still gives the item its toggle. An item can load its children in `ExpandedChanged`:

```blazor-preview
@code {
    bool _open;
    List<string>? _logs;

    async Task OnExpandedChanged(bool open)
    {
        _open = open;
        if (open && _logs is null)
        {
            await Task.Delay(600); // stands in for a server call
            _logs = ["app-2026-09-16.log", "app-2026-09-17.log", "app-2026-09-18.log"];
        }
    }
}

<MokaTree Style="width:280px">
    <MokaTreeItem Text="logs" Icon="MokaIcons.File.Folder" Expanded="_open" ExpandedChanged="OnExpandedChanged">
        @if (_logs is null)
        {
            <MokaTreeItem Text="Loading..." Icon="MokaIcons.Status.Loading" Disabled />
        }
        else
        {
            foreach (string log in _logs)
            {
                <MokaTreeItem Text="@log" Icon="MokaIcons.File.FileText" />
            }
        }
    </MokaTreeItem>
</MokaTree>
```

## Selection

With `Selectable` set, a click on a row toggles that item's `Selected`. Each item keeps its own flag, so several can be selected at once, and the tree reports `aria-multiselectable`. For single selection, bind every item to one value:

```blazor-preview
@code {
    bool _srcOpen = true;
    string? _selected = "Program.cs";
    string[] _files = ["Program.cs", "App.razor", "appsettings.json"];

    void Select(string name, bool on) => _selected = on ? name : null;
}

<div style="display:flex;flex-direction:column;gap:8px;width:280px">
    <MokaTree Selectable>
        <MokaTreeItem Text="src" Icon="MokaIcons.File.Folder" @bind-Expanded="_srcOpen"
                      Selected="@(_selected == "src")" SelectedChanged="@(on => Select("src", on))">
            @foreach (string file in _files)
            {
                <MokaTreeItem Text="@file" Icon="MokaIcons.File.Code"
                              Selected="@(_selected == file)" SelectedChanged="@(on => Select(file, on))" />
            }
        </MokaTreeItem>
    </MokaTree>
    <MokaCaption>Selected: @(_selected ?? "nothing")</MokaCaption>
</div>
```

A clicked row toggles, so clicking the selected item clears the selection. `Selected` also highlights a row in a tree that isn't `Selectable`, such as the file open in an editor. Clicks don't change it there.

## Disabled Items

A disabled item is dimmed and ignores clicks and keys. It can't be selected, expanded or collapsed, and neither can anything under it, so an expanded disabled folder shows its contents as disabled too.

```blazor-preview
<MokaTree Style="width:280px" Selectable>
    <MokaTreeItem Text="Documents" Icon="MokaIcons.File.Folder" Expanded>
        <MokaTreeItem Text="Report.pdf" Icon="MokaIcons.File.Document" />
        <MokaTreeItem Text="Archive" Icon="MokaIcons.Toggle.Lock" Disabled>
            <MokaTreeItem Text="2025.zip" Icon="MokaIcons.File.Document" />
        </MokaTreeItem>
        <MokaTreeItem Text="Budget.xlsx" Icon="MokaIcons.File.Document" />
    </MokaTreeItem>
</MokaTree>
```

## Context Menu

`OnContextMenu` passes the mouse event. Your handler already knows which item it belongs to, so it can open a menu at the cursor through `IMokaContextMenuService` (see [Context Menu](context-menu)):

```razor
@inject IMokaContextMenuService ContextMenu

<MokaTree>
    @foreach (FileNode file in _files)
    {
        <MokaTreeItem Text="@file.Name" Icon="MokaIcons.File.Document"
                      OnContextMenu="e => ContextMenu.Show(e, MenuFor(file))" />
    }
</MokaTree>

@code {
    IReadOnlyList<MokaContextMenuItem> MenuFor(FileNode file) =>
    [
        new() { Text = "Rename", Icon = MokaIcons.Action.Edit, OnClick = () => RenameAsync(file) },
        new() { Text = "Delete", Icon = MokaIcons.Action.Delete, DividerBefore = true, OnClick = () => DeleteAsync(file) }
    ];
}
```

The layout needs a `MokaContextMenuHost` for the shared menu to appear. The context-menu key and Shift+F10 on a focused item raise `OnContextMenu` too, with the menu placed under the item's row.

## Keyboard

The tree follows the WAI-ARIA tree pattern. Only one item is in the tab order: the one that last had focus, or before that, the selected item or the first one. Tab moves into the tree and out again.

| Key | Action |
|-----|--------|
| Down / Up | Move to the next or previous visible item |
| Home / End | Move to the first or last visible item |
| Right | Expand a collapsed item. On an expanded item, move to its first child |
| Left | Collapse an expanded item. Otherwise move to its parent |
| Enter / Space | Act like a click on the row, so in a `Selectable` tree they toggle the selection |
| Context-menu key / Shift+F10 | Raise the item's `OnContextMenu` |

Visible items are the ones whose parents are all expanded. The arrows stop at the first and last item rather than wrapping. Keys pressed with Ctrl, Alt or Cmd are left to the page. A disabled item still takes focus, but the keys can't select, expand or collapse it. Clicking an item's expand toggle leaves focus on the item, so the arrow keys carry on from there.

Up to 0.1.11 the rows took no focus: Tab stopped on each expand toggle, and there was no way to select an item from the keyboard.

## Accessibility

- `MokaTree` has `role="tree"`, each item has `role="treeitem"`, and each set of children has `role="group"`.
- An item with children reports `aria-expanded` as `"true"` or `"false"`. In a `Selectable` tree every item reports `aria-selected` the same way, and the tree sets `aria-multiselectable="true"`.
- A disabled item, and every item under it, has `aria-disabled="true"`.
- The expand toggles are out of the tab order and hidden from screen readers. Right and Left do their job from the keyboard, and `aria-expanded` carries the state.
