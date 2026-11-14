---
title: Input Group
description: Groups inputs and buttons into a visually connected row.
order: 37
---

# Input Group

`MokaInputGroup` visually connects multiple inputs and buttons into a single cohesive row. Border radii are automatically adjusted so only the first and last children have rounded corners, creating a seamless grouped appearance.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | -- | Input and button elements to group |
| `Size` | `MokaSize` | `Md` | Size applied to all child inputs |
| `Disabled` | `bool` | `false` | Disables all child inputs |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Prefix and Input

```blazor-preview
<MokaInputGroup>
    <MokaButton Variant="MokaVariant.Outlined" Disabled>https://</MokaButton>
    <MokaTextField Placeholder="example.com" />
</MokaInputGroup>
```

## Input and Button

```blazor-preview
<MokaInputGroup>
    <MokaTextField Placeholder="Search..." />
    <MokaButton Color="MokaColor.Primary">Search</MokaButton>
</MokaInputGroup>
```

## A Labeled Field

A field in the group keeps its label above and its helper line below, and the buttons beside it meet its box: the group
lays its children out on three rows, labels, boxes and helper lines, so what a field draws above or below the box never
stretches a button or moves it out of line. Before 0.5.4 a joined button grew as tall as the label, box and helper
together.

```blazor-preview
@code { string _folder = ""; }
<MokaInputGroup>
    <MokaTextField @bind-Value="_folder" Label="Folder name" HelperText="Letters, digits and dashes." />
    <MokaButton Color="MokaColor.Primary">Create</MokaButton>
</MokaInputGroup>
```

For a button beside the box with a gap, not joined, use the field's `Actions`. See
[A button beside the box](/components/forms#a-button-beside-the-box).

## Three Connected Inputs

```blazor-preview
<MokaInputGroup>
    <MokaTextField Placeholder="City" />
    <MokaTextField Placeholder="State" />
    <MokaTextField Placeholder="ZIP" />
</MokaInputGroup>
```
