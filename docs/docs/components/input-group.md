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

## Three Connected Inputs

```blazor-preview
<MokaInputGroup>
    <MokaTextField Placeholder="City" />
    <MokaTextField Placeholder="State" />
    <MokaTextField Placeholder="ZIP" />
</MokaInputGroup>
```
