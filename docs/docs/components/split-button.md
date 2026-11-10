---
title: Split Button
description: A button with a primary action and a dropdown arrow for secondary actions.
order: 54
---

# Split Button

`MokaSplitButton` combines a primary clickable button with a dropdown arrow that reveals additional actions. Useful for forms with a default action and alternatives (e.g. Save / Save As / Save Draft).

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment` | **required** | The primary button label |
| `OnClick` | `EventCallback` | -- | Callback for the primary button click |
| `DropdownContent` | `RenderFragment` | `null` | Content rendered inside the dropdown panel |
| `Color` | `MokaColor` | `Primary` | Color theme for the button |
| `Variant` | `MokaVariant` | `Filled` | Visual variant (Filled, Outlined, Soft, Text) |
| `Size` | `MokaSize` | `Md` | Button size |
| `Disabled` | `bool` | `false` | Disables both the button and the dropdown |
| `Rounded` / `RoundedValue` | `MokaRounding?` / `string?` | -- | Corner radius of the split button. Both halves take their outer corners from it |
| `Margin` / `MarginValue` | `MokaSpacingScale?` / `string?` | -- | Margin around the split button |
| `Padding` / `PaddingValue` | `MokaSpacingScale?` / `string?` | -- | Padding around the two halves |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

The primary half and the arrow take their outer corners from the split button, as the buttons in a `MokaButtonGroup` do, so a pill works:

```razor
<MokaSplitButton OnClick="Save" Rounded="MokaRounding.Full">
    <ChildContent>Save</ChildContent>
    <DropdownContent>
        <MokaDropdownItem Text="Save as..." />
    </DropdownContent>
</MokaSplitButton>
```

Up to 0.1.12 the two halves kept fixed corners, which hid a larger radius.

## Basic Split Button

```blazor-preview
<MokaSplitButton OnClick="() => { }">
    Save
</MokaSplitButton>
```

## Colors

```blazor-preview
<div style="display: flex; gap: 12px; flex-wrap: wrap;">
    <MokaSplitButton Color="MokaColor.Primary" OnClick="() => { }">Primary</MokaSplitButton>
    <MokaSplitButton Color="MokaColor.Secondary" OnClick="() => { }">Secondary</MokaSplitButton>
    <MokaSplitButton Color="MokaColor.Success" OnClick="() => { }">Success</MokaSplitButton>
    <MokaSplitButton Color="MokaColor.Error" OnClick="() => { }">Error</MokaSplitButton>
</div>
```

## With Dropdown Items

The arrow is named by `ToggleLabel` ("More options"), through `aria-label` and a `MokaTooltip` below it. The tooltip
hides while the menu is open, since the menu opens in the same place.

```blazor-preview
<MokaSplitButton OnClick="() => { }">
    Save
    <DropdownContent>
        <MokaDropdownItem Text="Save As..." Icon="MokaIcons.Action.Save" OnClick="() => { }" />
        <MokaDropdownItem Text="Save Draft" Icon="MokaIcons.File.FileText" OnClick="() => { }" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Export" Icon="MokaIcons.Action.Download" OnClick="() => { }" />
    </DropdownContent>
</MokaSplitButton>
```

## Above the page

The open menu sits in the browser's top layer, so a card that hides its overflow no longer cuts it off and nothing later on the page paints over it, whatever its `z-index`. It stays against the button as the page scrolls and flips to the other side when the window has no room there. While it's open the library sets its position, so a stylesheet can restyle it but not move it.

## Looks

The open menu's panel follows the theme's popup look, like every other floating panel. [Popover](/components/popover#looks) lists the seven looks. What you put in `DropdownContent` keeps its own styles.

```razor
<MokaThemeProvider Theme="MokaTheme.Default.WithPopups(MokaPopupLook.Glass)">
    @Body
</MokaThemeProvider>
```

The looks are drawn only on the new design. On `MokaTheme.Dark` and `Light` the menu looks as it always has.
