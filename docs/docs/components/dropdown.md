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
| `ChildContent` | `RenderFragment?` | -- | The trigger element (usually a button) |
| `Items` | `RenderFragment?` | -- | Dropdown content (menu items) |
| `Open` | `bool` | `false` | Whether the dropdown is visible (two-way bindable) |
| `OpenChanged` | `EventCallback<bool>` | -- | Callback when open state changes |
| `Position` | `MokaPopoverPosition` | `BottomStart` | Position relative to the trigger |
| `CloseOnItemClick` | `bool` | `true` | Clicking a menu item closes the dropdown |
| `MatchWidth` | `bool` | `false` | Dropdown matches the trigger width |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaDropdownItem

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Custom content (overrides `Text`) |
| `Text` | `string?` | -- | Text label |
| `Icon` | `MokaIconDefinition?` | -- | Icon displayed before the text |
| `OnClick` | `EventCallback<MouseEventArgs>` | -- | Click handler |
| `Disabled` | `bool` | `false` | Whether the item is disabled |
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
