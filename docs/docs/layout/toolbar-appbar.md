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

On every theme but `MokaTheme.Dark` and `Light`, items wrap onto another line when they don't fit, so the bar grows rather than cutting anything off, and a text button with the default color is drawn quiet rather than in the accent. The heights come from the theme's density.

### MokaToolbar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Toolbar content (buttons, selects, dividers) |
| `Dense` | `bool` | `true` | Compact height (`--moka-height-toolbar-dense`) |
| `Bordered` | `bool` | `true` | Border around the toolbar |
| `Elevated` | `bool` | `false` | Elevation shadow |
| `Rounded` | `MokaRounding?` | `Md` | Border radius |
| `Wrap` | `bool` | `false` | Allow items to wrap to next line. On `MokaTheme.Dark` and `Light` only: every other theme always wraps. `Expandable` wins |
| `Expandable` | `bool` | `false` | Keeps the bar on one row. Controls that don't fit drop out, and a button opens the bar onto more rows. See [When it runs out of room](#when-a-toolbar-runs-out-of-room) |
| `MoreLabel` | `Func<int, string>?` | "Show 2 more tools" | Names that button, from how many controls didn't fit |
| `LessLabel` | `string` | "Show fewer tools" | Names that button while the bar is open |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaToolbarDivider

A vertical separator between toolbar sections. No parameters. In an `Expandable` toolbar it goes once it no longer has a control on both sides.

### MokaToolbarGroup Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Group content |
| `Label` | `string?` | -- | Optional label for the group: beside it on most themes, above it on `MokaTheme.Dark` and `Light` |
| `Priority` | `int` | `0` | Which groups stay when an `Expandable` toolbar runs out of room. The lowest goes first |

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

### When a toolbar runs out of room

A toolbar that doesn't set `Expandable` wraps or overflows as it always has. With `Expandable` it stays on one row:

1. A control that may shrink gives way first. Give it `min-width: 0`, or a `flex` and a `min-width` of your own. A text field shrinks down to `8rem` by itself.
2. Then whole controls drop out, lowest priority first, and among equals the last one first. A divider goes once it no longer has a control on both sides.
3. A `...` button appears. It opens the bar onto more rows with those same controls in their places, so a search box or a select works there just as it did on the row. The bar closes itself once everything fits.

```razor
<MokaToolbar Expandable="true">
    <MokaToolbarGroup Label="File" Priority="10">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text">Save</MokaButton>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text">Export</MokaButton>
    </MokaToolbarGroup>
    <MokaToolbarDivider />
    <MokaToolbarGroup Label="Insert">
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text">Image</MokaButton>
        <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text">Table</MokaButton>
    </MokaToolbarGroup>
    <MokaSpacer />
    <MokaButton Size="MokaSize.Xs" Variant="MokaVariant.Text" data-moka-priority="5">Settings</MokaButton>
</MokaToolbar>
```

Whatever sits directly in the toolbar counts as one control, so a `div` around several drops as one. `MokaToolbarGroup` has `Priority`, and anything else can carry `data-moka-priority`. A `MokaSpacer` never drops or counts.

A dropped control leaves the tab order and the accessibility tree, and one holding keyboard focus goes last. Controls come back when there's room with a little to spare.

Measuring takes a small script in the browser, and it calls .NET only when the number of dropped controls changes. On a static page, or until the script has run, the controls wrap and nothing is hidden, because there's no button yet to reach them.

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
| `Fixed` | `bool` | `false` | Sticks to the top of the window, and reports its height so a link to part of a page lands below it. See [Fixed bars and links](#fixed-bars-and-links) |
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

### Fixed bars and links

A `Fixed` bar sticks to the top of the window, so it covers whatever the window scrolls to there. It publishes its height as `--moka-appbar-space` on the root element, and `moka.css` makes that the page's `scroll-padding-top`. A link like `layout#Card`, the browser's own jump on load, and anything else that scrolls an element into view stop below the bar, whatever height it wraps to. `MokaAppShell`'s phone bar does the same, and `MokaActionBar` sets the bottom padding the same way.

Your own sticky parts can keep clear of the bar with it too:

```css
.my-filters {
    position: sticky;
    top: var(--moka-appbar-space, 0px);
}
```

Let the window scroll, not a box around the page. A box with its own `overflow` holds every sticky part inside it to itself.

---

## MokaStatusBar

Bottom status bar similar to VS Code's status bar. Fixed to the viewport bottom with compact height and small font. Supports start (left) and end (right) content areas. With `Fixed="false"` it is a row under whatever it follows instead, such as a file list's item count and selection under the list.

```razor
<div class="explorer">
    <FileList />
    <MokaStatusBar Fixed="false">
        <StartContent>
            <MokaStatusBarItem Text="128 items" />
            <MokaStatusBarItem Text="3 selected, 4.2 MB" />
        </StartContent>
    </MokaStatusBar>
</div>
```

### MokaStatusBar Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Generic status bar content |
| `StartContent` | `RenderFragment?` | -- | Left-aligned items |
| `EndContent` | `RenderFragment?` | -- | Right-aligned items |
| `Bordered` | `bool` | `true` | Top border |
| `Fixed` | `bool` | `true` | Fixed to the bottom of the window, across its width. `false` lays it out where it is, as a row across its container |
| `Look` | `MokaStatusBarLook?` | `null` | How this bar is drawn. Null takes the theme's choice, `Standard` unless the app set one |
| `Expandable` | `bool` | `false` | Shows a button with the number of items that didn't fit. It opens the bar onto more rows |
| `MoreLabel` | `Func<int, string>?` | "Show 2 more items" | Names that button, from how many items didn't fit |
| `LessLabel` | `string` | "Show fewer items" | Names that button while the bar is open |
| `Class` | `string?` | -- | Additional CSS classes |

### MokaStatusBarItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Priority` | `int` | `0` | Which items stay when the bar runs out of room. The lowest goes first |
| `ChildContent` | `RenderFragment?` | -- | Custom content (overrides `Text`/`Icon`) |
| `Text` | `string?` | -- | Display text |
| `Icon` | `MokaIconDefinition?` | -- | Icon before the text |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Makes the item a button: tab stop, focus ring, Enter and Space |
| `Tooltip` | `string?` | -- | Shown above the item in a `MokaTooltip` on hover and on keyboard focus, wrapping at 20rem. Names an item that shows only an icon; describes one that shows text |
| `TooltipClass` | `string?` | -- | Classes for the tooltip element that wraps the item when it has a `Tooltip`. That element, not the item, is what the status bar's flex layout sees, so `min-width`, `flex-shrink` and `order` belong here |
| `TooltipStyle` | `string?` | -- | Inline styles for that same wrapper |
| `Class` | `string?` | -- | Additional CSS classes. Stays on the item and styles the item itself, with or without a tooltip |

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

### When it runs out of room

The two sides never overlap, at any width, with nothing to set. The start side takes what the end side leaves. When they don't fit:

1. An item that may shrink gives way first. Give it `min-width: 0` (through `TooltipClass` if it has a tooltip) and its text ends in an ellipsis.
2. Then whole items drop out, lowest `Priority` first. Among equals the start side goes before the end side, last item first. A separator goes once it no longer has an item on both sides.

```razor
<MokaStatusBar>
    <StartContent>
        <MokaStatusBarItem Text="Connected" Priority="10" />
        <MokaStatusBarItem Text="@_network" />
        <MokaStatusBarItem Text="@_address" Priority="-1" />
    </StartContent>
    <EndContent>
        <MokaStatusBarItem Text="@_rates" Priority="5" />
    </EndContent>
</MokaStatusBar>
```

A dropped item leaves the tab order and the accessibility tree, and an item holding keyboard focus goes last. Items come back when there's room, with a little to spare, so text that changes length doesn't make its neighbors blink.

Whatever sits directly in a side counts as one item, so a `div` around several items drops as one. Put `data-moka-priority` on your own element to rank it.

Measuring takes a small script in the browser, with no calls to .NET. On a static page the stylesheet alone keeps the sides apart and removes what doesn't fit, last in the source first, and `Priority` has no effect.

To let people reach what dropped out, set `Expandable`. A button with the count (`+2`) appears only while something doesn't fit. It opens the bar onto more rows, upward from the bottom of the window, with those items in their usual places, and the bar closes itself once everything fits. This one does talk to .NET, only when the count changes.

### Looks

`Standard` is the default, and a bar only changes if an app asks.

| Look | What it draws |
|------|---------------|
| `Standard` | A filled bar with a line above it |
| `Accent` | The whole bar in the accent. Strong, for one bar per window |
| `Hairline` | No fill, a thin line above and muted text. The quietest |
| `Floating` | Lifted off the window's edges with rounded corners and a shadow |
| `Chips` | No bar, each item its own small outlined chip. Separators don't draw |
| `Segments` | Edge to edge blocks with hard dividers, the first one tinted. Separators don't draw |
| `Terminal` | Monospace capitals on the page color, under a line in the accent |

Set one for the whole app in the theme, so an appearance screen can offer it, or on one bar. A bar's own `Look` wins, and `Look="MokaStatusBarLook.Standard"` keeps a bar plain whatever the theme says.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithStatusBar(MokaStatusBarLook.Accent)">

<MokaStatusBar Look="MokaStatusBarLook.Terminal" Expandable="true">
```

Like the other style choices, `WithStatusBar` does nothing on `MokaTheme.Dark` and `Light`. A bar's own `Look` works on every theme.

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
