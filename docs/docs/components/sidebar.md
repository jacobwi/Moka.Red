---
title: Sidebar
description: App side panel with a header, a scrolling body and a footer, plus mini, closed and overlay modes.
order: 119
---

# Sidebar

`MokaSidebar` is the side panel of an app layout: a header, a body that scrolls, and a footer. The body usually holds a `MokaMenu`. The sidebar can shrink to an icon-only mini mode, close to zero width, or float over the page on a backdrop for small screens.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Body content, usually a `MokaMenu`. The body scrolls when it overflows |
| `Header` | `RenderFragment?` | -- | Top section, such as a logo or app name, with a border below it |
| `Footer` | `RenderFragment?` | -- | Bottom section, such as the signed-in user, with a border above it |
| `Open` | `bool` | `true` | Whether the sidebar shows. Two-way bindable |
| `OpenChanged` | `EventCallback<bool>` | -- | Fires with `false` when Escape or a click on the backdrop closes an overlay sidebar |
| `Collapsed` | `bool` | `false` | Mini mode: the width switches to `CollapsedWidth`. The sidebar never collapses itself, so set it one way |
| `Width` | `string` | `"240px"` | Width when expanded |
| `CollapsedWidth` | `string` | `"56px"` | Width in mini mode |
| `Overlay` | `bool` | `false` | Fixes the sidebar to the edge of the viewport, over the page, with a backdrop. While open it takes focus and holds Tab like a dialog, and Escape or a click on the backdrop closes it |
| `Bordered` | `bool` | `true` | Border on the side that faces the page |
| `Elevated` | `bool` | `false` | Adds the `--moka-shadow-2` shadow |
| `Position` | `MokaSidebarPosition` | `Left` | `Left` or `Right`. Sets the side of the border and the edge an overlay sidebar sits on |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Space around the sidebar. Dropped while it is closed. See [Spacing](#spacing) |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the sidebar, around the header, body and footer. Dropped while it is closed |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the sidebar |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## App Layout

Put the sidebar and the page in a flex row. The sidebar fills the row's height, and its footer stays at the bottom.

```blazor-preview
<div style="display:flex;width:100%;height:280px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);overflow:hidden">
    <MokaSidebar Width="200px" aria-label="Main">
        <Header>
            <strong>Acme Console</strong>
        </Header>
        <ChildContent>
            <MokaMenu>
                <MokaMenuItem Text="Overview" Icon="MokaIcons.Navigation.Home" Href="#" Active />
                <MokaMenuItem Text="Projects" Icon="MokaIcons.File.Folder" Href="#" />
                <MokaMenuItem Text="Alerts" Icon="MokaIcons.Status.Bell" Href="#" Badge="3" />
                <MokaMenuItem Text="Settings" Icon="MokaIcons.Action.Settings" Href="#" />
            </MokaMenu>
        </ChildContent>
        <Footer>
            <span style="font-size:var(--moka-font-size-sm)">Ada Lovelace</span>
        </Footer>
    </MokaSidebar>
    <div style="flex:1;padding:var(--moka-spacing-lg)">
        <MokaHeading Level="4">Overview</MokaHeading>
        <MokaParagraph>The page content sits next to the sidebar.</MokaParagraph>
    </div>
</div>
```

## Mini Mode

The sidebar has no collapse button of its own. Put one in the header and set `Collapsed` on both the sidebar and the menu, so the menu hides its text too.

```blazor-preview
<div style="display:flex;width:100%;height:260px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);overflow:hidden">
    <MokaSidebar Width="200px" Collapsed="_collapsed" aria-label="Main">
        <Header>
            <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm"
                        StartIcon="@(_collapsed ? MokaIcons.Navigation.ChevronsRight : MokaIcons.Navigation.ChevronsLeft)"
                        aria-label="@(_collapsed ? "Expand sidebar" : "Collapse sidebar")"
                        OnClick="@(() => _collapsed = !_collapsed)" />
        </Header>
        <ChildContent>
            <MokaMenu Collapsed="_collapsed">
                <MokaMenuItem Text="Overview" Icon="MokaIcons.Navigation.Home" Href="#" Active />
                <MokaMenuItem Text="Projects" Icon="MokaIcons.File.Folder" Href="#" />
                <MokaMenuItem Text="Alerts" Icon="MokaIcons.Status.Bell" Href="#" />
                <MokaMenuItem Text="Settings" Icon="MokaIcons.Action.Settings" Href="#" />
            </MokaMenu>
        </ChildContent>
    </MokaSidebar>
    <div style="flex:1;padding:var(--moka-spacing-lg)">
        <MokaParagraph>The arrow button switches between the full sidebar and the icon-only rail.</MokaParagraph>
    </div>
</div>

@code {
    private bool _collapsed;
}
```

## Overlay

For small screens, `Overlay` puts the sidebar over the page on a dimmed, blurred backdrop. Opening it moves focus inside, and Escape or a click on the backdrop closes it. A close button in the header helps on touch screens, where there is no Escape key.

```blazor-preview
<div style="width:100%;height:220px">
    <MokaButton StartIcon="MokaIcons.Navigation.Menu" OnClick="@(() => _open = true)">Menu</MokaButton>
    <MokaSidebar Overlay @bind-Open="_open" Width="220px" aria-label="Main">
        <Header>
            <div style="display:flex;align-items:center">
                <strong>Acme Console</strong>
                <MokaSpacer />
                <MokaButton Variant="MokaVariant.Text" Size="MokaSize.Sm" StartIcon="MokaIcons.Navigation.Close"
                            aria-label="Close menu" OnClick="@(() => _open = false)" />
            </div>
        </Header>
        <ChildContent>
            <MokaMenu>
                <MokaMenuItem Text="Overview" Icon="MokaIcons.Navigation.Home" Href="#" Active />
                <MokaMenuItem Text="Projects" Icon="MokaIcons.File.Folder" Href="#" />
                <MokaMenuItem Text="Settings" Icon="MokaIcons.Action.Settings" Href="#" />
            </MokaMenu>
        </ChildContent>
    </MokaSidebar>
</div>

@code {
    private bool _open;
}
```

## Right Side

`Position="MokaSidebarPosition.Right"` moves the border to the left edge. Place the sidebar after the page content in the row.

```blazor-preview
<div style="display:flex;width:100%;height:240px;border:1px solid var(--moka-color-outline-variant);border-radius:var(--moka-radius-md);overflow:hidden">
    <div style="flex:1;padding:var(--moka-spacing-lg)">
        <MokaHeading Level="4">Installation</MokaHeading>
        <MokaParagraph>The article text goes here.</MokaParagraph>
    </div>
    <MokaSidebar Position="MokaSidebarPosition.Right" Width="180px" aria-label="On this page">
        <Header>
            <span style="font-size:var(--moka-font-size-xs);font-weight:var(--moka-font-weight-semibold);text-transform:uppercase;letter-spacing:0.08em">On this page</span>
        </Header>
        <ChildContent>
            <MokaMenu>
                <MokaMenuItem Text="Requirements" Href="#requirements" Active />
                <MokaMenuItem Text="Install the package" Href="#install" />
                <MokaMenuItem Text="Register services" Href="#register" />
            </MokaMenu>
        </ChildContent>
    </MokaSidebar>
</div>
```

## Spacing

`Margin`, `Padding` and `Rounded` apply to the sidebar's own `<nav>`. With a margin and a radius, an overlay sidebar floats clear of the screen edges:

```razor
<MokaSidebar @bind-Open="_open" Overlay Margin="MokaSpacingScale.Sm" Rounded="MokaRounding.Lg">
    ...
</MokaSidebar>
```

- A closed sidebar drops its margin and padding, so an inline one takes no room and an overlay one slides fully off the screen.
- An overlay sidebar is sized by the top and bottom of the viewport, so a vertical margin shrinks it rather than pushing it off the screen.
- An inline sidebar fills its parent's height with the margin inside it, so in a parent with a set height a vertical margin shrinks the sidebar instead of making it taller than the parent. Up to 0.1.12 it was `height: 100%`, and the margin came on top. A browser that supports neither the `stretch` keyword nor `-webkit-fill-available` for heights falls back to 100%.

Up to 0.1.12 the sidebar ignored all three.

## Behaviour

- The sidebar is a flex column that fills its parent's height. The header and footer keep their size and the body scrolls.
- Changes to the width, from `Width` or `Collapsed`, animate over `--moka-transition-slow`.
- A closed sidebar has zero width and is `inert`: its content stays in the page, but Tab skips it and screen readers leave it out. A sidebar collapsed to a zero `CollapsedWidth` is inert too. The icon rail at the default `56px` is not.
- With `Overlay`, the sidebar is fixed to the top and bottom of the viewport on the `Position` edge, at `--moka-z-modal`, above a backdrop at `--moka-z-modal-backdrop` tinted with `--moka-color-background`. A closed overlay sidebar slides off-screen.
- `Elevated` lifts the sidebar one layer above the page next to it, so the page's background does not cover the shadow.
- A one-way `Open` sets the starting state. After a backdrop click the sidebar stays closed when the parent re-renders, and opens again when the parent passes a different value. Use `@bind-Open` so the parent's field follows the backdrop.

Up to 0.1.12 a parent render reopened an unbound overlay sidebar the user had just closed, and the sidebar took a `CollapsedChanged` parameter that it never raised. It is gone: set `Collapsed` one way.

## Accessibility

- The sidebar renders a `<nav>` element, and unmatched attributes go on it. Name it with `aria-label`, such as `aria-label="Main"`, when the page has more than one navigation landmark. `MokaMenu` renders its own `<nav>`, so a menu inside a sidebar is a landmark inside another.
- In mini mode the menu hides its text, so each link or button in a `Collapsed` menu carries its `Text` as `aria-label` and `title`.
- An open overlay sidebar works like a modal dialog. Opening it moves focus to an element marked `autofocus` or `data-autofocus`, else its first control, else the sidebar itself. Content that already took focus inside keeps it.
- While it is open, Tab and Shift+Tab stay inside and wrap at the ends, and Escape closes it. Keys pressed inside it don't reach Blazor `@onkeydown` handlers on elements around it.
- Closing it, by Escape, the backdrop or `Open` set to `false`, returns focus to where it was before it opened, as long as focus was still in the sidebar. Turning `Overlay` off while it is open, as a layout that goes inline on wide screens does, only lets go of Tab: the sidebar stays on screen, so focus stays where it is.
- An inline sidebar has no keys of its own and leaves focus alone. Its keys reach the page as usual.

Up to 0.1.12 Escape did not close an overlay sidebar, opening one left focus on the page behind it, and a closed sidebar's links stayed in the tab order.
