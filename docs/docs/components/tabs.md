---
title: Tabs
description: Data-driven tab strip with drag reorder, pin, close, context menu, groups, and badges.
order: 11
---

# Tabs

`MokaTabStrip<TValue>` renders a horizontal tab strip driven by a list of `TabInfo<TValue>` records. Tabs support drag-and-drop reordering, pinning, close buttons, right-click context menus, group membership, and per-tab badges. The strip renders only the headers: the caller owns the tab list and the content. `MokaTabContainer<TValue>` wraps a strip with session state (`AddMokaTabs<TValue>()`), panels and lazy rendering.

## TabInfo Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | auto GUID | Unique tab identifier |
| `Title` | `string` | `""` | Tab header label |
| `Value` | `TValue?` | - | User-defined value |
| `IconContent` | `RenderFragment?` | - | Custom icon render fragment |
| `ContentComponentType` | `Type?` | - | Component rendered as tab content |
| `ContentParameters` | `IDictionary<string, object?>?` | - | Parameters for the content component |
| `Badge` | `TabBadgeInfo?` | - | Badge count/dot on the tab header |
| `GroupName` | `string?` | - | Assigns the tab to a group |
| `IsClosable` | `bool` | `true` | Can be closed from the strip: close button, middle click, Delete, context menu |
| `IsPinned` | `bool` | `false` | Pinned tabs show a dot and cannot be closed. `MokaTabContainer` keeps them first |
| `IsDraggable` | `bool` | `true` | Participates in drag reorder |
| `Tooltip` | `string?` | - | Native tooltip on the header |
| `ActiveColor` | `string?` | - | CSS color for the active tab's text and underline |
| `CssClass` | `string?` | - | Extra CSS class on the tab header |

## MokaTabStrip Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Tabs` | `IReadOnlyList<TabInfo<TValue>>` | **required** | The tab list |
| `Groups` | `IReadOnlyList<TabGroupInfo>` | `[]` | Group definitions |
| `ActiveTabId` | `string?` | - | Currently active tab id |
| `OnTabActivated` | `EventCallback<string>` | - | Fired when a tab is clicked, or gets Enter or Space |
| `OnTabClosed` | `EventCallback<string>` | - | Fired when a tab is closed: close button, middle click, Delete, or the built-in menu |
| `OnTabReordered` | `EventCallback<(string TabId, int NewIndex)>` | - | Fired after drag reorder |
| `OnTabPinToggled` | `EventCallback<string>` | - | Fired when pin is toggled |
| `OnTabGroupChanged` | `EventCallback<(string TabId, string? GroupName)>` | - | Fired when group changes |
| `OnGroupCollapseToggled` | `EventCallback<string>` | - | Fired when a group collapses |
| `OnTabContextMenu` | `EventCallback<MokaItemContextMenuArgs<TabInfo<TValue>>>` | - | Fired on right click or the context-menu key. While set, the built-in menu stays closed |
| `ShowCloseButton` | `bool` | `true` | Close buttons on closable tabs |
| `ShowPinButton` | `bool` | `true` | Pin buttons on the tab headers |
| `CloseOnMiddleClick` | `bool` | `true` | A middle click closes a closable, unpinned tab |
| `AllowDragReorder` | `bool` | `true` | Enables drag-and-drop reorder |
| `AllowContextMenu` | `bool` | `true` | Right click opens the built-in context menu |
| `CustomContextMenuItems` | `IReadOnlyList<ContextMenuItem>?` | - | Extra items for the built-in menu, after any plugin items |
| `PluginRegistry` | `MokaTabPluginRegistry?` | - | Plugins whose `GetContextMenuItems` add to the built-in menu |
| `Theme` | `TabTheme?` | - | Colour overrides, applied by the strip itself (see Styling) |
| `TabStripCssClass` | `string?` | - | CSS class on the strip container |

`MokaTabContainer<TValue>` passes `OnTabContextMenu` and `CloseOnMiddleClick` through to its strip.

## Keyboard

The strip follows the WAI-ARIA tabs pattern with manual activation. One tab is in the tab order at a time: the selected one.

| Key | Action |
|-----|--------|
| Left / Right | Move focus to the previous or next tab, wrapping at the ends |
| Home / End | Move focus to the first or last tab |
| Enter / Space | Activate the focused tab |
| Delete | Close the focused tab, if it can be closed |
| Context-menu key or Shift+F10 | Open the tab's context menu |

Inside the built-in menu, Up, Down, Home and End move between items and Escape or Tab closes it, returning focus to the tab. The pin and close buttons stay out of the tab order.

## Your Own Context Menu

Handle `OnTabContextMenu` to show an app menu instead of the built-in one, for example through `IMokaContextMenuService`:

```razor
@inject IMokaContextMenuService ContextMenu

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="@_active"
              OnTabActivated="id => _active = id"
              OnTabContextMenu="ShowTabMenu" />

@code {
    void ShowTabMenu(MokaItemContextMenuArgs<TabInfo<string>> args) =>
        ContextMenu.Show(args.MouseEvent,
        [
            new MokaContextMenuItem { Text = $"Rename {args.Item.Title}", OnClick = () => Rename(args.Item) },
            new MokaContextMenuItem { Text = "Close", DividerBefore = true, OnClickSync = () => Close(args.Item.Id) }
        ]);
}
```

## Styling

The tab components ship their styles in the scoped CSS bundle, so a strip needs no stylesheet link, works outside `MokaTabContainer`, and is styled from the first paint in every host, including MAUI Blazor Hybrid. Versions up to 0.1.11 used a separate `moka-tabs.css` injected through `<HeadContent>`. That file is gone: remove any `<link>` to `_content/Moka.Red.Navigation/moka-tabs.css`.

Every colour goes through a `--moka-tab-*` custom property that falls back to a theme token. Set them in CSS on any ancestor, or through `TabTheme`, which the strip applies itself:

```razor
<MokaTabStrip Tabs="_tabs"
              ActiveTabId="@_active"
              Theme="@(new TabTheme { ActiveTabColor = "#00e676", StripBackground = "#0c0c10" })" />
```

`TabInfo.ActiveColor` overrides the active colour for one tab.

## Basic Usage

```blazor-preview
@code {
    string _active = "tab-1";
    List<TabInfo<string>> _tabs = [
        new() { Id = "tab-1", Title = "Overview", Value = "overview" },
        new() { Id = "tab-2", Title = "Details", Value = "details" },
        new() { Id = "tab-3", Title = "History", Value = "history" },
    ];
}

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id"
              ShowCloseButton="false" />

<div style="padding:16px">
    Active: @(_tabs.FirstOrDefault(t => t.Id == _active)?.Title)
</div>
```

## Closable Tabs

```blazor-preview
@code {
    string? _active;
    List<TabInfo<string>> _tabs = [
        new() { Id = "1", Title = "Document.txt" },
        new() { Id = "2", Title = "README.md" },
        new() { Id = "3", Title = "Program.cs" },
    ];

    protected override void OnInitialized() => _active = _tabs[0].Id;

    void Close(string id)
    {
        _tabs.RemoveAll(t => t.Id == id);
        if (_active == id)
            _active = _tabs.LastOrDefault()?.Id;
    }
}

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id"
              OnTabClosed="Close" />
```

## Pinned Tabs

Pinned tabs cannot be closed and show a dot before their title. The strip keeps your order; `MokaTabContainer` sorts pinned tabs first.

```blazor-preview
@code {
    string _active = "home";
    List<TabInfo<string>> _tabs = [
        new() { Id = "home", Title = "Home", IsPinned = true, IsClosable = false },
        new() { Id = "settings", Title = "Settings", IsPinned = true, IsClosable = false },
        new() { Id = "docs", Title = "Documentation" },
        new() { Id = "api", Title = "API Reference" },
    ];
}

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id" />
```

## Drag Reorder

```blazor-preview
@code {
    string _active = "a";
    List<TabInfo<string>> _tabs = [
        new() { Id = "a", Title = "Alpha" },
        new() { Id = "b", Title = "Beta" },
        new() { Id = "c", Title = "Gamma" },
        new() { Id = "d", Title = "Delta" },
    ];

}

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id"
              AllowDragReorder />
```

## Tab Badges

```blazor-preview
@code {
    string _active = "inbox";
    List<TabInfo<string>> _tabs = [
        new() { Id = "inbox", Title = "Inbox",
                Badge = new TabBadgeInfo { Count = 12, CssClass = "error" } },
        new() { Id = "sent", Title = "Sent" },
        new() { Id = "drafts", Title = "Drafts",
                Badge = new TabBadgeInfo { ShowDot = true, CssClass = "warning" } },
    ];
}

<MokaTabStrip Tabs="_tabs"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id"
              ShowCloseButton="false" />
```

## Groups

```blazor-preview
@code {
    string _active = "a";
    List<TabGroupInfo> _groups = [
        new() { Name = "Frontend", Color = "#2196f3", Order = 0 },
        new() { Name = "Backend", Color = "#4caf50", Order = 1 },
    ];
    List<TabInfo<string>> _tabs = [
        new() { Id = "a", Title = "UI", GroupName = "Frontend" },
        new() { Id = "b", Title = "Styles", GroupName = "Frontend" },
        new() { Id = "c", Title = "API", GroupName = "Backend" },
        new() { Id = "d", Title = "DB", GroupName = "Backend" },
    ];
}

<MokaTabStrip Tabs="_tabs"
              Groups="_groups"
              ActiveTabId="_active"
              OnTabActivated="id => _active = id"
              ShowCloseButton="false" />
```
