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
once and disposed with the layout. Panels that render together share that one import. A
splitter drag rewrites only its own track, so the other panels keep sizes such as
`min(300px, 30vw)`, `minmax(200px, 25%)` or `calc(100% - 4px)` exactly as written.

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
| `MinSize` | `string?` | `null` | Minimum size of the panel's track, as a CSS length. A splitter drag stops there too when it is in px, rem, em, % of the layout, vw or vh. |
| `MaxSize` | `string?` | `null` | Maximum size of the panel's track, with the same rules as `MinSize`. |
| `Resizable` | `bool` | `true` | Show a splitter handle and allow drag-resizing. |
| `Collapsible` | `bool` | `false` | Show a collapse toggle button in the header. |
| `Collapsed` | `bool` | `false` | Whether the panel is currently collapsed. Two-way bindable. The body stays mounted while collapsed. See [Header buttons and bound parameters](#header-buttons-and-bound-parameters). |
| `CollapsedSize` | `string?` | `"0px"` | Grid track size when collapsed, such as `"32px"` for a strip that keeps the title and the expand button. At a zero size the collapsed panel is inert. See [Collapsed strips](#collapsed-strips). |
| `Floating` | `bool` | `false` | Undock the panel as a free-floating window. Two-way bindable. Docking again keeps the size the panel had before it floated. |
| `FloatingX` | `double` | `100` | Floating window X position (px from left). Dragging the header moves the window; a new value from the parent moves it again. |
| `FloatingY` | `double` | `100` | Floating window Y position (px from top). Same rules as `FloatingX`. |
| `FloatingWidth` | `string` | `"300px"` | Width when floating. |
| `FloatingHeight` | `string` | `"400px"` | Height when floating. |
| `Scrollable` | `bool` | `true` | Scroll the body when its content overflows. Set `false` for content that scrolls itself; see [Content that scrolls itself](#content-that-scrolls-itself). |
| `Title` | `string?` | - | Plain-text header title. |
| `TitleContent` | `RenderFragment?` | - | Custom header title (overrides `Title`). |
| `Actions` | `RenderFragment?` | - | Content rendered at the right of the header. |
| `SizeChanged` | `EventCallback<double>` | - | Fires with the new pixel size after each resize. |

### CSS modifiers applied by the component

| Class | Condition |
|-------|-----------|
| `moka-dock-panel--left/right/top/bottom` | Dock position (docked only) |
| `moka-dock-panel--collapsed` | Collapsed and docked |
| `moka-dock-panel--resizable` | Resizable and docked |
| `moka-dock-panel--floating` | Floating mode |
| `moka-dock-panel-body--no-scroll` | On the body when `Scrollable="false"` |

While collapsed, the body carries the `hidden` attribute instead of being removed, and so does the
wrapper around `Actions` (`moka-dock-panel-custom-actions`). The collapse button carries
`aria-expanded`, and a panel collapsed to a zero size carries `inert`.

`MinSize` and `MaxSize` bound the grid track. A px `Size` between px bounds is clamped up front;
any other combination becomes CSS `min()`, `max()` or `clamp()` around the size, such as
`clamp(6rem, 240px, 40%)`. A `Size` that is not a length, such as `minmax(200px, 30%)`, is used
as written. Up to 0.1.12 the bounds went on the panel element as `min-width` and
`max-width`, where a percentage resolves against the panel's own track, so `MaxSize="40%"` left a
gap between the panel and the content.

### Header buttons and bound parameters

The collapse, undock and dock buttons change the panel's own state and report it through
`CollapsedChanged` and `FloatingChanged`. The panel keeps that state until the parent passes a
different value, so the buttons work with `@bind-Collapsed`, with a one-way `Collapsed="@x"`,
and with no binding at all. Up to 0.1.12 a parent render that repeated a one-way value undid the
button, so the collapse button seemed to do nothing.

A one-way value goes stale after a click: the parent's field still says `false` while the panel
is collapsed, and setting `false` again changes nothing. When the parent also sets the value,
bind it, or update the field in `CollapsedChanged`:

```razor
<MokaDockPanel Dock="MokaDockPosition.Left" Title="Explorer" Collapsible="true"
               Collapsed="_collapsed" CollapsedChanged="v => _collapsed = v">
    <FileTree />
</MokaDockPanel>

<MokaButton OnClick="() => _collapsed = !_collapsed">Toggle explorer</MokaButton>

@code {
    private bool _collapsed;
}
```

Docking a floating panel again, from its header button or from the parent, keeps the size the
panel had before it floated. A new `Size` from the parent is the only thing that replaces a size
the user dragged the splitter to. Up to 0.1.12 the header button reset it to `Size`.

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

Set `Floating="true"` to undock a panel. The header becomes draggable, with a mouse, a pen or a
finger, and the panel renders as a fixed overlay. Toggle between docked and floating via
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

## Collapsed strips

Set `CollapsedSize` to a non-zero value (e.g., `"32px"`) to keep a thin strip visible
when collapsed.

```razor
<MokaDockPanel Dock="MokaDockPosition.Left"
               Title="Explorer"
               Collapsible="true"
               CollapsedSize="32px"
               Size="220px">
    <PanelContent />
</MokaDockPanel>
```

The strip shows the title and the expand button. Beside the content (`Left`, `Right`) the header
turns into a column: the expand button at the top and the title running down the strip. Above or
below the content (`Top`, `Bottom`) it stays a row, centered in the strip. The undock button and
`Actions` are hidden until the panel expands; `Actions` stays mounted, so its components keep their
state. Leave room for the 22px expand button: a strip narrower than that clips it. Up to 0.1.12 the
header row ran past the edge of a side strip, so the expand button was out of sight but still took
focus, along with the undock button and the actions.

At the default `CollapsedSize="0px"` (or any other zero, such as `"0"` or `"0rem"`) the collapsed
panel is `inert`: nothing of it is on screen, so its buttons leave the tab order and the
accessibility tree. Expanding it is then up to the parent, for example a toolbar button that flips
a bound `Collapsed`:

```razor
<MokaButton Size="MokaSize.Xs" aria-pressed="@(_explorerCollapsed ? "false" : "true")"
            OnClick="() => _explorerCollapsed = !_explorerCollapsed">Explorer</MokaButton>

<MokaDockLayout Style="height: 100vh;">
    <MokaDockPanel Dock="MokaDockPosition.Left" Title="Explorer"
                   Collapsible="true" @bind-Collapsed="_explorerCollapsed">
        <FileTree />
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
