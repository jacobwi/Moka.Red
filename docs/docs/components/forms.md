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
| `MokaNativeSelect<T>` | `T` | A real select, see [Native Select](native-select.md) |
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
| `MokaRadioGroup` | `TValue` | Radio button group. `AriaLabel` names it apart from the visible `Label` |
| `MokaAutoComplete<T>` | `T` | Async/sync suggestions |
| `MokaSearchInput` | `string` | Debounced search field |
| `MokaInlineEdit` | `string` | Text that becomes a field, see [Inline Edit](inline-edit.md) |
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

## Validation summary

`MokaValidationSummary` lists every message the form's `EditContext` holds, above the fields. It renders
nothing while the form is clean, so an empty box never sits above a form, and the list is a `role="alert"`, so
it is read out when it fills.

```razor
<EditForm Model="_model" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <MokaValidationSummary />

    <MokaTextField @bind-Value="_model.Name" Label="Name" />
    <MokaButton Type="submit">Save</MokaButton>
</EditForm>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string?` | `"Please fix the following"` | Heading above the list. Null or empty renders the messages with no heading |

It lists messages rather than linking to fields: a control's id is generated per field wrapper, and a message
carries no way back to it. The messages for one field are still shown by that field, as they always were.

## Forms on static pages

A page with no render mode, such as a sign-in page that has to set a cookie, posts what the browser posts. Put the
fields in an `EditForm` with a `FormName`, bind them with `@bind-Value`, and read them through
`[SupplyParameterFromForm]`:

```razor
@page "/sign-in"
@attribute [ExcludeFromInteractiveRouting]

<EditForm Model="Login" FormName="sign-in" OnValidSubmit="SignIn">
    <DataAnnotationsValidator />

    <MokaTextField @bind-Value="Login!.Email" Label="Email" />
    <MokaPasswordField @bind-Value="Login!.Password" Label="Password" />
    <MokaCheckbox @bind-Value="Login!.Remember" Label="Keep me signed in" />

    <MokaButton Type="submit">Sign in</MokaButton>
</EditForm>

@code {
    [SupplyParameterFromForm]
    private LoginForm? Login { get; set; }

    protected override void OnInitialized() => Login ??= new();
}
```

Use `??=` in `OnInitialized` rather than an initializer on the property. When a post carries none of the form's
fields the binder sets the property to null, and an `EditForm` with no `Model` throws.

What a control can do on a static page depends on whether the browser can edit it alone:

| On a static page | Controls |
|---|---|
| Edited and posted | `MokaTextField`, `MokaPasswordField`, `MokaTextArea`, `MokaNumericField`, `MokaPhoneInput`, `MokaCurrencyInput`, `MokaColorInput`, `MokaCheckbox`, `MokaSwitch`, `MokaRadioGroup`, and `MokaNativeSelect` over strings, numbers, enums, dates or `Guid`s |
| Edited and posted, if you give it a `name` | `MokaSlider` and `MokaSearchInput`, which aren't tied to a form field |
| Shown and posted, but can't be changed | `MokaSelect`, `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaCascader`, `MokaInlineEdit`. Their popups need interactivity, so they keep the value the server set. `MokaSelect` with `Multiple` posts nothing |
| Not posted | Everything else, including `MokaRangeBar`, `MokaOtpInput`, `MokaPinInput`, `MokaIpAddressInput`, `MokaMacAddressInput`, `MokaAutoComplete`, `MokaTreeSelect`, `MokaTagInput`, `MokaRating`, `MokaKnob`, `MokaRangeSlider`, `MokaDateRangePicker`, `MokaFileUpload`, `MokaSignaturePad` and `MokaCreditCardInput` |

For a one-time code on a static page, use `MokaTextField`, since the digit boxes only join up with interactivity.
Buttons inside a field, such as Clear or Show password, do nothing there either.

A `name` attribute on any of these replaces the generated one. On interactive server pages the inputs carry a `name`
too, as Blazor's own `InputText` does, which also helps browsers fill them in. In WebAssembly they have none, unless
you give them one.

## Optional fields

`Optional` on `MokaTextField`, `MokaNumericField`, `MokaPasswordField`, `MokaTextArea`, `MokaSelect` and `MokaNativeSelect` shows a quiet word beside the label. It sits inside the label, so a screen reader reads it with the label. `Required` wins if both are set.

```razor
<MokaTextField @bind-Value="_name" Label="Name" Optional />
```

`MokaFieldWrapper` draws the word, and a control of your own can set it there:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Optional` | `bool` | `false` | Shows `OptionalText` beside the label |
| `OptionalText` | `string` | `"Optional"` | The word it shows |

## Naming a control that has no label of its own

`<label for>` only names a native form control, so a control built from a div (`MokaRating`,
`MokaSelect`, `MokaTreeSelect`) points `aria-labelledby` at the field label instead.
`MokaFieldWrapper` does this for its own children through `CustomControl`, and publishes the id it
uses so your own control can do the same:

```razor
<MokaFieldWrapper Label="Mode" InputId="mode" CustomControl="true">
    <MokaSegmentedControl aria-labelledby="@MokaFieldWrapper.LabelIdFor("mode")" @bind-Value="_mode">
        ...
    </MokaSegmentedControl>
</MokaFieldWrapper>
```

`MokaRadioGroup` takes its name from `Label`, which also draws a visible heading. For a group that
already sits under someone else's heading, set `AriaLabel` instead and leave `Label` off.

`MokaSearchInput` in a toolbar needs a name but no label above it. Give it a `Label` and `HideLabel`: the label stays
tied to the input, so screen readers read it, and is hidden from sight.

```razor
<MokaSearchInput Label="Search files" HideLabel @bind-Value="_query" />
```

## Buttons inside fields

The small buttons the inputs draw show only an icon or an arrow, so each is named by `aria-label` and by a `MokaTooltip`
on hover and on keyboard focus, never by a `title`:

| Component | Buttons |
|-----------|---------|
| `MokaTextField`, `MokaSearchInput`, `MokaAutoComplete` | Clear |
| `MokaPasswordField` | "Show password" / "Hide password" |
| `MokaTagInput` | "Remove *tag*" on each tag, "Clear all tags" |
| `MokaSelect` | "Clear selection", "Remove *item*" on each chip |
| `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker` | "Choose date", "Choose time", "Choose colour", and the colour picker's "Copy hex value", "Pick from screen" and swatches named by their value |
| `MokaCalendar`, `MokaDatePicker`, `MokaDateRangePicker` | "Previous month", "Next month" |
| `MokaFileUpload` | "Remove *file*" on each file |
| `MokaTreeSelect` | "Expand" / "Collapse" on each parent |
| `MokaFormBuilder` | "Move up", "Move down", "Remove field" on each field |

A button at the right end of an input opens its name below, lined up with that end. A button that opens a popup (the
date, time and colour pickers' toggles, the select's and autocomplete's buttons, the tag input's clear) shows no tooltip
while the popup is open, since the popup opens in the same place; its name stays for screen readers. Buttons that make a
column down a list, such as the file list's and the form builder's, open their names beside the column so a name never
covers the button below. The date picker's month arrows were bare "<" and ">" with no name at all before 0.1.20, and the
date range picker's and tree select's buttons had none either.

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

### MokaTextField Parameters (extras)

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |
| `Mono` | `bool` | `false` | Writes the value in the monospace font and turns spell check off |
| `Suffix` | `string?` | - | A unit inside the box at the right |

### Addresses and units

`Mono` is for addresses, names and keys. `Suffix` draws a unit after the value. The input's right padding makes room for it, it doesn't catch the pointer, and the input's `aria-describedby` points at it, so a screen reader hears the unit. An `aria-describedby` you pass stays, with the suffix's id after it. Beside an `EndIcon` or the clear button, the suffix sits to their left.

```razor
<MokaTextField @bind-Value="_address" Label="Address" Mono StartIcon="MokaIcons.Status.Info" />
<MokaTextField @bind-Value="_limit" Label="Speed limit" Suffix="Mbit/s" />
```

## PasswordField

```blazor-preview
@code { string _pwd = ""; }
<MokaPasswordField @bind-Value="_pwd" Label="Password" HelperText="Minimum 8 characters" />
```

### MokaPasswordField Parameters (extras)

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |

## NumericField

```blazor-preview
@code { int _qty = 1; }
<MokaNumericField TValue="int" @bind-Value="_qty" Label="Quantity" Min="1" Max="100" Step="1" />
```

The field reads and writes numbers in the invariant culture, with a dot before the decimals, whatever the page's culture. Up to 0.1.12 it wrote them in the page's culture and read them back in the invariant one, so a German page showed 1.5 as "1,5" and turned an edit of it into 15.

### MokaNumericField Parameters (extras)

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |
| `Mono` | `bool` | `false` | Writes the value in the monospace font |
| `Suffix` | `string?` | - | A unit inside the box at the right, tied to the input with `aria-describedby` |
| `StartIcon` | `MokaIconDefinition?` | - | Icon inside the box at the left. The input's left padding makes room for it |

```razor
<MokaNumericField TValue="int" @bind-Value="_limit" Label="Speed limit" Suffix="Mbit/s" StartIcon="MokaIcons.Status.Info" Mono />
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
| `MaxRows` | `int?` | - | Limits auto-resize height |
| `Optional` | `bool` | `false` | Shows a quiet "Optional" beside the label. `Required` wins |
| `Mono` | `bool` | `false` | Writes the text in the monospace font and turns spell check off, for lists of names or addresses |

```razor
<MokaTextArea @bind-Value="_hosts" Label="Blocked hosts" Mono Rows="6" />
```

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

### MokaTimePicker Parameters (extras)

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Typed` | `bool` | `false` | The time can be typed as well as picked |

### Typing a time

With `Typed`, the input stops being read-only and reads what it's given when it loses focus or on Enter. It reads 21:30, 9:30 pm, 9.30p, 21h30, 2130, 930pm, 9 pm and 21, whatever `Format24` says, and shows the time back in the field's own format.

```razor
<MokaTimePicker @bind-Value="_time" Label="Quiet hours start" Typed Format24="false" />
```

- A bare hour on a 24-hour field is that hour: 9 is 09:00. On a 12-hour field it names two times, so it keeps the half the field is in, the way the popup's hour column keeps its AM or PM: 9 is 09:00 PM while the field holds an afternoon time, and 09:00 AM otherwise. 12 on an empty field is noon.
- Text it can't read keeps the last good value and shows an error with `aria-invalid`. The text stays in the box so it can be fixed. Inside an `EditForm` the form won't submit until it is.
- `Min` and `Max` still apply. A time outside them is turned down the same way.
- Emptying the field clears the value.
- The popup keeps working from the clock button, and a pick from it drops the rejected text. A click in the input only puts the caret there, and Space types a space. Down still opens the popup.

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

### Your app's own file picker

An app can pick the files itself, in place of the browser's dialog: register an `IMokaFilePicker`, or pass one to `FilePicker` (`IMokaFilePicker?`), which wins over the registered one. A Blazor Hybrid app on Windows wants this, since WebView2's file dialog can end the app. See [File Picker](/components/file-picker) for a complete MAUI picker.

```razor
<MokaFileUpload Label="Receipt" Accept="image/*,.pdf" Multiple FilePicker="_picker" OnFilesSelected="Read" />
```

With a picker, a click, Enter or Space on the zone or its labels is cancelled in the browser before the dialog opens, and the picker is asked with `Multiple` and `Accept`. What it hands back goes through every check above and then `OnFilesSelected` and the list, as a pick from the browser does. An empty list, or an `OperationCanceledException`, is a cancel and changes nothing. Any other exception goes to the nearest error boundary, as one from `OnFilesSelected` would. While the picker is open another click does nothing, and files it returns after the upload was disabled or removed are ignored. Drops still arrive through the file input. Without a picker the zone renders and behaves as it always has.

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

### Picture cards

For a choice people judge by looking, such as a theme or a layout, set `Cards` and give each item a `Preview`. It's the same control: native radios, one tab stop, the arrow keys, `EditForm` and form posts all work as above.

```razor
<MokaRadioGroup @bind-Value="_mode" Label="Color mode" Cards="true" MinColumn="12rem">
    <MokaRadioItem Value="@("light")" Label="Light">
        <Preview><ThemeMock Theme="MokaTheme.DefaultLight" /></Preview>
    </MokaRadioItem>
    <MokaRadioItem Value="@("dark")" Label="Dark">
        <Preview><ThemeMock Theme="MokaTheme.Default" /></Preview>
    </MokaRadioItem>
    <MokaRadioItem Value="@("system")" Label="System" Hint="Follows this device as it changes">
        <Preview><ThemeMock /></Preview>
    </MokaRadioItem>
</MokaRadioGroup>
```

| Parameter | On | What it does |
|-----------|----|--------------|
| `Cards` | `MokaRadioGroup` | Draws the items as cards that wrap to fit. `Orientation` doesn't apply |
| `MinColumn` | `MokaRadioGroup` | The narrowest a card gets before the cards wrap. A CSS length or `var()`, `10rem` when unset |
| `Preview` | `MokaRadioItem` | The picture above the name. Any markup |
| `Hint` | `MokaRadioItem` | A line under the name |

`Preview` only shows with `Cards`. `Hint` shows on a plain radio too, as a quiet line under its label. The picture is hidden from screen readers because the name says what it shows, so make the name enough on its own. The chosen card gets a check by its name, and its ring follows `WithSelection`.

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

### Looks

The suggestions take the same `MokaListLook` as [`MokaSelect`](select.md#looks), and their box follows the theme's popup look. The looks that mark a chosen row (`Check`, `Solid`, `Radio`, `Terminal`) mark the suggestion equal to `Value`. `Compact`'s key hints stay at the foot of the list as it scrolls, with their words in `MoveHintText`, `PickHintText` and `CloseHintText`.

```razor
<MokaThemeProvider Theme="@(MokaTheme.Red.WithLists(MokaListLook.Check))">
    @Body
</MokaThemeProvider>

<MokaAutoComplete @bind-Value="_country" Label="Country" SearchFunc="Search" Look="MokaListLook.Attached" />
```

`Look` on the field beats the theme. Looks belong to the new design: on `MokaTheme.Dark` and `MokaTheme.Light` the suggestions stay as they are.

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
- The canvas draws at the screen's own density and follows the pad's size, so it stays sharp on any screen and keeps the drawing when the pad is resized. `StrokeWidth` is in CSS pixels.
- `Value` is a PNG at 2 pixels per CSS pixel of the pad, on every device, so the same signature comes out the same size from a laptop or a phone. Give an `<img>` that shows it a width, or it shows at twice the pad's size.

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

## Lists and pickers above the page

The open calendar of a `MokaDatePicker`, the time list of a `MokaTimePicker`, the `MokaColorPicker` panel and the suggestions of `MokaAutoComplete` and `MokaTagInput` sit in the browser's top layer, so a card that hides its overflow no longer cuts them off and nothing later on the page paints over them, whatever its `z-index`. They stay against their field as the page scrolls and flip above it when the window has no room below. While one is open the library sets its position, so a stylesheet can restyle it but not move it.
