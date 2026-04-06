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
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

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

```blazor-preview
<MokaSplitButton OnClick="() => { }">
    Save
    <DropdownContent>
        <MokaDropdownItem Text="Save As..." Icon="MokaIcons.Action.Save" OnClick="() => { }" />
        <MokaDropdownItem Text="Save Draft" Icon="MokaIcons.Content.Draft" OnClick="() => { }" />
        <MokaDropdownItem Divider />
        <MokaDropdownItem Text="Export" Icon="MokaIcons.Action.Download" OnClick="() => { }" />
    </DropdownContent>
</MokaSplitButton>
```
