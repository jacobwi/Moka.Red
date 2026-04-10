---
title: Bento Grid
description: Asymmetric grid layout where items can span multiple rows and columns for dashboard and feature layouts.
order: 86
---

# Bento Grid

`MokaBentoGrid` and `MokaBentoItem` create an asymmetric grid layout inspired by bento box designs. Items can span multiple rows and columns, making it ideal for feature showcases, dashboards, and marketing layouts.

## MokaBentoGrid Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | `MokaBentoItem` children |
| `Columns` | `int` | `4` | Number of grid columns |
| `Gap` | `MokaSpacingScale` | `Md` | Gap between items |
| `GapValue` | `string?` | — | Custom gap value (overrides `Gap`) |
| `RowHeight` | `string` | `"auto"` | Grid row height |
| `MinRowHeight` | `string` | `"120px"` | Minimum row height |

## MokaBentoItem Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `ChildContent` | `RenderFragment?` | — | Item content |
| `ColSpan` | `int` | `1` | Number of columns to span |
| `RowSpan` | `int` | `1` | Number of rows to span |
| `ColStart` | `int?` | — | Explicit column start position |
| `RowStart` | `int?` | — | Explicit row start position |
| `Title` | `string?` | — | Item title |
| `Description` | `string?` | — | Item description text |
| `Clickable` | `bool` | `false` | Hover effect and pointer cursor |
| `OnClick` | `EventCallback` | — | Click callback |

## Basic Bento Layout

```blazor-preview
<MokaBentoGrid Columns="3">
    <MokaBentoItem ColSpan="2" Title="Feature One" Description="Spans two columns">
        Main feature content here.
    </MokaBentoItem>
    <MokaBentoItem Title="Metric">
        <MokaStat Title="Users" Value="1,234" />
    </MokaBentoItem>
    <MokaBentoItem Title="Item A">Detail A</MokaBentoItem>
    <MokaBentoItem Title="Item B">Detail B</MokaBentoItem>
    <MokaBentoItem Title="Item C">Detail C</MokaBentoItem>
</MokaBentoGrid>
```

## Multi-Row Spanning

```blazor-preview
<MokaBentoGrid Columns="4" MinRowHeight="100px">
    <MokaBentoItem ColSpan="2" RowSpan="2" Title="Hero Block">
        This item spans 2 columns and 2 rows.
    </MokaBentoItem>
    <MokaBentoItem Title="Top Right 1">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Top Right 2">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Bottom Right 1">Small cell</MokaBentoItem>
    <MokaBentoItem Title="Bottom Right 2">Small cell</MokaBentoItem>
</MokaBentoGrid>
```

## Clickable Items

```blazor-preview
@code { string _clicked = "None"; }
<MokaText>Last clicked: @_clicked</MokaText>
<MokaBentoGrid Columns="3" Gap="MokaSpacingScale.Sm">
    <MokaBentoItem Title="Analytics" Clickable OnClick="@(() => _clicked = "Analytics")">Click me</MokaBentoItem>
    <MokaBentoItem Title="Reports" Clickable OnClick="@(() => _clicked = "Reports")">Click me</MokaBentoItem>
    <MokaBentoItem Title="Settings" Clickable OnClick="@(() => _clicked = "Settings")">Click me</MokaBentoItem>
</MokaBentoGrid>
```
