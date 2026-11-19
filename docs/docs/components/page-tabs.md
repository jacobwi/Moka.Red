---
title: Page Tabs
description: The plain tabs on a page, with a line under the one showing or as a segmented switch.
order: 122
---

# Page Tabs

`MokaTabs` is for moving between the views of a page. For a closable, pinnable, reorderable session tab bar, use [`MokaTabStrip`](/components/tabs).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaTabItem>` | **required** | The tabs, in order |
| `Label` | `string` | **required** | What the tabs choose between, for screen readers: "Firewall views" |
| `Active` | `string?` | - | The `Id` of the tab showing. Use `@bind-Active` to hear changes |
| `ActiveChanged` | `EventCallback<string>` | - | Raised when the person picks another tab |
| `Segmented` | `bool` | `false` | A segmented switch instead of a line under the tab showing |
| `Stretch` | `bool` | `false` | The tabs share the full width |
| `Overflow` | `MokaTabsOverflow` | `Scroll` | What happens to tabs that don't fit: the list scrolls, or `Menu` puts them behind a `+2` button. See [When they don't fit](#when-they-dont-fit) |
| `MoreLabel` | `Func<int, string>?` | "2 more tabs" | Names the menu button, from how many tabs are in the menu |
| `Panel` | `RenderFragment?` | - | The showing tab's content, wrapped as its tab panel. Leave it out to put the content anywhere on the page |
| `IconOnly` | `MokaTabsIconOnly` | `Never` | When tabs with an icon show only the icon: `Narrow` in a box under 480 px (30rem), `Always` at every width. See [Icons only](#icons-only) |
| `TooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where an icon-only tab's tooltip opens. `Top` suits tabs along the bottom of a screen |
| `GroupLook` | `MokaTabsGroupLook` | `Label` | Once tabs have a `Group`: `Label` shows each group's name before its first tab, `Divider` only a line between groups. See [Groups](#groups) |
| `Class` | `string?` | - | Additional CSS classes on the root |
| `Style` | `string?` | - | Additional inline styles on the root |

Other attributes land on the tab list. `Margin`, `Padding` and `Rounded` land on the root.

## MokaTabItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Id` | `string` | **required** | Stable and unique among the tabs. It is what `Active` holds |
| `Text` | `string` | **required** | The tab's name |
| `Count` | `int?` | - | A number beside the name. Null shows none, and 0 shows 0 |
| `Tone` | `MokaBadgeTone` | `Count` | What `Count` means. See [Badge](/components/badge) |
| `Href` | `string?` | - | An address of its own, so the tab is a link. Without one the tab only switches state. `javascript:`, `vbscript:` and `data:` addresses are dropped |
| `Icon` | `MokaIconDefinition?` | - | An icon before the name |
| `Disabled` | `bool` | `false` | The tab can't be picked, and the arrow keys pass over it |
| `Group` | `string?` | - | The name of the tab's group. Tabs next to each other with the same group show together. Set it with an initializer: `new("disks", "Disks") { Group = "Storage" }` |

## Basic

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Device views">
    <Panel>Showing @_active.</Panel>
</MokaTabs>

@code {
    private static readonly IReadOnlyList<MokaTabItem> _tabs =
    [
        new("all", "All", 14),
        new("limited", "Limited", 2, MokaBadgeTone.Limited),
        new("needs", "Needs you", 1, MokaBadgeTone.NeedsYou),
        new("updates", "Updates", 3, MokaBadgeTone.Ready),
        new("guests", "Guests")
    ];

    private string? _active = "all";
}
```

## Segmented

Drawn the same as `MokaSegmentedControl`. Use that one to choose a value, and this one when the choice shows a panel or has its own address: a radio group has neither.

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Log views" Segmented />
```

## Stretch

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Range" Stretch />
```

## Each tab its own address

A tab with `Href` is a real link, so it opens in a new tab with a middle click and the back button works. Set `Active` from the page you land on.

```razor
<MokaTabs Items="_tabs" Active="tailnet" Label="VPN sections" />

@code {
    private static readonly IReadOnlyList<MokaTabItem> _tabs =
    [
        new("tailnet", "Tailnet", Href: "/vpn/tailnet"),
        new("peers", "Peers", Href: "/vpn/peers")
    ];
}
```

## Icons only

A phone's tab strip shows icons. `IconOnly` hides the names of tabs that have an icon, the way every icon-only control
in the library does it: the tab is named by `aria-label` for screen readers, with its count when it has one ("Photos
12"), and its name shows in a `MokaTooltip` on hover or keyboard focus, never a `title`. A tab without an icon keeps its
name. Counts still show on their badges, and the arrow keys and the overflow menu work as before; the menu lists names.

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Sections" IconOnly="MokaTabsIconOnly.Narrow"
          Segmented Stretch TooltipPosition="MokaTooltipPosition.Top" />
```

`Always` shows icons at every width. `Narrow` shows names until the tabs' own box is under 480 px (30rem), by a
container query with no script, and keeps the tooltips quiet while the names show. A container takes no width from
what's in it, so with `Narrow` the tabs fill the width they're given. Either way each tab sits inside its tooltip's
element, so a stylesheet of your own that reached a tab as a direct child of the list reaches the tooltip's element
instead.

## Groups

A page with many views can group its tabs. Give each tab a `Group`: tabs next to each other with the same group show
together, the group's name stands before its first tab as a small label, and a line stands between two groups.

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Server settings" Overflow="MokaTabsOverflow.Menu" />

@code {
    private static readonly IReadOnlyList<MokaTabItem> _tabs =
    [
        new("overview", "Overview") { Group = "General" },
        new("activity", "Activity", 3, MokaBadgeTone.NeedsYou) { Group = "General" },
        new("disks", "Disks") { Group = "Storage" },
        new("shares", "Shares", 12) { Group = "Storage" },
        new("interfaces", "Interfaces") { Group = "Network" },
        new("names", "Names") { Group = "Network" }
    ];

    private string? _active = "overview";
}
```

`GroupLook="MokaTabsGroupLook.Divider"` keeps only the lines, for a row with little room. While the tabs show only their
icons (`IconOnly`) the names hide the same way. Tabs in no group need nothing before them, and after a group they start
with a line. A group that comes back later in the list starts again with its name, so keep a group's tabs together.

All the tabs stay in one tab list with one tab stop, and the arrow keys walk them in order, from one group into the
next. A tab list may hold only tabs (WAI-ARIA 1.2), so each name sits in an element hidden from screen readers, and each
tab points at it with `aria-describedby`, so screen readers give the group as the tab's description, after its name.

`Segmented`, `Stretch` and links work with groups as they do without. With `Overflow="MokaTabsOverflow.Menu"` a group's
name goes once all its tabs are in the menu, and the menu lists its tabs under their group's name, each group a
`role="group"` named after it.

## Keyboard

The tabs follow the WAI-ARIA tabs pattern with automatic activation: moving to a tab shows it, and on a tab with an address, goes there. One tab is in the tab order at a time.

| Key | Action |
|-----|--------|
| Right, Left | The next or previous tab, wrapping at the ends, passing over disabled ones and going from one group into the next |
| Home, End | The first or last tab, whatever its group |
| Space | Shows the focused tab |
| Tab | Leaves the list, for the panel |

## When they don't fit

By default the list scrolls sideways with no scrollbar. Below 720 px the tabs take a little less padding.

With `Overflow="MokaTabsOverflow.Menu"` tabs that don't fit go into a menu behind a `+2` button at the end of the row. The last tab goes first, and the tab showing always stays, taking the place of another if it would have dropped.

```razor
<MokaTabs Items="_tabs" @bind-Active="_active" Label="Project views" Overflow="MokaTabsOverflow.Menu" />
```

The menu lists each tab with its icon, count and disabled state. A tab with `Href` is still a real link there: it goes to its address, and a middle click opens it in a new tab.

The arrow keys, Home and End pass over tabs in the menu, and Tab goes from the tab showing to the `+2` button. Enter, Space or Down open the menu.

`Segmented` and `Stretch` work with it. A segmented list stays as wide as its tabs, with the button in its own box right after it. With `Stretch` the tabs that fit share the width left beside the button.

Measuring takes a small script in the browser, and it calls .NET only when which tabs are in the menu changes. The script can only measure tabs already on screen, a moment after they render, and after the page goes live when it was prerendered. Until then the row shows whole tabs only: a tab that doesn't fit waits on a hidden second row rather than showing cut off at the edge, and the button's room is kept, so the tabs you see first are the ones that stay. Two things can still change when the measure runs: a group's name at the end of the row goes if its first tab didn't fit, and a tab showing that sits past the end of the row (one picked from the menu, on a page that draws its tabs again) takes the place of the tab before it. A segmented list spans the row until then, and hugs its tabs once measured.

On a static page, with no interactivity, there is no script and the list scrolls. If the tab showing alone is wider than the row, the list scrolls too.

Tests that render the tabs with `Overflow="MokaTabsOverflow.Menu"` in bUnit set the renderer first, as the tabs ask whether the page is interactive: `SetRendererInfo(new RendererInfo("Server", true))`.
