---
title: Tabs
description: Data-driven tab strip with drag reorder, pin, close, context menu, groups, and badges.
order: 11
---

# Tabs

`MokaTabStrip<TValue>` renders a horizontal tab strip driven by a list of `TabInfo<TValue>` records. Tabs support drag-and-drop reordering, pinning, close buttons, right-click context menus, group membership, and per-tab badges. Content rendering is delegated to the caller — the component manages only the strip itself.

## TabInfo Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | auto GUID | Unique tab identifier |
| `Title` | `string` | `""` | Tab header label |
| `Value` | `TValue?` | — | User-defined value |
| `IconContent` | `RenderFragment?` | — | Custom icon render fragment |
| `ContentComponentType` | `Type?` | — | Component rendered as tab content |
| `ContentParameters` | `IDictionary<string, object?>?` | — | Parameters for the content component |
| `Badge` | `TabBadgeInfo?` | — | Badge count/dot on the tab header |
| `GroupName` | `string?` | — | Assigns the tab to a group |
| `IsClosable` | `bool` | `true` | Shows the close button |
| `IsPinned` | `bool` | `false` | Pinned tabs appear first and cannot be closed |
| `IsDraggable` | `bool` | `true` | Participates in drag reorder |
| `ActiveColor` | `string?` | — | CSS color overriding the active underline |
| `CssClass` | `string?` | — | Extra CSS class on the tab header |

## MokaTabStrip Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Tabs` | `IReadOnlyList<TabInfo<TValue>>` | **required** | The tab list |
| `Groups` | `IReadOnlyList<TabGroupInfo>` | `[]` | Group definitions |
| `ActiveTabId` | `string?` | — | Currently active tab id |
| `OnTabActivated` | `EventCallback<string>` | — | Fired when a tab is clicked |
| `OnTabClosed` | `EventCallback<string>` | — | Fired when a tab's X is clicked |
| `OnTabReordered` | `EventCallback<(string TabId, int NewIndex)>` | — | Fired after drag reorder |
| `OnTabPinToggled` | `EventCallback<string>` | — | Fired when pin is toggled |
| `OnTabGroupChanged` | `EventCallback<(string TabId, string? GroupName)>` | — | Fired when group changes |
| `OnGroupCollapseToggled` | `EventCallback<string>` | — | Fired when a group collapses |
| `ShowCloseButton` | `bool` | `true` | Close buttons on closable tabs |
| `ShowPinButton` | `bool` | `true` | Pin buttons in context menu |
| `AllowDragReorder` | `bool` | `true` | Enables drag-and-drop reorder |
| `AllowContextMenu` | `bool` | `true` | Right-click context menu |
| `CustomContextMenuItems` | `IReadOnlyList<ContextMenuItem>?` | — | Extra items appended to context menu |
| `TabStripCssClass` | `string?` | — | CSS class on the strip container |

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

Pinned tabs appear first, cannot be closed, and show a pin icon.

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
