---
title: PIN Input
description: Numeric PIN entry field with masked display and auto-advance.
order: 44
---

# PIN Input

`MokaPinInput` renders a row of individual digit boxes for numeric PIN or code entry. Each box auto-advances on input and supports backspace navigation. Values can be masked (shown as dots) for security.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string` | `""` | Current PIN value (bindable via `@bind-Value`) |
| `ValueChanged` | `EventCallback<string>` | -- | Fires when the value changes |
| `Length` | `int` | `4` | Number of digit boxes |
| `Masked` | `bool` | `true` | Displays dots instead of digits |
| `Disabled` | `bool` | `false` | Disables all inputs |
| `Size` | `MokaSize` | `Md` | Input size: `Sm`, `Md`, `Lg` |
| `OnComplete` | `EventCallback<string>` | -- | Fires when all digits are entered |
| `Class` | `string?` | -- | Additional CSS classes |
| `Style` | `string?` | -- | Additional inline styles |

## Basic 4-Digit PIN

```blazor-preview
<MokaPinInput @bind-Value="@_pin" />

@code {
    private string _pin = "";
}
```

## 6-Digit Code

```blazor-preview
<MokaPinInput @bind-Value="@_pin6" Length="6" />

@code {
    private string _pin6 = "";
}
```

## Unmasked

```blazor-preview
<MokaPinInput @bind-Value="@_pinVisible" Masked="false" />

@code {
    private string _pinVisible = "";
}
```

## Disabled

```blazor-preview
<MokaPinInput Value="12" Length="4" Disabled="true" />
```

## With Completion Callback

```blazor-preview
<MokaPinInput @bind-Value="@_pinCb" OnComplete="HandleComplete" />
<MokaText Size="MokaSize.Sm" Style="margin-top:var(--moka-spacing-sm)">@_pinStatus</MokaText>

@code {
    private string _pinCb = "";
    private string _pinStatus = "Enter all 4 digits...";

    private void HandleComplete(string pin)
    {
        _pinStatus = $"PIN entered: {pin}";
    }
}
```
