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
| `LabelWidth` | `string?` | -- | Fixed width for the label (e.g., `"120px"`), useful for aligned lists. Under `Columns` it is the width of the label column |
| `Copyable` | `bool` | `false` | Shows a copy button next to the value |
| `CopyLabel` | `string?` | `"Copy "` and the `Label` | Name of the copy button, in its tooltip and for screen readers. A list of key-values would otherwise give a row of buttons all called "Copy". The label's case is kept, since lower-casing would mangle the acronyms these labels are full of |
| `CopiedLabel` | `string?` | `"Copied!"` | Name of the copy button for the two seconds after a copy |
| `CopyTooltipPosition` | `MokaTooltipPosition` | `Bottom` | Where the copy button's tooltip opens |
| `Truncate` | `bool` | `false` | Truncates long values with an ellipsis |
| `Look` | `MokaKeyValueLook` | `Standard` | `Columns` puts the label in a column of its own as the small uppercase micro label, with the value beside it. See [Two columns](#two-columns) |
| `Mono` | `bool` | `false` | Draws the value in the monospace font with tabular figures, for ids, hashes, addresses and counts |
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

## Two columns

`Look="MokaKeyValueLook.Columns"` lays a key-value out in two columns: the label as the 11 px uppercase micro label (`.moka-micro-label`), the value beside it. The label column is a share of the key-value's own width, at most 11rem, so every key-value of a list starts its value at the same place without a width on each. Long labels and values wrap inside their columns. `Orientation` doesn't apply.

```razor
<div style="display: flex; flex-direction: column; gap: var(--moka-spacing-sm);">
    <MokaKeyValue Label="Name" Value="Avery Quinn" Look="MokaKeyValueLook.Columns" />
    <MokaKeyValue Label="Role" Value="Owner" Look="MokaKeyValueLook.Columns" />
    <MokaKeyValue Label="Devices signed in" Value="1" Look="MokaKeyValueLook.Columns" Mono />
    <MokaKeyValue Label="Account id" Value="6b4ffb87be0f51d7ed0215c8b75a7b60" Look="MokaKeyValueLook.Columns" Mono Copyable />
</div>
```

`LabelWidth` sets one key-value's label column. For a whole list, set `--moka-kv-label-width` on its parent once:

```razor
<div style="--moka-kv-label-width: 8rem;">
    <MokaKeyValue Label="Device id" Value="27ba14de32195aa5" Look="MokaKeyValueLook.Columns" Mono />
    <MokaKeyValue Label="Encryption suite" Value="Standard" Look="MokaKeyValueLook.Columns" />
</div>
```

`Mono` works in either look.
