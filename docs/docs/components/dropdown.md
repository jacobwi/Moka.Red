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
| `Disabled` | `bool` | `false` | Whether the item is disabled. The arrow keys skip it and a click on it does nothing |
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
| Enter / Space | Open the menu on its first item | Activate the item |
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

A one-way `Open` sets the starting state and applies again only when its value changes, so a menu the user opened stays open when the parent re-renders. Use `@bind-Open` to track it.

Up to 0.1.12 the items took no focus and the arrow keys did nothing, the menu closed only after the item's `OnClick` finished, a click on a disabled item closed the menu, Escape inside a dialog closed the dialog as well, and a parent re-render with a one-way `Open` closed a menu the user had opened.
