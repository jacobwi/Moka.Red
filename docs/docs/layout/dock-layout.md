---
title: Dock Layout
description: CSS Grid-based docking system with resizable, collapsible, and floating panels.
---

# Dock Layout

`MokaDockLayout` is a CSS Grid-based docking system that arranges panels around a central
content area, like an IDE shell. Panels snap to the `Left`, `Right`, `Top`, or
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

The grid follows every change to a panel's `Dock`, `Size`, `MinSize`, `MaxSize`, `Collapsed`,
`CollapsedSize` or `Floating` in the same render, whether it comes from the panel's own header
buttons, a splitter drag, or a parameter the parent sets (a toolbar button bound to
`Collapsed`, for example).

Resizing and dragging are handled by a shared JS module (`moka-drag.js`) that is lazy-loaded
once and disposed with the layout. A splitter drag rewrites only its own track, so the other
panels keep sizes such as `min(300px, 30vw)`, `minmax(200px, 25%)` or `calc(100% - 4px)`
exactly as written.

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
| `Size` | `string` | `"250px"` | Panel size as a CSS value. A new value from the parent replaces a size the user dragged to. |
| `MinSize` | `string?` | — | Minimum size during resize. |
| `MaxSize` | `string?` | — | Maximum size during resize. |
| `Resizable` | `bool` | `true` | Show a splitter handle and allow drag-resizing. |
| `Collapsible` | `bool` | `false` | Show a collapse toggle button in the header. |
| `Collapsed` | `bool` | `false` | Whether the panel is currently collapsed. Two-way bindable. The body stays mounted while collapsed. |
| `CollapsedSize` | `string?` | `"0px"` | Grid track size when collapsed (can be a tab strip width, e.g. `"32px"`). |
| `Floating` | `bool` | `false` | Undock the panel as a free-floating window. Two-way bindable. |
| `FloatingX` | `double` | `100` | Floating window X position (px from left). |
| `FloatingY` | `double` | `100` | Floating window Y position (px from top). |
| `FloatingWidth` | `string` | `"300px"` | Width when floating. |
| `FloatingHeight` | `string` | `"400px"` | Height when floating. |
| `Scrollable` | `bool` | `true` | Scroll the body when its content overflows. Set `false` for content that scrolls itself; see [Content that scrolls itself](#content-that-scrolls-itself). |
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
| `moka-dock-panel-body--no-scroll` | On the body when `Scrollable="false"` |

While collapsed, the body carries the `hidden` attribute instead of being removed.

---

## MokaDockContent

`MokaDockContent` fills the `content` grid area: whatever space is left after the docked
panels claim their tracks. It accepts any `ChildContent` and forwards `Class`/`Style`.

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | `null` | The main area's content. |
| `Scrollable` | `bool` | `true` | Scroll the area when its content overflows. Set `false` for content that scrolls itself. Adds `moka-dock-content--no-scroll`. |

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

---

## Collapsing keeps content mounted

A collapsed panel hides its body with the `hidden` attribute instead of removing it, so the
components inside keep their state across a collapse and expand, such as a scroll position or
a running terminal session. While hidden, the body takes no space and is out of the tab order
and the accessibility tree.

Because the content stays alive, anything it runs (a timer, a subscription) keeps running while
the panel is collapsed. Bind `Collapsed` if a child needs to pause.

---

## Content that scrolls itself

The panel body and `MokaDockContent` scroll when their content overflows. A view that scrolls
on its own, such as a terminal or a code editor, then sits inside a second scroll container and
shows two scrollbars, or gets cut off if it sizes itself to its container.

Set `Scrollable="false"` on the panel or the content area. The region then clips instead of
scrolling and lays out as a flex column, and a single child fills it, so the child gets a
definite size and its own scrollbar is the only one:

```razor
<MokaDockLayout Style="height: 100vh;">

    <MokaDockPanel Dock="MokaDockPosition.Bottom"
                   Title="Terminal"
                   Size="240px"
                   Collapsible="true"
                   Scrollable="false">
        <MokaTerminal Lines="_output" ShowHeader="false" MaxHeight="none" />
    </MokaDockPanel>

    <MokaDockContent Scrollable="false">
        <CodeEditor />
    </MokaDockContent>

</MokaDockLayout>

@code {
    private readonly List<MokaTerminalLine> _output = [];
}
```

`MaxHeight="none"` lifts `MokaTerminal`'s default 400px cap on its output, so it can fill a
taller panel.

With several children, each keeps its own height. Give the one that should take the rest of
the space `flex: 1` and `min-height: 0`:

```razor
<MokaDockPanel Dock="MokaDockPosition.Bottom" Title="Output" Scrollable="false">
    <OutputFilter />
    <MokaTerminal Lines="_output" ShowHeader="false" MaxHeight="none"
                  Style="flex: 1; min-height: 0;" />
</MokaDockPanel>
```
