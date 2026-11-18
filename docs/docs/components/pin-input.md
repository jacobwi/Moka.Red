---
title: PIN Input
description: Numeric PIN entry, one masked field drawn as a box per digit.
order: 44
---

# PIN Input

`MokaPinInput` takes a numeric PIN, such as a card PIN or a backup code, in one password field drawn as a row of boxes, one per digit. It is masked by default and marked `autocomplete="off"` so the browser doesn't keep it. For a code the person received, such as a sign-in code, use [`MokaOtpInput`](forms.md#otpinput), which the browser can fill.

The digits are the field's own text, set in the monospace font and spaced to land in their boxes, so typing, Backspace, the arrow keys, selecting and pasting work as in any text field and show at once. The value keeps the digits 0 to 9 only, up to `Length`: a PIN pasted as "12 34" arrives as "1234", and full width digits count as the digits they show. Up to 0.8.1 each digit was an input of its own, and a pasted PIN filled one box.

## Parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Value` | `string?` | -- | Current PIN value (bindable via `@bind-Value`) |
| `ValueChanged` | `EventCallback<string?>` | -- | Fires when the value changes |
| `Length` | `int` | `4` | Number of digits, one box each. Below 1 counts as 1 |
| `Masked` | `bool` | `true` | Shows a dot for each digit and makes the field a password field |
| `Label` | `string?` | -- | Label above the boxes. It names the field; without one the field is named "PIN", unless you pass `aria-label` |
| `HelperText` | `string?` | -- | Hint text below the boxes |
| `ErrorText` | `string?` | -- | Error text below the boxes. Wins over a validation message |
| `Required` | `bool` | `false` | Shows a required marker and sets `aria-required` on the field |
| `Disabled` | `bool` | `false` | Disables the field |
| `Size` | `MokaSize` | `Md` | Box size: `Xs`, `Sm`, `Md`, `Lg` |
| `OnComplete` | `EventCallback<string>` | -- | Fires when a change fills every box, with the PIN |
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

A screen reader hears one password field, named by `Label` through the label's `for`. The field carries `aria-invalid`, `aria-required` and an `aria-describedby` that points at the helper or error text while one is shown, after any `aria-describedby` you pass. The boxes are drawing only. A script marks the box at the caret, or every box a selection covers; without it, every box lights while the field has focus.

## Static Pages

The field is a real input, so on a statically rendered page the PIN can be typed and, bound to a field with `@bind-Value`, is posted under that field's name. No script tidies the text there, so the server gets it as typed.

## Spacing and Attributes

`Id` and unmatched attributes go on the field, and the message id is built from it (`{Id}-message`). `Class`, `Style`, `Margin` and `Padding` go on the outer element, around the label, the boxes and the message. `Rounded` goes on every box, since each box draws its own border. Up to 0.8.1 `Id` and unmatched attributes went on the outer element, a `role="group"` around an input per digit.
