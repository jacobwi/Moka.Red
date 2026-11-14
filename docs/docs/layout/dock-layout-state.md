---
title: Dock Layout State
description: The dock layout's state - a tree you can bind, save and restore, change from code, switch between named layouts, and open panels in another window.
---

# Dock Layout State

A `MokaDockLayout`'s markup says where things start. Where they are now lives in a `MokaDockState`: the docked
tree of splits and groups, floating windows, groups pinned to side bars, closed panels, panels in other windows, and
which group is maximized. It is an immutable record, so the app can bind it, save it, compare it and change it with
plain functions.

## The tree

| Type | Holds |
|------|-------|
| `MokaDockState` | `Root` (the docked tree), `Floating`, `AutoHidden`, `Hidden`, `PoppedOut`, `Maximized`, `ActiveGroup`, `Version`, `AppVersion`. |
| `MokaDockSplitNode` | `Direction` (`Row` or `Column`) and `Children`. |
| `MokaDockGroupNode` | `Panels` (ids, in tab order), `Active`, `Collapsed`, and `Content` for the main area. |
| `MokaDockFloatingWindow` | A group with its `X`, `Y`, `Width`, `Height`, `Maximized`, and where it docks back. |
| `MokaDockAutoHidden` | A group pinned to a `Side`, and where it docks back. |
| `MokaDockParkedPanel` | A closed or popped-out panel, with where it goes back and its group's size. |

Every node has an `Id` and a `Size` along its parent split: a CSS length (`250px`, `25%`) or a share (`1.5fr`).

## Operations

Every operation returns a new state and leaves the parts it doesn't touch as the same instances, which is how the
layout redraws only the groups that changed. They are what the layout itself calls, so a test or a preset builder can
use them without rendering anything.

```csharp
MokaDockState next = state
    .Move("terminal", new MokaDockTarget("side", MokaDockZone.Center))   // into a group as a tab
    .Move("search", new MokaDockTarget("output", MokaDockZone.Right))     // a new group beside one
    .Move("problems", new MokaDockTarget(null, MokaDockZone.Bottom))     // a new group at the layout's edge
    .FloatPanel("explorer", x: 80, y: 60, width: 360, height: 280)
    .AutoHide("output", MokaDockPosition.Bottom)
    .Close("search")
    .Show("search");
```

| Method | Does |
|--------|------|
| `Activate(panel)` | Shows the panel in its group; a closed panel comes back first. |
| `Move(panel, target)`, `MoveGroup(group, target)` | Into a group as a tab, or a new group beside a node or at the layout's edge. |
| `Reorder(panel, index)` | Moves a tab within its group. |
| `Add(panel, target)` | Adds a panel the state doesn't hold yet. |
| `Close(panel, remember)`, `Show(panel)` | Takes a panel out, remembering where; brings it back there. |
| `FloatPanel`, `FloatGroup`, `MoveFloating`, `SetFloatingMaximized`, `BringToFront` | Floating windows. |
| `Dock(group)` | Puts a floating or pinned group back where it came from, at its size. |
| `AutoHide(group, side)` | Pins a group to a side bar. |
| `SetCollapsed`, `Resize(split, sizes)`, `Maximize(group)` | Folding, sizes, maximizing. |
| `PopOut`, `ReturnPoppedOut` | Another window. |
| `Reconcile(defaults, panels)` | Fits a saved state to the panels there are now. |
| `Reconcile(defaults, panels, current)` | The same, keeping what is open in `current`: panels opened from code, and panels shown in another window. |
| `Locate`, `FindGroup`, `FindNode`, `DockedGroups`, `AllGroups`, `Panels` | Queries. |

`MokaDockTarget(Node, Zone, Index)` names where something lands: `Zone.Center` joins the group as a tab at `Index`;
an edge puts a new group on that side of the node. A null `Node` is the layout's own edge.

## Binding

```razor
<MokaDockLayout @bind-State="_layout">
    ...
</MokaDockLayout>

@code {
    private MokaDockState? _layout;
}
```

`StateChanged` is raised after every change the person makes. A state passed in is fitted to the declared panels:
panels it doesn't mention join it where the markup has them, and panels the markup no longer has are dropped.
`CurrentState` and `DefaultState` read it from a reference.

## Saving and restoring

```razor
<MokaDockLayout StorageKey="editor-layout" StateVersion="2" MigrateState="Migrate">
```

With a `StorageKey` the layout restores the saved state on its first interactive render and saves after every
change. It uses the browser's local storage unless the app registers its own:

```csharp
builder.Services.AddMokaDockStorage<ProfileDockStorage>();   // an IMokaDockStorage: SaveAsync, LoadAsync, RemoveAsync
```

The JSON carries the format's `version`, which `MokaDockState.FromJson` brings up to date (a newer format, written by
a later release, is left alone and the layout starts from its markup), and the app's own `appVersion` from
`StateVersion`. When an app renames or regroups its panels, it raises `StateVersion` and gives `MigrateState`, which
gets an older saved state and its version and returns the fixed one, or null to start from the markup. Without a
`MigrateState`, an older saved state is fitted as it is and a newer one is dropped.

`ResetAsync()` goes back to the markup's layout and forgets the saved one. Panels opened from code stay open, and
panels shown in another window stay there (see [Panels from code](#panels-from-code)). Panels shown in another window when the page closed come
back to their places on the next load.

## Panels from code

```csharp
await _dock.OpenPanelAsync(new MokaDockPanelOptions
{
    Id = $"doc:{file.Path}",
    Title = file.Name,
    Icon = MokaIcons.File.FileText,
    Component = MokaDockComponent.Create<DocumentEditor>(new Dictionary<string, object?> { ["File"] = file }),
    CanClose = () => ConfirmDiscardAsync(file)
});
```

A panel opened from code lands in the main area as a tab (document tabs), or in the group used last when there is
no `MokaDockContent`, or at the `MokaDockTarget` given. `ActivatePanelAsync`, `ClosePanelAsync` (which asks
`CanClose` unless forced), `ShowPanelAsync`, `MovePanelAsync`, `FloatPanelAsync`, `AutoHidePanelAsync` and
`MaximizeAsync` work on any panel. `PanelOpened`, `PanelActivated`, `PanelClosed` and `PanelMoved` report each one.
A declared panel that closes can be shown again; one opened from code is forgotten. Opening a panel that is already
open shows it with the options given, so a new title, icon, badge or content reaches its tab and its content, such as
a mark on a document with unsaved changes. A panel opened from code stays open until it is closed: a preset, Reset or
a state bound from outside that doesn't mention it keeps it, in its group when that group is there, else where
`OpenPanelAsync` would open it.

## Named layouts

```razor
<MokaDockLayout @ref="_dock" Presets="_presets">
...
<MokaButton OnClick="@(() => _dock.ApplyPresetAsync("Debug"))">Debug layout</MokaButton>
```

A `MokaDockPreset` is a name and a state. `ApplyPresetAsync` switches to it inside a view transition, so the groups
glide to their places, unless the browser has none, the page is hidden or the person asked for less motion, and
completes once the new layout shows. Panels a preset doesn't mention join it where the markup has them; panels opened
from code stay open, and panels shown in another window stay there. A preset can be any saved state, such as one the
person saved with a name.

## Another window

**Open in new window** moves a panel to a browser window of its own. One Blazor circuit can't draw into two windows,
so the window is a second page, `/moka-dock-window`, that renders the panel in a circuit of its own:

- The panel's content has to be a component with parameters: `Component = MokaDockComponent.Create<T>(parameters)`.
  A `ChildContent` fragment closes over the first circuit and can't travel.
- On Blazor Server, register the service that links the two circuits and let the router find the page:

```csharp
builder.Services.AddMokaDockWindows();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MokaDockWindowPage).Assembly);
```

The window follows the parameters the layout passes the panel. **Bring back** in the window, `BringBackAsync` in the
app, or closing the window returns the panel where it was. The page needs an interactive render mode, as any page
the app routes to.

Where there is no window to open, such as a MAUI app's web view, or without `AddMokaDockWindows`, the menu doesn't
offer it and `PopOutPanelAsync` returns false. WebAssembly isn't supported yet: the second window would run its own
.NET runtime, and carrying a component's parameters across to it needs them serialized by type, which the trimmer
can't keep safe in general.
