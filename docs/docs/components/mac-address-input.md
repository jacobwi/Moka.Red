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
| `Label` | `string?` | -- | Field label. It names the group for screen readers |
| `HelperText` | `string?` | -- | Helper text displayed below the input |
| `ErrorText` | `string?` | -- | Error text below the input. Wins over a validation message |
| `Required` | `bool` | `false` | Shows a required marker and sets `aria-required` on every octet |
| `MacSeparator` | `string` | `":"` | Separator between octets, `":"` or `"-"` |
| `Uppercase` | `bool` | `true` | Uppercases hex digits as they are typed |
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
<MokaMacAddressInput Value="00-1A-2B-3C-4D-5E" MacSeparator="-" Label="MAC (dashes)" />
```

## Validation

Inside an `EditForm`, `@bind-Value` connects the address to the form's validation. A DataAnnotations message shows below the octets, and the root element gets the framework's `modified`, `valid` and `invalid` classes. `ErrorText` still wins over a validation message. A partly filled address arrives with empty octets, such as `"AA:BB::::"`.

```razor
<EditForm Model="_device" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaMacAddressInput @bind-Value="_device.Mac" Label="Device MAC" Required />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>

@code {
    private readonly Device _device = new();

    private void Save() { }

    private sealed class Device
    {
        [Required(ErrorMessage = "Enter the hardware address")]
        [RegularExpression("^([0-9A-F]{2}:){5}[0-9A-F]{2}$", ErrorMessage = "Fill in all six octets")]
        public string? Mac { get; set; }
    }
}
```

## Accessibility

The octets sit in a `role="group"` that `Label` names, and each octet has its own label, such as "MAC octet 1 of 6". Every octet carries `aria-invalid`, `aria-required` and an `aria-describedby` that points at the helper or error text while one is shown.

## Spacing and Attributes

`Id`, `Class`, `Style` and unmatched attributes go on the `role="group"` element, and the label and message ids are built from `Id` (`{Id}-label`, `{Id}-message`). `Margin` and `Padding` go on the group too, around the label, the octets and the message. `Rounded` goes on every octet, since each octet draws its own border. Up to 0.1.12 the MAC input ignored `Rounded`.
