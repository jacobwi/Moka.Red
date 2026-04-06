---
title: Dock Layout
description: CSS Grid-based docking system with resizable, collapsible, and floating panels.
---

# Dock Layout

`MokaDockLayout` is a CSS Grid-based docking system that arranges panels around a central
content area — similar to an IDE shell. Panels snap to the `Left`, `Right`, `Top`, or
`Bottom` edges, have interactive splitter handles for resizing, and can be collapsed or
floated as independent windows.

## Components

| Component | Purpose |
|-----------|---------|
| `MokaDockLayout` | Root container. Manages the CSS grid and panel registry. |
| `MokaDockPanel` | A panel docked to one edge (or floating). |
| `MokaDockContent` | The central fill area. Always occupies the remaining space. |

---

## MokaDockLayout

`MokaDockLayout` accepts `MokaDockPanel` and `MokaDockContent` children and computes a
`grid-template-areas` / `grid-template-columns` / `grid-template-rows` layout dynamically
based on which panels are registered and whether they are collapsed.

Resizing and dragging are handled by a shared JS module (`moka-drag.js`) that is lazy-loaded
once and disposed with the layout.

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `ChildContent` | `RenderFragment?` | Panel and content children. |
| `Class` | `string?` | Additional CSS classes on the root element. |
| `Style` | `string?` | Additional inline styles. |

---

## MokaDockPanel

Each `MokaDockPanel` registers itself with the parent `MokaDockLayout` via a cascading
parameter. The panel renders a splitter handle (when `Resizable="true"`) that calls back
into .NET when dragging ends, triggering a grid re-layout without a full page refresh.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Dock` | `MokaDockPosition` | `Left` | Edge to dock to: Left, Right, Top, Bottom. |
| `Size` | `string` | `"250px"` | Initial panel size as a CSS value. |
| `MinSize` | `string?` | — | Minimum size during resize. |
| `MaxSize` | `string?` | — | Maximum size during resize. |
| `Resizable` | `bool` | `true` | Show a splitter handle and allow drag-resizing. |
| `Collapsible` | `bool` | `false` | Show a collapse toggle button in the header. |
| `Collapsed` | `bool` | `false` | Whether the panel is currently collapsed. Two-way bindable. |
| `CollapsedSize` | `string?` | `"0px"` | Grid track size when collapsed (can be a tab strip width, e.g. `"32px"`). |
| `Floating` | `bool` | `false` | Undock the panel as a free-floating window. Two-way bindable. |
| `FloatingX` | `double` | `100` | Floating window X position (px from left). |
| `FloatingY` | `double` | `100` | Floating window Y position (px from top). |
| `FloatingWidth` | `string` | `"300px"` | Width when floating. |
| `FloatingHeight` | `string` | `"400px"` | Height when floating. |
| `Title` | `string?` | — | Plain-text header title. |
| `TitleContent` | `RenderFragment?` | — | Custom header title (overrides `Title`). |
| `Actions` | `RenderFragment?` | — | Content rendered at the right of the header. |
| `SizeChanged` | `EventCallback<double>` | — | Fires with the new pixel size after each resize. |

### CSS modifiers applied by the component

| Class | Condition |
|-------|-----------|
| `moka-dock-panel--left/right/top/bottom` | Dock position (docked only) |
| `moka-dock-panel--collapsed` | Collapsed and docked |
| `moka-dock-panel--resizable` | Resizable and docked |
| `moka-dock-panel--floating` | Floating mode |

---

## MokaDockContent

`MokaDockContent` fills the `content` grid area — whatever space is left after the docked
panels claim their tracks. It accepts any `ChildContent` and forwards `Class`/`Style`.

---

## IDE-style layout example

```razor
<MokaDockLayout Style="height: 100vh;">

    <MokaDockPanel Dock="MokaDockPosition.Left"
                   Title="Explorer"
                   Size="240px" MinSize="160px" MaxSize="480px"
                   Collapsible="true"
                   @bind-Collapsed="_explorerCollapsed">
        <FileTree />
    </MokaDockPanel>

    <MokaDockPanel Dock="MokaDockPosition.Right"
                   Title="Properties"
                   Size="280px" MinSize="200px"
                   Collapsible="true">
        <PropertyGrid />
    </MokaDockPanel>

    <MokaDockPanel Dock="MokaDockPosition.Bottom"
                   Title="Output"
                   Size="180px" MinSize="80px"
                   Collapsible="true">
        <OutputConsole />
    </MokaDockPanel>

    <MokaDockContent>
        <CodeEditor />
    </MokaDockContent>

</MokaDockLayout>

@code {
    private bool _explorerCollapsed;
}
```

---

## Floating panel example

Set `Floating="true"` to undock a panel. The header becomes draggable and the panel
renders as an absolutely-positioned overlay. Toggle between docked and floating via
`@bind-Floating`.

```razor
<MokaDockPanel Dock="MokaDockPosition.Left"
               Title="Inspector"
               @bind-Floating="_isFloating"
               FloatingWidth="360px" FloatingHeight="480px">
    <InspectorContent />
</MokaDockPanel>

@code {
    private bool _isFloating;
}
```

---

## Collapsed size for tab strips

Set `CollapsedSize` to a non-zero value (e.g., `"32px"`) to keep a thin strip visible
when collapsed, which you can use to render a collapsed tab indicator.

```razor
<MokaDockPanel Dock="MokaDockPosition.Left"
               Collapsible="true"
               CollapsedSize="32px"
               Size="220px">
    <PanelContent />
</MokaDockPanel>
```
