---
title: Toolbar, AppBar & StatusBar
description: Horizontal action bars for toolbars, app navigation headers, and status footers.
order: 22
---

# Toolbar, AppBar & StatusBar

Moka.Red provides three horizontal bar components: `MokaToolbar` for action/control bars, `MokaAppBar` for top navigation headers, and `MokaStatusBar` for bottom status footers.

---

## MokaToolbar

A horizontal bar of actions and controls suitable for rich text editors, action bars, or any row of grouped controls. Flows in-document (not fixed).

### MokaToolbar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Toolbar content (buttons, selects, dividers) |
| `Dense` | `bool` | `true` | Compact height (32px) |
| `Bordered` | `bool` | `true` | Border around the toolbar |
| `Elevated` | `bool` | `false` | Elevation shadow |
| `Rounded` | `MokaRounding?` | `Md` | Border radius |
| `Wrap` | `bool` | `false` | Allow items to wrap to next line |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaToolbarDivider

A vertical separator between toolbar sections. No parameters.

### MokaToolbarGroup Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Group content |
| `Label` | `string?` | -- | Optional label above the group |

### Basic Toolbar

```blazor-preview
<MokaToolbar>
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Save" />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Content.Copy" />
    <MokaToolbarDivider />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit" />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Delete" />
</MokaToolbar>
```

### Grouped Toolbar

```blazor-preview
<MokaToolbar>
    <MokaToolbarGroup Label="File">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Save" />
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Download" />
    </MokaToolbarGroup>
    <MokaToolbarDivider />
    <MokaToolbarGroup Label="Edit">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Edit" />
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Content.Copy" />
    </MokaToolbarGroup>
</MokaToolbar>
```

### Elevated Toolbar

```blazor-preview
<MokaToolbar Elevated Bordered="false">
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Soft">Action A</MokaButton>
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Soft">Action B</MokaButton>
</MokaToolbar>
```

---

## MokaAppBar

Top navigation bar with title, start/end content areas, and optional fixed positioning.

### Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom center content |
| `Title` | `string?` | -- | Title text (ignored if `TitleContent` is set) |
| `TitleContent` | `RenderFragment?` | -- | Rich title content |
| `StartContent` | `RenderFragment?` | -- | Left side content (menu button, logo) |
| `EndContent` | `RenderFragment?` | -- | Right side content (actions, avatar) |
| `Elevated` | `bool` | `true` | Elevation shadow |
| `Bordered` | `bool` | `false` | Bottom border |
| `Fixed` | `bool` | `false` | Sticky positioning at top |
| `Dense` | `bool` | `true` | Compact height |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### Basic AppBar

```blazor-preview
<MokaAppBar Title="My Application">
    <StartContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Navigation.Menu" />
    </StartContent>
    <EndContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" StartIcon="MokaIcons.Action.Settings" />
        <MokaAvatar Initials="JD" Size="MokaSize.Sm" />
    </EndContent>
</MokaAppBar>
```

### Bordered (No Elevation)

```blazor-preview
<MokaAppBar Title="Flat AppBar" Elevated="false" Bordered>
    <EndContent>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Outlined">Sign In</MokaButton>
    </EndContent>
</MokaAppBar>
```

---

## MokaStatusBar

Bottom status bar similar to VS Code's status bar. Fixed to the viewport bottom with compact height and small font. Supports start (left) and end (right) content areas.

### MokaStatusBar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Generic status bar content |
| `StartContent` | `RenderFragment?` | -- | Left-aligned items |
| `EndContent` | `RenderFragment?` | -- | Right-aligned items |
| `Bordered` | `bool` | `true` | Top border |
| `Class` | `string?` | -- | Additional CSS classes |

### MokaStatusBarItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content (overrides `Text`/`Icon`) |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Makes the item a button: tab stop, focus ring, Enter and Space |
| `Tooltip` | `string?` | -- | Shown above the item in a `MokaTooltip` on hover and on keyboard focus, wrapping at 20rem. Names an item that shows only an icon; describes one that shows text |
| `Class` | `string?` | -- | Additional CSS classes |

A status bar sits at the bottom of the window, so an item's tooltip opens above it, and near either end of the window it
slides along to stay in view. A long tooltip wraps rather than running off the window, and a line break in it starts a
new line. Up to 0.1.19 the tooltip was a `title`, which showed the browser's own hint.

With a tooltip the item sits inside the tooltip's element, `.moka-tooltip-trigger.moka-statusbar-item-tooltip`, which is
what the bar lays out; without one it is drawn as before. Two items with tooltips are therefore no longer adjacent
siblings, and a flex setting meant for the item's place in the bar (`min-width: 0`, `flex-shrink`) belongs on that
element. Reach it from your own CSS through the item's class:

```css
/* A separator between items, and a session item that gives way first when the bar is narrow. */
.status ::deep .moka-statusbar-item-tooltip + .moka-statusbar-item-tooltip { border-left: 1px solid var(--moka-color-outline); }
.status ::deep .moka-statusbar-item-tooltip:has(> .session) { min-width: 0; flex-shrink: 4; }
```

### Basic StatusBar

```blazor-preview
<MokaStatusBar>
    <StartContent>
        <MokaStatusBarItem Icon="MokaIcons.Status.CheckCircle" Text="Ready" />
        <MokaStatusBarItem Text="main" Icon="MokaIcons.File.Code" />
    </StartContent>
    <EndContent>
        <MokaStatusBarItem Text="UTF-8" />
        <MokaStatusBarItem Text="Ln 42, Col 8" />
    </EndContent>
</MokaStatusBar>
```

### Interactive Items

```blazor-preview
@code {
    string _encoding = "UTF-8";
}
<MokaStatusBar>
    <StartContent>
        <MokaStatusBarItem Text="Connected" Icon="MokaIcons.Status.CheckCircle" />
    </StartContent>
    <EndContent>
        <MokaStatusBarItem Text="@_encoding" OnClick="@(() => _encoding = _encoding == "UTF-8" ? "ASCII" : "UTF-8")" Tooltip="Click to toggle encoding" />
    </EndContent>
</MokaStatusBar>
```

An item with `OnClick` is a button to the keyboard and to screen readers. It joins the tab order with a focus ring, and Enter or Space click it without scrolling the page. Items without `OnClick` are plain text and take no focus. Up to 0.1.12 clickable items could only be used with the mouse.
