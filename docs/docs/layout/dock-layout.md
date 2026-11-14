---
title: Dock Layout
description: Panels docked around a main area, as in an IDE - tabbed groups in rows and columns, drag to dock, floating windows, side bars, and looks.
---

# Dock Layout

`MokaDockLayout` arranges panels around a main area, the way an IDE does. Panels sit as tabs in groups; groups sit
in rows and columns at any depth. The person can drag a tab or a whole group to any edge of any group or of the
layout, float it in a window, pin it to a side bar, maximize it, and move it back from the keyboard. The layout keeps
where everything is in a `MokaDockState`, which binds, saves and restores (see
[Dock Layout State](/layout/dock-layout-state)).

Every panel's content renders once, where the markup declares it, and is drawn over its group's slot with CSS
anchor positioning. A move changes only which slot it is drawn over, so a panel keeps its component state, its
scroll position and its focus when it moves to another group, floats, pins to a side bar or comes back.

> The examples on this page use the API added in Moka.Red 0.6.0, so they are shown as code rather than live previews
> until the docs site's preview package moves to that release.

## Components

| Component | Purpose |
|-----------|---------|
| `MokaDockLayout` | The root. Holds the state, draws the groups and handles every drag, key and window. |
| `MokaDockRow`, `MokaDockColumn` | A split: its parts side by side, or one above the other. They nest. |
| `MokaDockGroup` | Panels that start as tabs in one place. |
| `MokaDockPanel` | One panel: a tab, with a title, an icon, a badge and its content. |
| `MokaDockContent` | The main area. It takes the space the groups leave, and panels opened from code land in it. |

## An IDE layout

```razor
<MokaDockLayout StorageKey="ide">
    <MokaDockRow>
        <MokaDockGroup Id="side" Size="260px" MinSize="160px">
            <MokaDockPanel Id="explorer" Title="Explorer" Icon="MokaIcons.File.Folder">
                <FileTree />
            </MokaDockPanel>
            <MokaDockPanel Id="search" Title="Search" Icon="MokaIcons.Action.Search" Badge="3">
                <SearchPane />
            </MokaDockPanel>
        </MokaDockGroup>
        <MokaDockColumn>
            <MokaDockContent>
                <WelcomePage />
            </MokaDockContent>
            <MokaDockGroup Id="output" Size="200px">
                <MokaDockPanel Id="terminal" Title="Terminal" Scrollable="false">
                    <MokaTerminal Lines="_lines" />
                </MokaDockPanel>
                <MokaDockPanel Id="problems" Title="Problems">
                    <ProblemList />
                </MokaDockPanel>
            </MokaDockGroup>
        </MokaDockColumn>
    </MokaDockRow>
</MokaDockLayout>
```

A size is a CSS length (`260px`, `25%`, `12rem`) or a share (`2fr`); a part without one takes one share of what the
lengths leave. Give every panel an `Id`: it is how the state, the code API and a saved layout find it.

## One panel per side

The markup from before 0.6.0 still works. A panel placed directly in the layout docks to its `Dock` edge around
`MokaDockContent`, as a group of one; two panels on the same edge share that group as tabs (they used to overlap).

```razor
<MokaDockLayout>
    <MokaDockPanel Dock="MokaDockPosition.Left" Title="Explorer" Size="240px" Collapsible="true" CollapsedSize="32px">
        <FileTree />
    </MokaDockPanel>
    <MokaDockPanel Dock="MokaDockPosition.Bottom" Title="Output" Size="180px" @bind-Size="_outputSize">
        <OutputPane />
    </MokaDockPanel>
    <MokaDockContent>
        <Editor />
    </MokaDockContent>
</MokaDockLayout>
```

`MokaDockGroup` takes `Dock` too, for a group of tabs on one edge.

## Tabs

Each group shows its panels as tabs, in the style the layout's `TabStyle` or the group's own `TabStyle` picks. A
group with one panel shows that panel's title instead, unless the style is `Editor` or the look is `Ide`.

| `MokaDockTabStyle` | Looks like |
|--------------------|------------|
| `Underline` | Plain tabs with a line under the one showing, as `MokaTabs` draws them. The default. |
| `Editor` | Boxed tabs joined to the panel, each with its own close button. |
| `Pills` | Rounded pills, the one showing filled. |
| `Switcher` | The showing panel's title, with a list of the others to switch to. |

Tabs follow the WAI-ARIA tabs pattern with automatic activation: the arrows, Home and End pick a tab, and Delete
closes one that can close. When a strip runs out of room an **All tabs** button lists every tab, and a tab that
becomes the one showing, such as one dropped into the group, scrolls into view. A tab drags along its strip to
reorder.

Content renders the first time its tab shows (`Lazy`, default true) and stays mounted while another tab shows
(`KeepAlive`, default true). `KeepAlive="false"` removes it while hidden and renders it afresh each time.

## Drag to dock

Drag a tab to move one panel, or a group's header to move the whole group. While dragging, the layout shows where it
will land:

| `MokaDockDropIndicator` | What shows |
|-------------------------|------------|
| `Zones` | The half of the group under the pointer that a new group would take, or the whole group when the panel would join it as a tab; a strip along the layout's edge near it. The default. |
| `Compass` | Five targets in the middle of the group under the pointer (four edges and the middle) and four at the layout's edges. Only a target under the pointer drops anything. |

Dropping over a tab strip puts the tab at that place. Dropping where nothing lights up floats the panel there.
Escape cancels. `AllowDragging="false"` turns dragging off.

The header's menu does the same from the keyboard: **Dock left**, **Dock right**, **Dock top**, **Dock bottom**,
**Move to** each other group, **Undock**, **Auto-hide**, **Open in new window** and **Close**.

## Splits

Splitters are focusable `role="separator"` elements, named by the two groups they sit between, with `aria-valuenow`
giving the first one's share of the pair. Dragging one or pressing the arrows (Shift for bigger steps) resizes only
the two groups beside it; Home and End go to the limits; a double-click or Enter puts the pair back to their
starting sizes. `MinSize` and `MaxSize` on a group bound both. The size reaches .NET once, when the drag ends or the
key is let go.

## Floating windows

**Undock** floats a group in a window over the layout, no larger than the layout and inside it; `FloatPanelAsync`
does the same from code. A window comes to the front when it is clicked or gets focus,
moves by its header, resizes from any edge or corner, and snaps to the layout's edges and to the other windows. Its
size and place clamp to the layout, so it stays on screen when the layout shrinks. **Maximize** fills the layout and
**Dock** puts the group back where it came from, at the size it had. On a legacy panel, `FloatingX`, `FloatingY`,
`FloatingWidth` and `FloatingHeight` bind (`@bind-FloatingX`), and `Floatable="false"` turns floating off.

## Side bars

**Auto-hide** pins a group to a side bar of its tabs, as an IDE does. Clicking or hovering a tab
(`AutoHideOnHover`) slides the group in over the content; a press outside it or Escape sends it back. **Dock**
in its header returns it to its place.

## Header controls

The layout's `Controls` picks the buttons group headers show: `Floating`, `Collapse`, `Maximize`, `Close` and
`Menu` (`MokaDockControls.Default` is all but `Close`). A panel's `Closable` overrides `Close` for that panel, and its
`CanClose` can refuse, such as a document with unsaved changes. A maximized group fills the layout until its
button or Escape restores it. A panel's `Actions` sit before the layout's buttons while it shows.

## Keyboard and screen readers

| Key | Does |
|-----|------|
| F6, Shift+F6 | Move to the next or previous group, landing on its tab or its content. |
| Ctrl+Tab, Ctrl+Shift+Tab | Show the next or previous tab of the focused group. A browser tab keeps Ctrl+Tab for itself, so there the arrows on the tab strip do it; in a MAUI app it reaches the layout. |
| Arrows, Home, End | On a tab strip, pick a tab. On a splitter, resize. |
| Delete | Close the focused tab, when it can close. |
| Escape | Restore a maximized group, close a side bar's flyout, or cancel a drag. |
| Tab | Follows what is on screen: a group's header, then its content, then the next group. |

Each group is a region named by its title, which owns its showing panel's content for assistive technology. A moved,
floated, pinned or closed panel is announced.

## Looks

`Look` on the layout, or `MokaTheme.WithDocks` for the whole app, picks how it is drawn on the new design. The
original design (`MokaTheme.Dark`, `MokaTheme.Light`) always draws `Classic`.

| `MokaDockLook` | Looks like |
|----------------|------------|
| `Classic` | Panels on the surface with a line between them and a title over each. The default. |
| `Ide` | Tab strips for headers, an activity bar of icons for closed and pinned panels, and hairline splitters. |
| `Cards` | Each group a raised card with a gap of page background between them. |
| `Minimal` | Headers only while a group is hovered or holds focus, hairline splitters and tinted panes. |
| `Terminal` | Monospace titles in brackets, square corners, and the accent along the active group's edge. |

```csharp
builder.Services.AddMokaRed(options => options.Theme = MokaTheme.Default.WithDocks(MokaDockLook.Ide));
```

## Narrow screens

Below `NarrowBelow` (default `40rem`) of the layout's own width, not the window's, the layout switches to
`NarrowMode`. Measuring the layout itself means a layout in a side panel narrows with its panel, which a media query
on the window can't do; a container query alone can't either, because the narrow modes change what is rendered, not
only how. The layout's script watches its width and tells .NET once each time it crosses the line.

| `MokaDockNarrowMode` | Shows |
|----------------------|-------|
| `Sheets` | The main area fills the layout, and the other panels open as bottom sheets from a bar of their icons. The default. |
| `Tabs` | Every panel as a tab of one group, the main area first. |
| `Stack` | The groups one above the other, each folding open and shut. |

The saved layout stays the wide one. In `Sheets`, a panel opened in a sheet is created when the sheet opens and
removed when it closes, since `MokaBottomSheet` is modal, holds focus inside itself and removes its content when
closed; keep state that must outlive that in a service. `Tabs` and `Stack` keep every panel's state.

## Content that scrolls itself

`Scrollable="false"` on a panel or the main area clips instead of scrolling, and a single child fills it, so a view
that scrolls itself (a terminal, an editor) gets a definite size and shows the only scrollbar.

## Parameters

### MokaDockLayout

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | The rows, columns, groups, panels and the main area. |
| `State` | `MokaDockState?` | - | Where everything is. Two-way bindable. |
| `StateChanged` | `EventCallback<MokaDockState>` | - | Raised after every change the person makes. |
| `StorageKey` | `string?` | - | Saves and restores the state under this key. |
| `StateVersion` | `int` | `0` | The app's own version of its layout. |
| `MigrateState` | `Func<MokaDockState, int, MokaDockState?>?` | - | Brings a layout saved by an older `StateVersion` up to date. |
| `Look` | `MokaDockLook?` | theme | How the layout is drawn. |
| `TabStyle` | `MokaDockTabStyle` | `Underline` | How groups show their tabs. |
| `DropIndicator` | `MokaDockDropIndicator` | `Zones` | What shows where a dragged panel lands. |
| `Controls` | `MokaDockControls` | `Default` | The buttons group headers show. |
| `AllowDragging` | `bool` | `true` | Whether tabs and groups drag. |
| `AutoHideOnHover` | `bool` | `true` | Whether a side bar tab opens its group on hover as well as on click. |
| `NarrowMode` | `MokaDockNarrowMode` | `Sheets` | How a narrow layout shows its panels. |
| `NarrowBelow` | `string?` | `40rem` | The layout's own width below which it is narrow. Null never narrows. |
| `Presets` | `IReadOnlyList<MokaDockPreset>?` | - | Named layouts for `ApplyPresetAsync`. |
| `Text` | `MokaDockText?` | English | Every word the layout writes. |
| `PanelOpened`, `PanelActivated`, `PanelClosed`, `PanelMoved` | `EventCallback<MokaDockPanelChange>` | - | Raised for each panel that opens, shows, closes or moves. |

### MokaDockRow, MokaDockColumn

| Parameter | Type | Description |
|-----------|------|-------------|
| `Id` | `string?` | The split's id in the state. |
| `Size` | `string?` | Its size along the split it is in. |
| `ChildContent` | `RenderFragment?` | Its parts. |

### MokaDockGroup

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Id` | `string?` | generated | The group's id in the state. |
| `Size` | `string?` | one share | Its starting size along its split. |
| `MinSize`, `MaxSize` | `string?` | 48px, none | How far its splitters move. |
| `Resizable` | `bool` | `true` | Whether the splitters beside it move. |
| `Collapsible`, `Collapsed`, `CollapsedSize` | `bool`, `bool`, `string?` | `false`, `false`, `0px` | A fold-to-strip button, its starting state, and the strip's size. |
| `TabStyle` | `MokaDockTabStyle?` | layout's | This group's tabs. |
| `Dock` | `MokaDockPosition?` | - | Directly in the layout: the edge it docks to. |
| `AutoHide` | `MokaDockPosition?` | - | Starts pinned to this side's bar. |
| `Title` | `string?` | showing panel's | The group's name for screen readers and menus. |

### MokaDockPanel

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Id` | `string?` | generated | The panel's id, also the id of the element its content draws in. |
| `Title`, `TitleContent`, `Icon` | | - | Its tab: title, custom title, icon. |
| `Badge`, `BadgeTone` | `int?`, `MokaBadgeTone` | -, `Count` | A count on its tab. |
| `ChildContent` | `RenderFragment?` | - | Its content. |
| `Component` | `MokaDockComponent?` | - | A component as its content, which can open in another window. |
| `Actions` | `RenderFragment?` | - | Buttons in the header while it shows. |
| `Closable`, `CanClose` | `bool?`, `Func<Task<bool>>?` | layout's `Controls` | Whether it closes, and a question asked first. |
| `Floatable` | `bool` | `true` | Whether it can float at all. |
| `Lazy`, `KeepAlive` | `bool` | `true` | Render on first show; stay mounted while hidden. |
| `Visible`, `Active`, `AutoHide` | `bool`, `bool`, `MokaDockPosition?` | `true`, `false`, - | Starts closed, starts as its group's showing tab, starts pinned. |
| `Scrollable` | `bool` | `true` | Scroll or clip its content. |
| `Dock`, `Size`, `MinSize`, `MaxSize`, `Resizable` | | `Left`, `250px` | Directly in the layout: its edge and track. `Size` binds (`SizeChanged` is `EventCallback<string>`). |
| `Collapsible`, `Collapsed`, `CollapsedSize` | | | Its group's fold button and state; `Collapsed` binds. |
| `Floating`, `FloatingX`, `FloatingY`, `FloatingWidth`, `FloatingHeight` | | `false`, 100, 100, `300px`, `400px` | Its floating window; each binds. |

### MokaDockContent

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | - | What the main area shows while no panel is open in it. |
| `Size` | `string?` | one share | Its size in a row or column. |
| `Title` | `string?` | `Content` | Its name for screen readers and the narrow layout's tab. |
| `Scrollable` | `bool` | `true` | Scroll or clip its content. |
