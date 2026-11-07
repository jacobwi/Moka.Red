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
| `Panel` | `RenderFragment?` | - | The showing tab's content, wrapped as its tab panel. Leave it out to put the content anywhere on the page |
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

## Keyboard

The tabs follow the WAI-ARIA tabs pattern with automatic activation: moving to a tab shows it, and on a tab with an address, goes there. One tab is in the tab order at a time.

| Key | Action |
|-----|--------|
| Right, Left | The next or previous tab, wrapping at the ends and passing over disabled ones |
| Home, End | The first or last tab |
| Space | Shows the focused tab |
| Tab | Leaves the list, for the panel |

## When they don't fit

The list scrolls sideways with no scrollbar. Below 720 px the tabs take a little less padding.
