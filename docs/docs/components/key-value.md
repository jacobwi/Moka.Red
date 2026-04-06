---
title: Key-Value
description: Key-value pair display for metadata, details, and summary information.
order: 38
---

# Key-Value

`MokaKeyValue` renders a labeled key-value pair for displaying metadata, configuration details, or summary information. It supports horizontal and vertical orientations, fixed label widths for alignment, copyable values, and rich content via `ChildContent`.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Label` | `string` | -- | The key/label text |
| `Value` | `string?` | -- | Plain-text value (ignored when `ChildContent` is provided) |
| `ChildContent` | `RenderFragment?` | -- | Rich content to render as the value |
| `Orientation` | `MokaDirection` | `Row` | Layout direction: `Row` (horizontal) or `Column` (vertical/stacked) |
| `LabelWidth` | `string?` | -- | Fixed width for the label (e.g., `"120px"`), useful for aligned lists |
| `Copyable` | `bool` | `false` | Shows a copy button next to the value |
| `Truncate` | `bool` | `false` | Truncates long values with an ellipsis |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<MokaKeyValue Label="Name" Value="Moka.Red" />
<MokaKeyValue Label="Version" Value="0.1.3" />
<MokaKeyValue Label="License" Value="MIT" />
```

## Horizontal List with Fixed Label Width

Align labels across multiple key-value pairs for a clean layout.

```blazor-preview
<div style="display:flex;flex-direction:column;gap:4px">
    <MokaKeyValue Label="Status" Value="Active" LabelWidth="100px" />
    <MokaKeyValue Label="Created" Value="2026-01-15" LabelWidth="100px" />
    <MokaKeyValue Label="Last Modified" Value="2026-04-01" LabelWidth="100px" />
    <MokaKeyValue Label="Owner" Value="admin@example.com" LabelWidth="100px" />
</div>
```

## Vertical Stacked

```blazor-preview
<div style="display:flex;gap:24px">
    <MokaKeyValue Label="Total Users" Value="12,345" Orientation="MokaDirection.Column" />
    <MokaKeyValue Label="Active Today" Value="1,023" Orientation="MokaDirection.Column" />
    <MokaKeyValue Label="Revenue" Value="$45,678" Orientation="MokaDirection.Column" />
</div>
```

## Copyable

```blazor-preview
<MokaKeyValue Label="API Key" Value="sk-moka-abc123def456" Copyable />
```

## Rich Content via ChildContent

```blazor-preview
<MokaKeyValue Label="Status" LabelWidth="100px">
    <MokaTag Text="Active" Color="MokaColor.Success" Pill />
</MokaKeyValue>
```
