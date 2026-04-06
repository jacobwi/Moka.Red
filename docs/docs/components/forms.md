---
title: Forms
description: Overview of all form input components — text, numeric, date/time, color, file, signature, and more.
order: 8
---

# Forms

All form inputs in `Moka.Red.Forms` share a consistent API: `Label`, `HelperText`, `ErrorText`, `Required`, `Disabled`, `Placeholder`, `@bind-Value`, and inherited `Size`, `Color`, `Variant` from `MokaVisualInputBase<TValue>`.

## Quick Reference

| Component | Input Type | Notes |
|-----------|-----------|-------|
| `MokaTextField` | `string` | Single-line text |
| `MokaPasswordField` | `string` | Toggle visibility |
| `MokaNumericField<T>` | numeric | Min, Max, Step |
| `MokaTextArea` | `string` | Multi-line, auto-resize |
| `MokaCheckbox` | `bool` | Indeterminate support |
| `MokaSwitch` | `bool` | Toggle on/off |
| `MokaSlider` | numeric | Range, ticks, marks |
| `MokaRating` | numeric | Star rating |
| `MokaDatePicker` | `DateOnly` / `DateTime` | Calendar popup |
| `MokaTimePicker` | `TimeOnly` | Clock popup |
| `MokaColorPicker` | `string` | Hue/saturation/lightness + hex |
| `MokaFileUpload` | `IBrowserFile` | Single/multi, drag-drop |
| `MokaTagInput` | `IList<string>` | Free-text tags with Enter |
| `MokaRadioGroup` | `TValue` | Radio button group |
| `MokaAutoComplete<T>` | `T` | Async/sync suggestions |
| `MokaSearchInput` | `string` | Debounced search field |
| `MokaOtpInput` | `string` | One-time password |
| `MokaPhoneInput` | `string` | Country code + number |
| `MokaCurrencyInput` | `decimal` | Locale-aware currency |
| `MokaSignaturePad` | `string` | Canvas draw → base64 PNG |

## TextField

```blazor-preview
@code { string _name = ""; }
<MokaTextField @bind-Value="_name" Label="Full name" Placeholder="Enter your name"
               HelperText="As it appears on your ID" Required />
```

## PasswordField

```blazor-preview
@code { string _pwd = ""; }
<MokaPasswordField @bind-Value="_pwd" Label="Password" HelperText="Minimum 8 characters" />
```

## NumericField

```blazor-preview
@code { int _qty = 1; }
<MokaNumericField TValue="int" @bind-Value="_qty" Label="Quantity" Min="1" Max="100" Step="1" />
```

## TextArea

```blazor-preview
@code { string _bio = ""; }
<MokaTextArea @bind-Value="_bio" Label="Bio" Rows="4" AutoResize Placeholder="Tell us about yourself…" />
```

### MokaTextArea Parameters (extras)

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Rows` | `int` | `3` | Initial visible rows |
| `AutoResize` | `bool` | `false` | Grows with content |
| `MaxRows` | `int?` | — | Limits auto-resize height |

## Checkbox

```blazor-preview
@code { bool _agree; }
<MokaCheckbox @bind-Value="_agree" Label="I agree to the terms" />
```

## Switch

```blazor-preview
@code { bool _notify = true; }
<MokaSwitch @bind-Value="_notify" Label="Email notifications" />
```

## Slider

```blazor-preview
@code { double _vol = 50; }
<MokaSlider @bind-Value="_vol" Label="Volume" Min="0" Max="100" Step="5" ShowTicks />
```

## Rating

```blazor-preview
@code { int _stars = 3; }
<MokaRating @bind-Value="_stars" Max="5" />
```

## DatePicker

```blazor-preview
@code { DateTime? _date = DateTime.Today; }
<MokaDatePicker @bind-Value="_date" Label="Appointment date" />
```

## TimePicker

```blazor-preview
@code { TimeSpan? _time = DateTime.Now.TimeOfDay; }
<MokaTimePicker @bind-Value="_time" Label="Meeting time" />
```

## ColorPicker

```blazor-preview
@code { string _color = "#d32f2f"; }
<MokaColorPicker @bind-Value="_color" Label="Brand color" />
```

## FileUpload

```blazor-preview
@code {
    IBrowserFile? _file;
    void OnFilesChange(IReadOnlyList<IBrowserFile> files) => _file = files.FirstOrDefault();
}
<MokaFileUpload Label="Upload document" OnFilesSelected="OnFilesChange" Accept=".pdf,.docx" />
@if (_file is not null)
{
    <MokaCaption>@_file.Name (@(_file.Size / 1024) KB)</MokaCaption>
}
```

## TagInput

```blazor-preview
@code { IList<string> _tags = new List<string> { "blazor", "dotnet" }; }
<MokaTagInput @bind-Values="_tags" Label="Tags" Placeholder="Add tag and press Enter" />
```

## RadioGroup

```blazor-preview
@code { string _plan = "pro"; }
<MokaRadioGroup @bind-Value="_plan" Label="Plan">
    <MokaRadio Value="free">Free</MokaRadio>
    <MokaRadio Value="pro">Pro</MokaRadio>
    <MokaRadio Value="enterprise">Enterprise</MokaRadio>
</MokaRadioGroup>
```

## AutoComplete

```blazor-preview
@code {
    string? _country;
    string[] _all = ["Germany", "France", "Italy", "Spain", "Poland"];
    Task<IEnumerable<string>> Search(string q)
        => Task.FromResult(_all.Where(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)));
}
<MokaAutoComplete @bind-Value="_country" Label="Country" SearchFunc="Search" />
```

## OtpInput

```blazor-preview
@code { string _otp = ""; }
<MokaOtpInput @bind-Value="_otp" Length="6" Label="Verification code" />
```

## PhoneInput

```blazor-preview
@code { string _phone = ""; }
<MokaPhoneInput @bind-Value="_phone" Label="Phone number" CountryCode="+1" />
```

## CurrencyInput

```blazor-preview
@code { decimal? _amount; }
<MokaCurrencyInput @bind-Value="_amount" Label="Invoice amount" CurrencyCode="USD" />
```

## SignaturePad

```blazor-preview
@code { string? _sig; }
<MokaSignaturePad @bind-Value="_sig" Label="Signature" Height="120px" />
@if (_sig is not null)
{
    <img src="@_sig" style="border:1px solid #ccc;max-width:300px" />
}
```

## Error State

All inputs support `ErrorText` to render an error message below the field.

```blazor-preview
<MokaTextField Label="Email" Value="not-an-email"
               ErrorText="Please enter a valid email address." />
```

## Disabled State

```blazor-preview
<div style="display:flex;flex-direction:column;gap:8px">
    <MokaTextField Label="Read-only field" Value="Cannot edit this" Disabled />
    <MokaSwitch Label="Disabled toggle" Value="true" Disabled />
</div>
```
