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
| `ValueChanged` | `EventCallback<string?>` | -- | Fires when the value changes |
| `Label` | `string?` | -- | Field label. It names the group for screen readers |
| `HelperText` | `string?` | -- | Hint text below the field |
| `ErrorText` | `string?` | -- | Error text below the field. Wins over a validation message |
| `Required` | `bool` | `false` | Shows a required marker and sets `aria-required` on every segment |
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

## Validation

Inside an `EditForm`, `@bind-Value` connects the address to the form's validation. A DataAnnotations message shows below the segments, and the root element gets the framework's `modified`, `valid` and `invalid` classes. `ErrorText` still wins over a validation message. A partly filled IPv4 address arrives as `"10.0.."`, so a pattern can catch empty octets.

```razor
<EditForm Model="_server" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaIpAddressInput @bind-Value="_server.Address" Label="Server IP" Required />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>

@code {
    private readonly Server _server = new();

    private void Save() { }

    private sealed class Server
    {
        [Required(ErrorMessage = "Enter the server address")]
        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$", ErrorMessage = "Fill in all four octets")]
        public string? Address { get; set; }
    }
}
```

## Accessibility

The segments sit in a `role="group"` that `Label` names, and each segment has its own label, such as "Octet 1 of 4" (or "Group 1 of 8" for IPv6). Every segment carries `aria-invalid`, `aria-required` and an `aria-describedby` that points at the helper or error text while one is shown.

## Spacing and Attributes

`Id`, `Class`, `Style` and unmatched attributes go on the `role="group"` element, and the label and message ids are built from `Id` (`{Id}-label`, `{Id}-message`). `Margin` and `Padding` go on the group too, around the label, the segments and the message. `Rounded` goes on every segment, since each segment draws its own border. Up to 0.1.12 the IP input ignored all three.
