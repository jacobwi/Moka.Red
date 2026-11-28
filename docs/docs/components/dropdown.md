---
title: Dropdown
description: Dropdown menu with trigger button and action items.
order: 24
---

# Dropdown

`MokaDropdown` displays a list of action items when triggered. It uses `MokaPopover` internally with click trigger. Simpler than ContextMenu for common dropdown patterns.

## Parameters

### MokaDropdown

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | The trigger element (usually a button). Its first focusable element is the menu button |
| `Items` | `RenderFragment?` | -- | Dropdown content (menu items) |
| `Open` | `bool` | `false` | Whether the dropdown is visible (two-way bindable). A one-way value applies only when it changes |
| `OpenChanged` | `EventCallback<bool>` | -- | Callback when open state changes |
| `Position` | `MokaPopoverPosition` | `BottomStart` | Position relative to the trigger |
| `CloseOnItemClick` | `bool` | `true` | Clicking a menu item closes the dropdown, before the item's `OnClick` runs |
| `MatchWidth` | `bool` | `false` | Dropdown matches the trigger width |
| `Look` | `MokaPopupLook?` | `null` | How the menu panel is drawn, passed to its popover. Null takes the theme's popup look. See [Looks](#looks) |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the dropdown in the page |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding inside the menu panel |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the menu panel |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The dropdown's own root element takes no part in the layout (`display: contents`), so `Id`, `Class`, `Style` and the margin go on the wrapper of the `MokaPopover` inside it, the box that holds the trigger. The menu's id is `{Id}-menu`. The padding and radius go on the menu panel. Up to 0.1.12 the dropdown ignored `Margin`, `Padding`, `Rounded` and `Style`.

### MokaDropdownItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content (overrides `Text`) |
| `Text` | `string?` | -- | Text label |
| `Icon` | `MokaIconDefinition?` | -- | Icon displayed before the text |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler. Enter and Space raise it too |
| `Disabled` | `bool` | `false` | Whether the item is disabled. The arrow keys skip it and a click on it does nothing. A link loses its address |
| `Href` | `string?` | -- | An address, so the item is a link: an `a` element with `role="menuitem"`. A `javascript:`, `vbscript:` or `data:` address is not rendered and the item stays a plain one. See [Links](#links) |
| `Target` | `string?` | -- | Where the link opens, such as `"_blank"`. Only used with `Href`. One that opens another tab gets `rel="noopener noreferrer"`, an external-link mark and `NewTabText` |
| `NewTabText` | `string` | `"opens in a new tab"` | What screen readers hear after the name of a link whose `Target` opens another tab, after a comma |
| `Danger` | `bool` | `false` | Draws the item in the error color, on a red tint while it's hovered or focused. See [Danger items](#danger-items) |
| `Checked` | `bool?` | -- | Makes the item an option that is on or off: `role="menuitemcheckbox"` with `aria-checked`, and a check in the icon's place while it's on. Null is a plain item. A link ignores it. See [Checked items](#checked-items) |
| `Divider` | `bool` | `false` | Renders as a horizontal divider instead of an item |
| `Class` | `string?` | -- | Additional CSS classes |

## Basic Usage

```blazor-preview
<MokaDropdown>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronDown">
            Actions
        </MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Edit" Icon="MokaIcons.Action.Edit" />
        <MokaDropdownItem Text="Duplicate" Icon="MokaIcons.Content.Copy" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Delete" Icon="MokaIcons.Action.Delete" />
    </Items>
</MokaDropdown>
```

## With Click Handlers

```blazor-preview
@code {
    string _lastAction = "None";
}
<MokaDropdown>
    <ChildContent>
        <MokaButton>Menu</MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Option A" OnClick="@(() => _lastAction = "A")" />
        <MokaDropdownItem Text="Option B" OnClick="@(() => _lastAction = "B")" />
        <MokaDropdownItem Text="Disabled" Disabled />
    </Items>
</MokaDropdown>
<MokaText>Last action: @_lastAction</MokaText>
```

## Links

An item with `Href` is a link, an `a` element with `role="menuitem"` as in the WAI-ARIA menu button with links. The browser's own link handling works: a middle click or Ctrl+click opens it in a new tab, the link's context menu can copy or open it, and a `download` attribute (passed through to the link) makes it a download. Choosing it still closes the dropdown and raises `OnClick`. Enter and Space follow the link like a click.

```razor
<MokaDropdown>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronDown">Report</MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Open" Href="/reports/q3" Icon="MokaIcons.Navigation.ChevronRight" />
        <MokaDropdownItem Text="Open in a new tab" Href="/reports/q3" Target="_blank" />
        <MokaDropdownItem Text="Download PDF" Href="/reports/q3.pdf" download="q3-report.pdf" Icon="MokaIcons.Action.Download" />
        <MokaDropdownItem Text="Archived" Href="/reports/q3/archive" Disabled />
    </Items>
</MokaDropdown>
```

A disabled link has no `href`, `target` or `rel`, so it goes nowhere, and keeps `aria-disabled="true"`. An address the library won't render (`javascript:`, `vbscript:`, `data:`) leaves a plain item, as `MokaButton` and `MokaMenuItem` do.

A `Target` that opens another tab (`_blank` or a window's name) gives the link `rel="noopener noreferrer"`, so the page it opens can't reach this one, an external-link mark after its text and `NewTabText` for screen readers. A `rel` attribute stays on a link that keeps to this tab. Enter and Space click the link in the browser, inside the key press, so another tab opens as it does for a click.

## Danger items

`Danger` marks an item that destroys or removes something, such as Delete. The item and its icon are drawn in the error color, and a hovered or focused item keeps that color over a red tint. On the new design the color is the popup look's danger color when it has one (the Contrast look lightens it), else `--moka-color-error-soft`, the error color meant for text. The context menu (`MokaContextMenuItem.Danger`) and the menu bar (`MokaMenuBarItem.Danger`) draw it the same way.

```razor
<MokaDropdown>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronDown">Actions</MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Edit" Icon="MokaIcons.Action.Edit" />
        <MokaDropdownItem Text="Duplicate" Icon="MokaIcons.Content.Copy" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Delete" Icon="MokaIcons.Action.Delete" Danger OnClick="DeleteAsync" />
    </Items>
</MokaDropdown>
```

It's the look only: ask before anything that can't be undone. Without `Danger` an item looks as it always has.

## Checked items

`Checked` turns an item into an option that is on or off, such as "Show grid". While it's on, a check in the accent color takes the icon's place. While it's off, the item shows its `Icon`, or an empty space as wide as the check, so the names line up. The app keeps the state, so flip it in `OnClick`. The context menu (`MokaContextMenuItem.Checked`) and the account card's menu (`MokaAccountItem.Checked`) draw a check too.

```razor
<MokaDropdown>
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronDown">View</MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Show grid" Checked="_grid" OnClick="() => _grid = !_grid" />
        <MokaDropdownItem Text="Show rulers" Checked="_rulers" OnClick="() => _rulers = !_rulers" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Reset view" Icon="MokaIcons.Action.Refresh" />
    </Items>
</MokaDropdown>

@code {
    private bool _grid = true;
    private bool _rulers;
}
```

Choosing a checked item closes the menu like any other, unless the dropdown's `CloseOnItemClick` is off. An item with `Href` is a link, never a checkbox, so it ignores `Checked`. Without `Checked` an item looks and reads as it always has.

## Match Width

```blazor-preview
<div style="width:200px">
    <MokaDropdown MatchWidth>
        <ChildContent>
            <MokaButton FullWidth Variant="MokaVariant.Outlined">Select Action</MokaButton>
        </ChildContent>
        <Items>
            <MokaDropdownItem Text="First option" />
            <MokaDropdownItem Text="Second option" />
            <MokaDropdownItem Text="Third option" />
        </Items>
    </MokaDropdown>
</div>
```

## Keyboard

The dropdown follows the WAI-ARIA menu button pattern. The menu button is the first focusable element in `ChildContent`, so put a button there: a trigger with nothing focusable in it cannot be reached from the keyboard.

| Key | On the menu button | In the menu |
|-----|--------------------|-------------|
| Enter / Space | Open the menu on its first item | Activate the item. A link item follows its address |
| Down Arrow | Open the menu on its first item | Move to the next item, wrapping at the end |
| Up Arrow | Open the menu on its last item | Move to the previous item, wrapping at the start |
| Home / End | | Move to the first or last item |
| Escape | | Close the menu and return focus to the menu button |
| Tab / Shift+Tab | Close the menu | Close the menu. Tab moves on past the button, Shift+Tab lands on it |

Disabled items are skipped. A menu opened with the mouse takes focus itself rather than its first item, so nothing looks picked, and Down Arrow still reaches the first item. Activating an item puts focus back on the menu button, then closes the menu, then runs the item's `OnClick`, so a dialog the item opens returns focus to the button when it closes.

Escape in an open dropdown closes only the dropdown, not a `MokaDialog` around it.

## Accessibility

- The menu button gets `aria-haspopup="menu"`, `aria-expanded` as `"true"` or `"false"`, `aria-controls` while the menu is open, and an `id` when it has none.
- The menu has `role="menu"` and is labelled by the menu button. Items have `role="menuitem"`, a disabled item has `aria-disabled="true"`, and a divider has `role="separator"`.
- An item with `Href` is an `a` element with `role="menuitem"`, so screen readers announce it as a menu item and the browser still treats it as a link.
- An item with `Checked` has `role="menuitemcheckbox"` and `aria-checked` as `"true"` or `"false"`, so screen readers say whether it's on. The check is hidden from them, since `aria-checked` says it.
- `Danger` is color only. Name the action in its text ("Delete"), since the color alone doesn't say it.

A one-way `Open` sets the starting state and applies again only when its value changes, so a menu the user opened stays open when the parent re-renders. Use `@bind-Open` to track it.

Up to 0.1.12 the items took no focus and the arrow keys did nothing, the menu closed only after the item's `OnClick` finished, a click on a disabled item closed the menu, Escape inside a dialog closed the dialog as well, and a parent re-render with a one-way `Open` closed a menu the user had opened.

## Above the page

The open menu sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the trigger as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.

## Looks

The menu follows the theme's popup look, `MokaTheme.WithPopups(MokaPopupLook)`, like every other floating panel. [Popover](/components/popover#looks) lists the seven looks. The rows follow it too: their corners, the lit row's tint, text color and glow, the icons and the dividers.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithPopups(MokaPopupLook.Elevated)">
    @Body
</MokaThemeProvider>
```

`Look` on one dropdown wins over the theme for its menu:

```razor
<MokaDropdown Look="MokaPopupLook.Terminal">
    <ChildContent>
        <MokaButton Variant="MokaVariant.Outlined" EndIcon="MokaIcons.Navigation.ChevronDown">Branch</MokaButton>
    </ChildContent>
    <Items>
        <MokaDropdownItem Text="Checkout" Icon="MokaIcons.Navigation.ChevronRight" />
        <MokaDropdownItem Text="Rename" Icon="MokaIcons.Action.Edit" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Delete" Icon="MokaIcons.Action.Delete" />
    </Items>
</MokaDropdown>
```

The looks are drawn only on the new design. On `MokaTheme.Dark` and `Light` the menu looks the same whatever the theme or `Look` says.
