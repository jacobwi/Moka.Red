---
title: IP Address Input
description: Segmented input for entering IPv4 and IPv6 addresses.
order: 72
---

# IP Address Input

`MokaIpAddressInput` provides a segmented text input for entering IP addresses. Each octet (or hextet for IPv6) gets its own field with automatic focus advancement, making IP entry fast and error-free.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | Current IP address value (bindable via `@bind-Value`) |
| `Label` | `string?` | -- | Field label |
| `HelperText` | `string?` | -- | Hint text below the field |
| `Disabled` | `bool` | `false` | Disables all segments |
| `Size` | `MokaSize` | `Md` | Input size: `Sm`, `Md`, `Lg` |
| `AllowIPv6` | `bool` | `false` | When true, renders 8 hextet segments for IPv6 entry |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic IPv4

```blazor-preview
<MokaIpAddressInput @bind-Value="_ip" />

@code {
    private string? _ip = "192.168.1.1";
}
```

## With Label

```blazor-preview
<MokaIpAddressInput @bind-Value="_ip" Label="Server IP" HelperText="Enter the target server address" />

@code {
    private string? _ip;
}
```

## Disabled

```blazor-preview
<MokaIpAddressInput Value="10.0.0.1" Label="Gateway" Disabled="true" />
```
