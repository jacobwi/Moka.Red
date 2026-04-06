---
title: MAC Address Input
description: Segmented input for entering MAC hardware addresses with automatic formatting.
order: 76
---

# MAC Address Input

`MokaMacAddressInput` provides a segmented text entry for MAC (Media Access Control) hardware addresses. Each octet gets its own field with automatic focus-advance, paste support, and configurable separator style.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | Current MAC address value (two-way bindable) |
| `ValueChanged` | `EventCallback<string?>` | -- | Callback when value changes |
| `Label` | `string?` | -- | Field label |
| `HelperText` | `string?` | -- | Helper text displayed below the input |
| `Separator` | `char` | `':'` | Separator character between octets (`:` or `-`) |
| `Disabled` | `bool` | `false` | Disables the input |
| `Size` | `MokaSize` | `Md` | Input size: `Sm`, `Md`, `Lg` |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic

```blazor-preview
<MokaMacAddressInput Value="00:1A:2B:3C:4D:5E" />
```

## With Label

```blazor-preview
<MokaMacAddressInput Value="AA:BB:CC:DD:EE:FF" Label="Device MAC" HelperText="Enter the hardware address" />
```

## Dash Separator

```blazor-preview
<MokaMacAddressInput Value="00-1A-2B-3C-4D-5E" Separator='-' Label="MAC (dashes)" />
```
