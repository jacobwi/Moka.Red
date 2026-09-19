---
title: Forms
description: Overview of all form input components - text, numeric, date/time, color, file, signature, and more.
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
| `MokaRangeSlider` | two `double` | Start and end thumbs |
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
| `MokaCreditCardInput` | four `string` | Number, expiry, code, name |
| `MokaSignaturePad` | `string` | Canvas draw → base64 PNG |

## Id, Class and Attributes

`Id` names the control itself, the element that takes focus: the input, the select's trigger or the rating's slider. The field's label points at it with `for`. The select's trigger and the rating's slider are not native controls, which `for` may not point at, so they point `aria-labelledby` at the label instead. The tree select's button does the same and adds its current choice after the label, since a button's name replaces its text. The ids built from `Id` follow it: the label is `{Id}-label`, and the option list of `MokaSelect` and `MokaAutoComplete` is `{Id}-listbox`, with options `{Id}-listbox-option-0` and on. Without `Id`, an input whose label points at it makes up an id of its own, so the label still names the control.

`Class` and `Style` go on the component's own element, between the label and the helper text. Inside an `EditForm` that element also carries the framework's `modified`, `valid` and `invalid` classes, before your `Class`, so a form's CSS can mark the field. Every other attribute goes on the control, so `aria-describedby`, `name`, `autocomplete` or a `data-` attribute ends up where screen readers, scripts and tests look for it. The inputs that take part in `EditForm` validation also put `aria-invalid="true"` on the control while the field fails it.

```razor
<MokaTextField Id="email" Label="Email" aria-describedby="email-note" autocomplete="email" />
<p id="email-note">We never share it.</p>
```

Where a component differs:

| Component | `Id` and other attributes | `Class` and `Style` |
|-----------|---------------------------|---------------------|
| `MokaCheckbox`, `MokaSwitch` | The checkbox input, or the drawn box of a `DisplayOnly` checkbox | The label row |
| `MokaRadioGroup` | The `role="radiogroup"` element | The same element |
| `MokaRangeSlider` | The element that holds both thumbs | The same element |
| `MokaFileUpload` | The file input, so `capture`, `name` or `aria-describedby` work there | The field's outer element |
| `MokaOtpInput` and the other box inputs | The `role="group"` element. The label and message ids are built from `Id` | The same element |
| `MokaCreditCardInput`, `MokaSignaturePad` | The outer element | The same element |

`MokaSelect` and `MokaRating` take their name from `aria-label` when there is no `Label`, and render it once, on the control. With a `Label`, the label names the control and `aria-label` is left out.

Up to 0.1.12 the inputs inside a field and `MokaSearchInput` ignored `Id`, and the labels of `MokaSlider` and `MokaTagInput` were not tied to their inputs. `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaFileUpload`, `MokaPhoneInput` and `MokaCurrencyInput` ignored `Class`. `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaRadioGroup`, `MokaFileUpload`, `MokaSelect` and `MokaRating` dropped the other attributes, and with them `aria-invalid`. `MokaDateRangePicker` and `MokaTreeSelect` put `Id` and the attributes on their outer element. `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaPhoneInput`, `MokaCurrencyInput`, `MokaCheckbox`, `MokaSwitch` and `MokaRadioGroup` left out the field classes. The label of `MokaSelect` pointed `for` at its trigger, a div, and the label of `MokaTreeSelect` was tied to nothing.

## Spacing

`Margin`, `Padding` and `Rounded`, and their `MarginValue`, `PaddingValue` and `RoundedValue` forms, follow one rule. The margin goes on the outermost element, around the label and the helper text. The padding and the radius go on the element that draws the field's border: the input, the select's trigger or the tag input's box. Where no single element draws one, they go here:

| Component | Padding | Radius |
|-----------|---------|--------|
| `MokaCheckbox`, `MokaSwitch` | The label row | The box, the track |
| `MokaSlider`, `MokaRangeSlider` | The slider row | The track |
| `MokaRating` | The row of stars | The row of stars |
| `MokaRadioGroup` | The group | The group |
| `MokaFileUpload` | The drop zone | The drop zone |
| `MokaSignaturePad` | The dashed frame around the canvas | The frame |
| `MokaCreditCardInput` | The outer element | Each of the four fields |
| `MokaOtpInput`, `MokaPinInput`, `MokaIpAddressInput`, `MokaMacAddressInput` | The outer element | Each box |

The padding replaces the input's own, which also keeps text clear of an icon, the phone's country code or the currency symbol. Leave room for them:

```razor
<MokaCurrencyInput @bind-Value="_amount" Label="Amount" PaddingValue="8px 10px 8px 24px" />
```

Up to 0.1.12 the inputs inside a field ignored all three, and so did `MokaSearchInput`, `MokaCreditCardInput`, `MokaOtpInput` and `MokaIpAddressInput`. `MokaKnob`, `MokaSignaturePad`, `MokaPinInput` and `MokaMacAddressInput` ignored `Rounded`, and the signature pad's padding went around the whole pad instead of inside its frame.

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

The field reads and writes numbers in the invariant culture, with a dot before the decimals, whatever the page's culture. Up to 0.1.12 it wrote them in the page's culture and read them back in the invariant one, so a German page showed 1.5 as "1,5" and turned an edit of it into 15.

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
| `MaxRows` | `int?` | - | Limits auto-resize height |

## Checkbox

```blazor-preview
@code { bool _agree; }
<MokaCheckbox @bind-Value="_agree" Label="I agree to the terms" />
```

### Display Only

`DisplayOnly` draws the box and label without an input: nothing to focus, nothing to click, and hidden from assistive technology. Use it inside an element that owns the checked state, such as a list option with `aria-selected` or a menu item with `aria-checked`, where a real checkbox would be a second control and a second click target.

```razor
<div role="option" aria-selected="@_selected" @onclick="Toggle">
    <MokaCheckbox Value="_selected" DisplayOnly Label="Include archived" />
</div>
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

`Label` names the range input, so screen readers read "Volume" with the value. `Id` goes on the range input, `Padding` on the row around it and `Rounded` on the track.

## RangeSlider

```blazor-preview
@code { double _low = 20; double _high = 80; }
<MokaRangeSlider @bind-ValueStart="_low" @bind-ValueEnd="_high" Label="Price" />
```

- Each thumb is a native range input with its own tab stop, so the keyboard works on it as on any slider, and the focused thumb shows the focus ring.
- `Label` names the pair, and each thumb is named after it: "Price start" and "Price end". Without a label, pass `aria-label`, or the thumbs are "Range start" and "Range end".
- A thumb stops at the other one, and snaps back when dragged past it.
- With both thumbs at `Max`, the start thumb is on top, so a drag pulls the pair apart.
- `Id` and unmatched attributes go on the element that holds both thumbs.
- `Margin` goes on the field, `Padding` on the element that holds both thumbs, and `Rounded` on the track.

## Rating

```blazor-preview
@code { int _stars = 3; }
<MokaRating @bind-Value="_stars" MaxValue="5" Label="Rating" />
```

The rating is one control, a WAI-ARIA slider, rather than a row of separate stars. It takes one tab stop, screen readers read its value as "3 of 5" (or "No rating" at 0), and it takes its name from `Label`. Without a label, pass `aria-label`:

```razor
<MokaRating @bind-Value="_stars" aria-label="Rate this answer" />
```

| Key | Action |
|-----|--------|
| Right / Up | One star more |
| Left / Down | One star less |
| Home | No rating, or one star when `AllowClear` is false |
| End | All stars (`MaxValue`) |

- The keys don't scroll the page while the rating can change. With Ctrl, Alt or Cmd held they don't change the rating.
- `ReadOnly` keeps the tab stop, so the value can still be read, and reports `aria-readonly="true"`. Neither clicks nor keys change it.
- `Disabled` takes the rating out of the tab order and reports `aria-disabled="true"`.
- With `AllowClear="false"`, neither a click nor the keys go back to 0 once there is a rating.
- A click or a key changes the rating even when `Value` isn't bound, and a re-render of the parent doesn't undo it. The rating follows `Value` again when the parent passes a different one. Use `@bind-Value` to keep both in step.
- `Id` and other attributes go on the slider. `aria-label` names it only when there is no `Label`, and the slider carries it once.

Up to 0.1.12 each star was a separate `role="button"` (or `role="img"` when read-only) that the keyboard could not reach, and the stars were named by their position rather than the value.

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

- A colour reaches the preview, the presets and the recent colours only as a hex colour (`#rgb`, `#rgba`, `#rrggbb` or `#rrggbbaa`). Anything else shows no colour, so a value can never add CSS of its own.
- The gradient, the sliders and their thumbs are placed with invariant numbers. Up to 0.1.12 they sat at the left edge in locales that write decimals with a comma.

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

`Id` and unmatched attributes go on the file input, which is what the label and the "Browse" link open, so file input attributes such as `capture` work:

```razor
<MokaFileUpload Id="receipt" Label="Receipt photo" Accept="image/*" capture="environment" />
```

`Class`, `Style` and `Margin` go on the field's outer element, since the drop zone, the file list and the errors sit side by side inside it. `Padding` and `Rounded` go on the drop zone.

`Disabled` disables the file input and the remove buttons as well, so neither the keyboard nor the mouse can open the file dialog or remove a file. Up to 0.1.12 the keyboard could still do both.

The file input covers the drop zone without showing, so a dropped file lands on it and arrives the same way as a picked one, and a click anywhere on the zone opens the file dialog. Keyboard focus on the input shows as a ring around the zone. With `DragDrop="false"` the zone takes no drops and does not light up under one.

A drop skips the file dialog, so every file, dropped or picked, is checked in code:

- `Accept` takes extensions (`.pdf`), MIME types (`application/pdf`) and wildcards (`image/*`), compared without case. As in the browser, any other token is ignored, and an `Accept` with no valid token takes every file. A file of another type is listed as an error.
- `MaxFileSize` applies to each file.
- Without `Multiple` the list holds one file. A new file replaces it, and a drop of several keeps the first.
- With `Multiple`, `MaxFiles` caps the whole list across picks and drops. The files past it are left out, with one error line.

Each remove button is named after its file, such as "Remove report.pdf".

Up to 0.1.12 the drop zone cancelled every drop, so a dropped file did nothing, and the native file input showed inside the zone because its hiding rule never reached it. `Accept` only filtered the dialog, a pick of more files than `MaxFiles` threw and was lost, separate picks could grow the list past `MaxFiles`, and the remove buttons had no accessible name.

## TagInput

```blazor-preview
@code { IList<string> _tags = new List<string> { "blazor", "dotnet" }; }
<MokaTagInput @bind-Values="_tags" Label="Tags" Placeholder="Add tag and press Enter" />
```

The input never edits the list you pass in. Every change arrives through `ValuesChanged` as a new list, so a list you share elsewhere, or a read-only one, is safe.

`Label` names the text box, and `Id` goes on it. Once `MaxTags` is reached the text box goes away, and the label is plain text until a tag is removed.

## RadioGroup

```blazor-preview
@code { string _plan = "pro"; }
<MokaRadioGroup @bind-Value="_plan" Label="Plan">
    <MokaRadioItem Value="@("free")">Free</MokaRadioItem>
    <MokaRadioItem Value="@("pro")">Pro</MokaRadioItem>
    <MokaRadioItem Value="@("enterprise")">Enterprise</MokaRadioItem>
</MokaRadioGroup>
```

Each item is a native radio input inside its label, and the items of one group share a generated `name`. The browser handles the keyboard: Tab reaches the group once, and the arrow keys move the selection. Screen readers get the checked state from the input itself.

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

The arrow keys move the highlight through the suggestions, and screen readers announce each one. Enter picks the highlighted suggestion without submitting a surrounding form. With nothing highlighted, Enter submits as usual.

## OtpInput

```blazor-preview
@code { string _otp = ""; }
<MokaOtpInput @bind-Value="_otp" Length="6" Label="Verification code" />
```

`MokaOtpInput`, `MokaPinInput`, `MokaIpAddressInput` and `MokaMacAddressInput` take part in `EditForm` validation like the other inputs. With `@bind-Value`, a DataAnnotations message shows below the boxes, and every box gets `aria-invalid` and an `aria-describedby` pointing at that message. `Required` adds the marker after the label and `aria-required` on every box. The boxes sit in a `role="group"` that `Label` names.

```razor
<EditForm Model="_login" OnValidSubmit="Verify">
    <DataAnnotationsValidator />
    <MokaOtpInput @bind-Value="_login.Code" Label="Verification code" Required />
    <MokaButton Type="submit">Verify</MokaButton>
</EditForm>
```

`Id` and unmatched attributes go on the group, and the label and message ids are built from `Id`. `Margin` and `Padding` go on the group too, around the label, the boxes and the message. `Rounded` goes on every box, so `RoundedValue="50%"` gives round boxes.

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

With `Min` or `Max`, an amount outside the range is moved to the nearest end when the field loses focus. The input reports the moved amount, once, through `ValueChanged` and the `EditContext`, so a bound value and a form model hold what the field shows. Up to 0.1.12 the parent got the amount as typed, and its next render put it back in the field.

## CreditCardInput

```blazor-preview
@code { string _number = ""; string _expiry = ""; string _cvv = ""; string _name = ""; }
<MokaCreditCardInput @bind-CardNumber="_number" @bind-ExpiryDate="_expiry"
                     @bind-Cvv="_cvv" @bind-CardholderName="_name" />
```

- The number is grouped as it is typed (4-6-5 for Amex, fours otherwise) and the brand is detected from its first digits. Characters that don't belong in a box are removed from it as they are typed.
- Each box has its own name ("Card number", "Expiry date (MM/YY)", "Security code (CVV)", "Cardholder name"), and `Label` names the group.
- The security code takes four digits for Amex and three for other brands. Moving from Amex to another brand shortens it and reports the shorter code through `CvvChanged`.
- `Id` and unmatched attributes go on the outer element, and so do `Margin` and `Padding`. `Rounded` goes on each of the four fields.

## SignaturePad

```blazor-preview
@code { string? _sig; }
<MokaSignaturePad @bind-Value="_sig" Label="Signature" Height="120px" />
@if (_sig is not null)
{
    <img src="@_sig" style="border:1px solid #ccc;max-width:300px" />
}
```

A `Value` from the parent is drawn on the canvas, scaled to fit and centred, and a null or empty `Value` clears it. That works on a `ReadOnly` pad too, which is how to show a stored signature:

```razor
<MokaSignaturePad Value="@_storedSignature" ReadOnly Label="Signed" />
```

- A value the pad reported itself is not drawn again when a bound parent passes it back, so the strokes stay and Undo keeps working.
- Drawing over a stored signature adds to it. Undo removes the new strokes one at a time and leaves the stored signature; undoing the last one reports the stored value again. Clear removes both.
- `Value` is meant to be a data URL, which is what the pad produces. A URL from another origin loads only if that server allows CORS, since the canvas could not be exported otherwise.
- `Margin` goes on the outer element, `Padding` and `Rounded` on the dashed frame around the canvas.

Up to 0.1.12 a `Value` from the parent was never drawn, and setting it to null or empty left the old drawing on the canvas.

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
