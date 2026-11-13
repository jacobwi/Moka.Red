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
| `MokaPhoneInput` | `string` | Country code + number, or a country picker with `Country` |
| `MokaCountrySelect` | `string?` | A country's ISO code, see [Country Select](country-select.md) |
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
| Shown and posted, but can't be changed | `MokaSelect`, `MokaCountrySelect`, `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker`, `MokaCascader`, `MokaInlineEdit`, `MokaEmojiPicker`. Their popups need interactivity, so they keep the value the server set. `MokaSelect` with `Multiple` posts nothing |
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
| `MokaDatePicker`, `MokaTimePicker`, `MokaColorPicker` | "Choose date", "Choose time", "Choose colour", and the colour picker's "Copy hex value", "Pick from screen" and swatches named by their value; in the other layouts "Copy hex" and, with the format switch, "Copy value" |
| `MokaColorInput` | `Split`'s "Open the color picker" and "Pick from screen", and `Palette`'s swatches named by `ColorLabel` (Red to Gray for the defaults) |
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
| `EmojiButton` | `bool` | `false` | A smile at the end of the box opens the emoji panel and puts the pick in at the caret, over any selection, then hands focus back to the field. See [Emoji picker](/components/emoji-picker) |
| `EmojiOptions` | `MokaEmojiOptions?` | - | The panel's options. It opens as a popup either way: `List` opens the list and every other shape the grid |
| `EmojiOptionsChanged` | `EventCallback<MokaEmojiOptions>` | - | Raised with the new skin tone and frequently used list, for the app to store |
| `EmojiButtonLabel` | `string` | `"Insert emoji"` | The button's name and tooltip |

```razor
<MokaTextField @bind-Value="_title" Label="Title" EmojiButton @bind-EmojiOptions="_emoji" />
```

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
| `EmojiButton` | `bool` | `false` | A smile in the bottom corner opens the emoji panel and puts the pick in at the caret, then hands focus back. See [Emoji picker](/components/emoji-picker) |
| `EmojiOptions` | `MokaEmojiOptions?` | - | The panel's options, a popup either way |
| `EmojiOptionsChanged` | `EventCallback<MokaEmojiOptions>` | - | Raised with the new skin tone and frequently used list |
| `EmojiButtonLabel` | `string` | `"Insert emoji"` | The button's name and tooltip |

```razor
<MokaTextArea @bind-Value="_hosts" Label="Blocked hosts" Mono Rows="6" />
```

For a ":" typeahead (type `:ta` for a list, or `:tada:` for 🎉), wrap the text area in `MokaEmojiTypeahead`:

```razor
<MokaEmojiTypeahead>
    <MokaTextArea @bind-Value="_notes" Label="Release notes" EmojiButton />
</MokaEmojiTypeahead>
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

### Color

`Color` fills the checked or indeterminate box and tints its focus ring, with the check in the matching "on" color. Unset, both are the theme's primary color, as before. `Surface` is a neutral box: the text color with the check in the surface color.

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaCheckbox Value="true" Label="Primary" />
    <MokaCheckbox Value="true" Color="MokaColor.Success" Label="Success" />
    <MokaCheckbox Value="true" Color="MokaColor.Error" Label="Error" />
    <MokaCheckbox Value="true" Color="MokaColor.Surface" Label="Surface" />
</div>
```

Up to 0.4.2 `Color` did nothing on a checkbox.

## Switch

```blazor-preview
@code { bool _notify = true; }
<MokaSwitch @bind-Value="_notify" Label="Email notifications" />
```

The input is a checkbox with `role="switch"`, which ARIA in HTML allows on `input type=checkbox`, so a screen reader says "on" or "off" rather than "checked". The state comes from the checkbox's own checked state; ARIA in HTML forbids `aria-checked` there, so the switch writes none. A `role` you pass yourself still wins.

`Color` tints the track when the switch is on and its focus ring. Unset, both are the theme's primary color, as before. `Surface` is a neutral grey track.

```blazor-preview
<div style="display:flex;gap:16px;flex-wrap:wrap">
    <MokaSwitch Value="true" Label="Primary" />
    <MokaSwitch Value="true" Color="MokaColor.Success" Label="Success" />
    <MokaSwitch Value="true" Color="MokaColor.Warning" Label="Warning" />
    <MokaSwitch Value="true" Color="MokaColor.Surface" Label="Surface" />
</div>
```

Up to 0.4.2 the switch's checkbox had no switch role, and `Color` did nothing.

On the new design `MokaTheme.WithSwitches(MokaSwitchLook.Tinted)` draws a switch that is on as a tint of its color inside a ring of it, with the knob in that color and a small glow, in place of the solid track. `Color` still picks the color. `MokaTheme.Ember` has it on, and `MokaTheme.Dark` and `Light` don't change.

```razor
<MokaThemeProvider Theme="_theme">
    <MokaSwitch @bind-Value="_wifiOnly" Label="Back up on Wi-Fi only" />
</MokaThemeProvider>

@code {
    private readonly MokaTheme _theme = MokaTheme.Red.WithSwitches(MokaSwitchLook.Tinted);
    private bool _wifiOnly = true;
}
```

## Toggles that save

`Loading` on `MokaSwitch` and `MokaCheckbox` (it lives on `MokaToggleBase`) marks a toggle busy while its new value saves
to a server. A spinner turns in the switch's knob or in the checkbox's box, in the toggle's color, and the box still shows
the value: the knob stays where it is and a checked box stays filled. The input carries `aria-busy="true"` and stays
enabled, so it keeps focus. A click, a click on the label or Space doesn't change it again while it's busy: a native
checkbox flips itself before any handler runs, so the click's default action is cancelled, the box never flips and no
change is raised. That needs an interactive render.

```razor
<MokaSwitch @bind-Value="_backup" @bind-Value:after="SaveBackupAsync" Loading="_saving" Label="Back up to the server" />

@code {
    private bool _backup;
    private bool _saving;

    private async Task SaveBackupAsync()
    {
        _saving = true;
        await Api.SaveBackupAsync(_backup);
        _saving = false;
    }
}
```

If the save fails, set the value back yourself when `Loading` ends.

## SearchInput

```razor
<MokaSearchInput @bind-Value="_query" Label="Search files" HideLabel OnSearch="FilterAsync" />
```

`OnSearch` fires once typing pauses for `Debounce` milliseconds (300 by default; 0 or less searches on every key), with the text in the box. The clear button reports an empty search at once and drops a search that was still waiting, including one whose delay had just ended. A search reports the box as it is when its delay ends, so if the parent passes a new `Value` meanwhile, that is what it reports. Escape, which empties a search box in Chromium-based browsers, arrives as typing an empty value and drops the earlier search the same way.

The delay runs on a `TimeProvider` when the app registers one, else the system clock.

Up to 0.4.2 the clear button left the delay running, so text typed just before a clear was searched after the box was emptied and replaced the clear's empty search.

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

- A colour reaches the preview, the presets and the recent colours only as a hex colour (`#rgb`, `#rgba`, `#rrggbb` or `#rrggbbaa`) or a theme token such as `var(--moka-color-primary)`. Anything else shows no colour, so a value can never add CSS of its own. A theme token is also a valid value in an `EditForm`.
- The gradient, the sliders and their thumbs are placed with invariant numbers. Up to 0.1.12 they sat at the left edge in locales that write decimals with a comma.

### Layout and extras parameters

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Layout` | `MokaColorPickerLayout` | `Standard` | The panel: `Standard`, `Tabbed`, `Wheel`, `Sliders`, `PaletteFirst`, `Compact` |
| `Inline` | `bool` | `false` | Draws the panel in place, always open, with no field or popup |
| `ShowContrast` | `bool` | `false` | The color as text on a background, with its contrast ratio and WCAG level |
| `ContrastBackground` | `string?` | `null` | What `ShowContrast` measures against: hex, `rgb()` or a theme token. Null is the theme's page background |
| `ShowFormats` | `bool` | `false` | A HEX, RGB, HSL and OKLCH switch with the value in that format and a copy button |
| `Format`, `FormatChanged` | `MokaColorFormat` | `Hex` | The format `ShowFormats` starts on, and the person's pick |
| `ShowAlphaPreview` | `bool` | `false` | With `ShowAlpha`: the alpha bar over a checkerboard from the theme's colors on the new design, a half solid and half see-through preview, the 8 digit hex and the opacity as a number |
| `LabelledEyeDropper` | `bool` | `false` | The eyedropper as a labelled "Pick from screen" button with I as its key, hidden where the browser has none |
| `ClearableRecentColors` | `bool` | `false` | The recent colors with Clear and Undo, a chosen one moved to the front at once, in every layout |
| `RecentColors`, `RecentColorsChanged` | `IReadOnlyList<string>?` | `null` | The recent colors, newest first, at most eight. Bind them to keep them |
| `ShowThemeColors` | `bool` | `false` | Named swatches that set the value to a theme token, so the color follows the theme |

### Layouts

`Standard` is the original panel and stays the default. The other layouts drag in the browser and tell the app the color when the pointer is let go, so a drag sends nothing over the circuit while it moves. Every slider in them is `role="slider"` and takes the arrow keys, Shift for ten steps, Page Up and Page Down, Home and End.

- `Tabbed`: Spectrum and Swatches tabs over one hex row. Swatches holds the theme's colors and the palette (`Presets`, or 24 colors). The arrow keys move between the tabs.
- `Wheel`: a hue ring around a saturation square, with the hex, a copy button and an H, S, B readout.
- `Sliders`: hue, saturation and lightness sliders whose tracks show where each goes, each with a number field.
- `PaletteFirst`: the palette and recent colors on top, and a small spectrum under More colors.
- `Compact`: a narrow panel with the square, one hue bar, and the hex with an eyedropper. `MokaColorInput`'s popups open it by default.

```razor
<MokaColorPicker Label="Accent" Layout="MokaColorPickerLayout.Tabbed" @bind-Value="_accent" />

<MokaColorPicker Inline="true" Layout="MokaColorPickerLayout.Wheel" @bind-Value="_accent" />
```

An `Inline` panel counts a color as chosen, for the recent colors, when focus leaves it.

### Contrast check

The chosen color as text on the page background, with its ratio (cut, not rounded, to two decimals, so 4.499 never reads as 4.50) and the WCAG level it meets: AAA at 7:1, AA at 4.5:1, "Large text only" at 3:1, else "Fails". It's worked out in C# from the cascaded theme, so it follows a switch between light and dark, and a see-through color is laid over the background first.

```razor
<MokaColorPicker Label="Text color" ShowContrast="true" @bind-Value="_text" />

<MokaColorPicker Label="Text on cards" ShowContrast="true" ContrastBackground="var(--moka-color-surface)" @bind-Value="_text" />
```

### Format switch

HEX, RGB, HSL and OKLCH. The value shows in mono in the chosen format, and the copy button copies it that way. The value the picker writes stays hex.

Every copy button in the picker (the field's "Copy hex value", the panel's "Copy hex" and "Copy value") copies in the browser, inside the click, the way `MokaCopyButton` does: up to 0.4.2 the copy came a round trip later on Blazor Server, and Safari, iOS and Firefox refused it. When the browser refuses, the format switch says "Press Ctrl+C to copy it."

```razor
<MokaColorPicker ShowFormats="true" @bind-Format="_format" @bind-Value="_color" />
```

### Alpha on a checkerboard

```razor
<MokaColorPicker ShowAlpha="true" ShowAlphaPreview="true" @bind-Value="_overlay" />
```

On the original design (`MokaTheme.Dark` and `Light`) the Standard panel's alpha bar keeps its look; the preview and the 8 digit hex show on both.

### Pick from screen

A labelled button in place of the bare eyedropper icon. Press I anywhere in the panel but a text field to use it. It hides where the browser has no EyeDropper (Firefox, Safari), and a status line says what it picked.

```razor
<MokaColorPicker LabelledEyeDropper="true" @bind-Value="_color" />
```

### Recent colors

The last eight colors, newest first, with Clear. Choosing one moves it to the front. Bind the list to keep it across pages or visits.

```razor
<MokaColorPicker ClearableRecentColors="true" @bind-RecentColors="_recent" @bind-Value="_color" />

@code {
    private IReadOnlyList<string> _recent = [];
}
```

### Theme colors

Named swatches (Primary, Success, Warning, Error, Info, Surface, On surface, Outline) that set the value to the token, such as `var(--moka-color-primary)`, in place of a fixed hex, so the color follows the theme. The panel shows the color the token stands for in the current theme. In `Tabbed` they go in the Swatches tab.

```razor
<MokaColorPicker ShowThemeColors="true" @bind-Value="_labelColor" />
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

### What the upload tells the app

| Name | Type | Description |
|------|------|-------------|
| `OnFilesAdded` | `EventCallback<IReadOnlyList<IBrowserFile>>` | Only the files a pick, a drop or the app's picker just added, so each file arrives once to read or upload |
| `OnFilesSelected` | `EventCallback<IReadOnlyList<IBrowserFile>>` | The whole list, each time it changes: a pick or drop that added a file, a remove, Clear all |
| `OnFileRemoved` | `EventCallback<IBrowserFile>` | A file that left the list: its remove button, Clear all, or a new file taking its place in a look that holds one |
| `Files`, `FilesChanged` | `IReadOnlyList<IBrowserFile>?` | The list, for `@bind-Files`, so the app can change it |

A pick or a drop whose every file was refused (its type, its size, or past `MaxFiles`) changes nothing, so it raises nothing and only shows its error lines; up to 0.4.1 it raised `OnFilesSelected` with the list from before, so an app could read a file twice. Each list handed over is new and stays as it was. In a look that holds one file, a new file raises `OnFileRemoved` for the one it replaced first. After a change the order is `OnFileRemoved`, `FilesChanged`, `OnFilesAdded`, then `OnFilesSelected`.

```razor
<MokaFileUpload Look="MokaFileUploadLook.Slot" Accept=".json" @bind-Files="_backup" OnFilesAdded="ReadAsync" />
<MokaButton Disabled="@(_settings is null)" OnClick="RestoreAsync">Restore</MokaButton>

@code {
    private IReadOnlyList<IBrowserFile> _backup = [];
    private Settings? _settings;

    private async Task ReadAsync(IReadOnlyList<IBrowserFile> files)
    {
        await using Stream stream = files[0].OpenReadStream(MaxBytes);
        _settings = await Settings.ReadAsync(stream);
    }

    private async Task RestoreAsync()
    {
        await Live.RestoreAsync(_settings!);
        _settings = null;
        _backup = [];
    }
}
```

Setting `Files` to a new list changes the upload's list, here emptying the slot once its file has been used. A one-file look keeps the first file. It raises no callbacks, clears the last pick's error lines, and shows the files without a preview unless `PreviewSource` gives one. The upload takes a list only when it isn't the one it was given last, as a bound parameter should, so bind it rather than passing `[]` one way: an unbound `[]` set twice is the same list. That follows the Blazor docs' advice to change a child through its parameters rather than a component reference (`blazor/components/index.md`, "Capture references to components").

### Your app's own file picker

An app can pick the files itself, in place of the browser's dialog: register an `IMokaFilePicker`, or pass one to `FilePicker` (`IMokaFilePicker?`), which wins over the registered one. A Blazor Hybrid app on Windows wants this, since WebView2's file dialog can end the app. See [File Picker](/components/file-picker) for a complete MAUI picker.

```razor
<MokaFileUpload Label="Receipt" Accept="image/*,.pdf" Multiple FilePicker="_picker" OnFilesSelected="Read" />
```

With a picker, a click, Enter or Space on the zone or its labels is cancelled in the browser before the dialog opens, and the picker is asked with `Multiple` and `Accept`. What it hands back goes through every check above and then `OnFilesSelected` and the list, as a pick from the browser does. An empty list, or an `OperationCanceledException`, is a cancel and changes nothing. Any other exception goes to the nearest error boundary, as one from `OnFilesSelected` would. While the picker is open another click does nothing, and files it returns after the upload was disabled or removed are ignored. Drops still arrive through the file input. Without a picker the zone renders and behaves as it always has.

Each remove button is named after its file, such as "Remove report.pdf".

Up to 0.1.12 the drop zone cancelled every drop, so a dropped file did nothing, and the native file input showed inside the zone because its hiding rule never reached it. `Accept` only filtered the dialog, a pick of more files than `MaxFiles` threw and was lost, separate picks could grow the list past `MaxFiles`, and the remove buttons had no accessible name.

### Upload looks

Every option below is a parameter whose default keeps the upload as it was, markup included.

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Look` | `MokaFileUploadLook` | `Standard` | How the upload draws itself and its files: `Standard`, `CardList`, `Field`, `Picture`, `Gallery`, `Quiet`, `Terminal` or `Slot` |
| `DragLook` | `MokaFileUploadDragLook` | `Standard` | How the zone looks while a file is over it: `Standard`, `Glow`, `Tint`, `Marching` or `Lift`. The `Standard` and `CardList` looks only |
| `ListLook` | `MokaFileUploadListLook` | `Standard` | How the chosen files are listed: `Standard`, `Rows`, `Progress`, `Chips` or `Thumbnails`. The `Standard` and `Quiet` looks only |
| `CheckWhileDragging` | `bool` | `false` | Reads the dragged files' types and count and says before the drop whether they fit |
| `PictureShape` | `MokaPictureShape` | `Circle` | The frame of the `Picture` look: `Circle` or `RoundedSquare` |
| `PictureSource` | `string?` | -- | The picture the app holds now, for the `Picture` look. Only an address a picture can come from is used |
| `OnPictureRemoved` | `EventCallback` | -- | Raised by Remove in the `Picture` look when the picture is the app's own, not a picked file |
| `Previews` | `bool` | `false` | Shows picked pictures where a look has room for one |
| `PreviewSource` | `Func<IBrowserFile, string?>?` | -- | The app's own address for a file's picture. Wins over `Previews` |
| `Status` | `Func<IBrowserFile, MokaUploadStatus?>?` | -- | Each file's upload state as the app sees it |
| `OnRetry` | `EventCallback<IBrowserFile>` | -- | Shows Retry on failed files and raises this with the file |
| `Text` | `MokaFileUploadText?` | -- | Every word the upload writes. Null keeps the English ones |

```razor
<MokaFileUpload Label="Attachments" Look="MokaFileUploadLook.CardList" Multiple
                Status="StatusOf" OnRetry="RetryAsync" OnFilesAdded="StartAsync" />
```

- `CardList` is a slimmer zone, then each file as a small card with its type, its size and, when the app reports it, its progress.
- `Field` is sized like a text field: a Browse button with the chosen files as chips inside it, and the total under it. Chips that don't fit on the line are counted as "+2" and leave the tab order until there is room. A drop on the field adds files.
- `Picture` is one picture in a round or rounded-square frame (`PictureShape`), for an avatar or a logo. Replace shows on hover and on keyboard focus, and a drop on the frame replaces the picture. It shows `PictureSource` until a file is picked, and the picked file once its picture is known (turn on `Previews`).
- `Gallery` is picture thumbnails in a grid with an Add tile at the end, which goes once `MaxFiles` is reached. The first is the cover, remove shows on hover or focus, and a drop anywhere on the grid adds pictures.
- `Quiet` is one line under the fields, for a tight form. A drop on the line adds files.
- `Terminal` is monospace in a dashed accent frame, each file a log line with a text bar for its progress, and what the last pick turned away logged with it.
- `Slot` is one required document, empty or filled, with Replace and Remove.

`Picture` and `Slot` always take one file, whatever `Multiple` says. `Picture` and `Gallery` take `image/*` when `Accept` isn't set. `ShowFileList` applies to the lists under `Standard`, `CardList` and `Quiet`; the other looks show their files as part of the control. Every look keeps the file input over its drop target, so a click or a drop lands on it, and `Accept`, `MaxFileSize`, `MaxFiles`, `Disabled` and an app's `FilePicker` work the same in all of them.

### While a file is dragged over

```razor
<MokaFileUpload Label="Attachments" DragLook="MokaFileUploadDragLook.Lift" />
```

`Glow` turns the border solid with an accent ring and glow, and the inside stays light. `Tint` fills the zone with the accent tint and swaps its words for a large drop icon and "Drop to add it". `Marching` moves the dashed border round the zone, and stands still when the system asks for less motion. `Lift` raises the zone a little with a shadow and says "Drop to upload", also without the movement for less motion. The other looks light up their own way.

With `CheckWhileDragging` the browser reads the dragged files' types and count before the drop and the zone says yes or no: "1 file, ready to drop", "ZIP files can't be added" with the types it takes, "No room for more files", or "Too many files" with how many more fit. A file's size and name aren't known until the drop, so `MaxFileSize` is checked then, and an extension in `Accept` is matched through the types it usually has. A type the browser can't tell counts as fitting. The check only advises: a drop goes through every check as before. The `Standard` and `CardList` zones show the words; the other looks turn green or red. The check runs in the browser, so a drag costs no round trips to .NET.

```razor
<MokaFileUpload Label="Attachments" Accept="application/pdf,image/png,image/jpeg" Multiple MaxFiles="3" CheckWhileDragging />
```

### The list of chosen files

```razor
<MokaFileUpload Label="Attachments" ListLook="MokaFileUploadListLook.Progress" Multiple Status="StatusOf" />
```

`Rows` is one grouped list with a colored type tile, the name, and the size and type in monospace. `Progress` adds a thin line under each name for the upload: a check and Done, a percent while uploading, red with Retry when it failed. `Chips` puts each file in a chip that wraps, with the total and Clear all under them. `Thumbnails` shows pictures as thumbnails in one row that scrolls, other files as a type tile.

### Upload progress

The upload never uploads anything: the app does, and reports each file through `Status`. Return a `MokaUploadStatus(State, Percent, Message)` for a file, with `MokaUploadState` `Waiting`, `Uploading`, `Done` or `Failed`, or null for a file it says nothing about. `Message` shows in place of the size, such as why it failed.

`Status` is asked for every file every time the upload draws, so when an upload moves on, re-render the component that holds the upload (`StateHasChanged`) and it asks again. A file the app reports uploading has its remove button named "Cancel *file*", and it raises `OnFileRemoved` like any remove, so stop the upload there. With `OnRetry`, failed files show Retry. When a file moves to done or failed, a hidden status region says so for screen readers; percents aren't read out.

```razor
@code {
    private readonly Dictionary<IBrowserFile, MokaUploadStatus> _uploads = new(ReferenceEqualityComparer.Instance);

    private MokaUploadStatus? StatusOf(IBrowserFile file) => _uploads.GetValueOrDefault(file);

    private async Task StartAsync(IReadOnlyList<IBrowserFile> files)
    {
        foreach (IBrowserFile file in files)
        {
            _uploads[file] = new MokaUploadStatus(MokaUploadState.Uploading);
            // Send the file, setting _uploads[file] as it goes and calling StateHasChanged.
        }
    }
}
```

### Pictures of picked files

The upload never reads a file unless asked. `Previews` shows picked pictures through an object URL the browser makes from the file it already holds, as the Blazor docs' "Upload image preview" does, so nothing is read or sent to .NET and nothing sits in the server's memory. The URL is let go when the file leaves the list or the upload goes. That works for files picked or dropped through the browser. `RequestImageFileAsync` was the other way: it resizes in the browser but still streams the picture to .NET to make a data URL, which the docs call most fit for WebAssembly.

`PreviewSource` gives the app's own address for a file, such as where it was uploaded, and wins over `Previews`. Files an `IMokaFilePicker` hands over get a picture only this way. Either address is shown only when a picture can come from it (`http`, `https`, `blob`, a relative address or an image `data:` URI).

```razor
<MokaFileUpload Label="Listing photos" Look="MokaFileUploadLook.Gallery" Multiple MaxFiles="12" Previews />
<MokaFileUpload Label="Profile picture" Look="MokaFileUploadLook.Picture" PictureSource="@_avatarUrl"
                Previews OnPictureRemoved="RemoveAvatarAsync" OnFilesSelected="UploadAvatarAsync" />
```

### Words

`Text` takes a `MokaFileUploadText` with every word the upload writes, English by default. Anything holding a name or a count is a function. The few words the browser fills in while a file is dragged (`CheckReady`, `ManyFiles`, `CheckWrongType`) are strings with `{0}` where the count or the type goes.

```razor
<MokaFileUpload Text="@(new MokaFileUploadText { DragHere = "Släpp filer här eller", Browse = "Bläddra", Remove = name => $"Ta bort {name}" })" />
```

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

### A country picker

With `Country`, the fixed `CountryCode` becomes a button with the flag and the dialing code, opening the searchable list of [`MokaCountrySelect`](country-select.md): its rows show dialing codes, and "44" or "+44" finds the United Kingdom. The value stays one string, the country's dialing code and the number, such as `+46701234567`.

```razor
<MokaPhoneInput @bind-Value="_phone" @bind-Country="_country" Label="Phone" />

@code {
    private string _phone = "";
    private string? _country = "SE";
}
```

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Country` | `string?` | -- | The picked country's code. Null keeps the fixed `CountryCode` and everything as before |
| `CountryChanged` | `EventCallback<string?>` | -- | Raised when another country is picked. The number moves to its dialing code |
| `Countries` | `IReadOnlyList<string>?` | -- | Limits the picker to these codes |
| `SuggestedCountries` | `IReadOnlyList<string>?` | -- | Codes listed first, after the browser's own country |
| `CountryLabel` | `string?` | -- | The picker's name and tooltip. Null is "Country code" |
| `DropTrunkZero` | `bool` | `true` | Whether a 0 typed before the number is left out of the value, so 070 123 45 67 in Sweden is saved as +46701234567 |

Numbers in the North American plan (+1) are still formatted as (555) 123-4567 when `AutoFormat` is on. Others keep the grouping you type, up to the 15 digits a number can have with its dialing code. A 0 in front is the trunk prefix in most countries, so it's left out, but Italy, San Marino, Vatican City, Côte d'Ivoire, the Republic of the Congo, Benin and Gabon keep it after the dialing code, so there it stays. Other trunk prefixes (8 in Russia, 06 in Hungary) aren't known, and a number typed with one keeps it. On a static page the picker can't open, and the number posts as typed, without the dialing code.

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
