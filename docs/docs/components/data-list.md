---
title: Data List
description: Definition/description list for displaying labeled key-value data.
order: 42
---

# Data List

`MokaDataList` renders a structured definition list for displaying labeled data pairs. It supports vertical and horizontal orientations, striped rows, borders, dense spacing, and rich content via render fragments.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Items` | `IReadOnlyList<MokaDataListItem>?` | -- | Simple items rendered automatically. Use `ChildContent` instead for rich content. |
| `ChildContent` | `RenderFragment?` | -- | Slot for manually composing `MokaDataListItem` children |
| `Orientation` | `MokaDirection` | `Column` | `Column` for vertical stacking, `Row` for side-by-side layout |
| `Striped` | `bool` | `false` | Alternates row background colors |
| `Bordered` | `bool` | `false` | Adds borders around rows |
| `Dense` | `bool` | `false` | Reduces padding for compact display |
| `Size` | `MokaSize` | `Md` | Text size: `Sm`, `Md`, `Lg` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

### MokaDataListItem

| Name | Type | Description |
|------|------|-------------|
| `Label` | `string` | The label/key text |
| `Value` | `string?` | Plain text value |
| `ValueContent` | `RenderFragment?` | Rich content for the value cell (takes precedence over `Value`) |

## Basic Vertical List

```blazor-preview
<MokaDataList Items="profileItems" />

@code {
    IReadOnlyList<MokaDataListItem> profileItems = new[]
    {
        new MokaDataListItem("Name", "Jane Doe"),
        new MokaDataListItem("Email", "jane@example.com"),
        new MokaDataListItem("Role", "Administrator"),
        new MokaDataListItem("Joined", "March 2024")
    };
}
```

## Horizontal Layout

```blazor-preview
<MokaDataList Items="serverInfo" Orientation="MokaDirection.Row" />

@code {
    IReadOnlyList<MokaDataListItem> serverInfo = new[]
    {
        new MokaDataListItem("Status", "Running"),
        new MokaDataListItem("Uptime", "14 days"),
        new MokaDataListItem("Region", "US-East")
    };
}
```

## Striped

```blazor-preview
<MokaDataList Items="specItems" Striped="true" />

@code {
    IReadOnlyList<MokaDataListItem> specItems = new[]
    {
        new MokaDataListItem("CPU", "8 cores"),
        new MokaDataListItem("Memory", "32 GB"),
        new MokaDataListItem("Storage", "1 TB SSD"),
        new MokaDataListItem("Network", "10 Gbps")
    };
}
```

## Bordered

```blazor-preview
<MokaDataList Items="orderItems" Bordered="true" />

@code {
    IReadOnlyList<MokaDataListItem> orderItems = new[]
    {
        new MokaDataListItem("Order #", "ORD-2024-1234"),
        new MokaDataListItem("Status", "Shipped"),
        new MokaDataListItem("Total", "$149.99")
    };
}
```

## Dense

```blazor-preview
<MokaDataList Items="denseItems" Dense="true" Bordered="true" />

@code {
    IReadOnlyList<MokaDataListItem> denseItems = new[]
    {
        new MokaDataListItem("Version", "0.1.3"),
        new MokaDataListItem("Framework", ".NET 10"),
        new MokaDataListItem("License", "MIT"),
        new MokaDataListItem("Size", "42 KB")
    };
}
```

## Rich Content

Use `ChildContent` with `MokaDataListItem` components for custom value rendering.

```blazor-preview
<MokaDataList>
    <MokaDataListItem Label="User">
        <ValueContent>
            <MokaFlexbox Align="MokaAlign.Center" Gap="MokaSpacingScale.Xs">
                <MokaAvatar Initials="JD" Size="MokaSize.Sm" />
                <MokaText>Jane Doe</MokaText>
            </MokaFlexbox>
        </ValueContent>
    </MokaDataListItem>
    <MokaDataListItem Label="Status">
        <ValueContent>
            <MokaBadge Content="Active" Color="MokaColor.Success" />
        </ValueContent>
    </MokaDataListItem>
    <MokaDataListItem Label="Tags">
        <ValueContent>
            <MokaFlexbox Gap="MokaSpacingScale.Xxs">
                <MokaChip Text="Admin" Size="MokaSize.Sm" />
                <MokaChip Text="Editor" Size="MokaSize.Sm" />
            </MokaFlexbox>
        </ValueContent>
    </MokaDataListItem>
</MokaDataList>
```
