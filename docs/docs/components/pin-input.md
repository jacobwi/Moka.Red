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
| `Value` | `string?` | -- | Current PIN value (bindable via `@bind-Value`) |
| `ValueChanged` | `EventCallback<string?>` | -- | Fires when the value changes |
| `Length` | `int` | `4` | Number of digit boxes |
| `Masked` | `bool` | `true` | Displays dots instead of digits |
| `Label` | `string?` | -- | Label above the boxes. It names the group for screen readers |
| `HelperText` | `string?` | -- | Hint text below the boxes |
| `ErrorText` | `string?` | -- | Error text below the boxes. Wins over a validation message |
| `Required` | `bool` | `false` | Shows a required marker and sets `aria-required` on every box |
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

## Validation

Inside an `EditForm`, `@bind-Value` connects the PIN to the form's validation. A DataAnnotations message shows below the boxes, and the root element gets the framework's `modified`, `valid` and `invalid` classes. `ErrorText` still wins over a validation message. With a plain `Value` and no binding, the input stays out of validation.

```razor
<EditForm Model="_account" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaPinInput @bind-Value="_account.Pin" Label="PIN" Required />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>

@code {
    private readonly Account _account = new();

    private void Save() { }

    private sealed class Account
    {
        [Required(ErrorMessage = "Enter your PIN")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "The PIN has 4 digits")]
        public string? Pin { get; set; }
    }
}
```

## Accessibility

The boxes sit in a `role="group"` that `Label` names, and each box has its own label, such as "PIN digit 1 of 4". Every box carries `aria-invalid`, `aria-required` and an `aria-describedby` that points at the helper or error text while one is shown.

## Spacing and Attributes

`Id`, `Class`, `Style` and unmatched attributes go on the `role="group"` element, and the label and message ids are built from `Id` (`{Id}-label`, `{Id}-message`). `Margin` and `Padding` go on the group too, around the label, the boxes and the message. `Rounded` goes on every box, since each box draws its own border. Up to 0.1.12 the PIN input ignored `Rounded`.
